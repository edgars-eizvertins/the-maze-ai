using Labyrinth.Application.Abstractions;
using Labyrinth.Application.Content;
using Labyrinth.Application.Mapping;
using Labyrinth.Application.State;
using Labyrinth.Domain;
using Labyrinth.Shared;

namespace Labyrinth.Application.Services;

/// <summary>
/// Orchestrates a player's turn: navigation, combat hand-off, provisions, elixir
/// and the manual narrative adjustments (gold/items/attributes the book asks the
/// reader to apply by hand). The single entry point used by the API controllers.
/// </summary>
public sealed class GameService
{
    private const int MaxAutoDepth = 30;

    private readonly ISectionRepository _sections;
    private readonly IPlayerAccountStore _store;
    private readonly CombatService _combat;
    private readonly GameStateFactory _factory;
    private readonly IDiceRoller _dice;
    private readonly CombatResolver _resolver;

    public GameService(ISectionRepository sections, IPlayerAccountStore store,
        CombatService combat, GameStateFactory factory, IDiceRoller dice, CombatResolver resolver)
    {
        _sections = sections;
        _store = store;
        _combat = combat;
        _factory = factory;
        _dice = dice;
        _resolver = resolver;
    }

    // ---- Reads -----------------------------------------------------------
    public async Task<ServiceResult<TurnDto>> GetTurnAsync(string player, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        return acc is null
            ? ServiceResult<TurnDto>.Fail("Игрок не найден.")
            : ServiceResult<TurnDto>.Ok(BuildTurn(acc.State));
    }

    // ---- Navigation ------------------------------------------------------
    public async Task<ServiceResult<TurnDto>> ChooseAsync(string player, int target, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        if (state.IsFinished) return ServiceResult<TurnDto>.Fail("Игра окончена. Начните заново.");
        if (state.InCombat) return ServiceResult<TurnDto>.Fail("Сначала заверши битву.");

        var current = _sections.Get(state.CurrentSection);
        if (current.Choices.All(c => c.Target != target))
            return ServiceResult<TurnDto>.Fail("Недопустимый выбор для этого раздела.");

        var steps = new List<AutoStepDto>();
        MoveTo(state, target, steps);
        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state, autoSteps: steps));
    }

    /// <summary>
    /// Jump to an explicit section the reader is told to note down and return to
    /// ("запиши этот номер … а потом N") or that the book asks to compute (e.g. §76,
    /// the sum of the three keys). Honour-based, like the manual adjustments — it is
    /// how the printed book is played and is the only way past the "note-the-number"
    /// return sections such as §315.
    /// </summary>
    public async Task<ServiceResult<TurnDto>> GoToAsync(string player, int target, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        if (state.IsFinished) return ServiceResult<TurnDto>.Fail("Игра окончена. Начните заново.");
        if (state.InCombat) return ServiceResult<TurnDto>.Fail("Сначала заверши битву.");
        if (!_sections.Exists(target)) return ServiceResult<TurnDto>.Fail($"Раздела {target} не существует.");

        var steps = new List<AutoStepDto>();
        MoveTo(state, target, steps);
        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state, autoSteps: steps));
    }

    // ---- Combat ----------------------------------------------------------
    public async Task<ServiceResult<TurnDto>> CombatRoundAsync(string player, bool useLuck, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        if (!state.InCombat) return ServiceResult<TurnDto>.Fail("Сейчас нет битвы.");

        var round = _combat.PlayRound(state, useLuck);
        await _store.SaveStateAsync(player, state, ct);

        var turn = BuildTurn(state, round is null ? [] : [round.ToDto()]);
        return ServiceResult<TurnDto>.Ok(turn);
    }

    public async Task<ServiceResult<TurnDto>> FleeAsync(string player, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        var flee = _combat.Flee(state);
        if (!flee.Success) return ServiceResult<TurnDto>.Fail(flee.Error!);

        var steps = new List<AutoStepDto>();
        if (!state.IsFinished) MoveTo(state, flee.Value, steps);
        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state, autoSteps: steps));
    }

    // ---- Provisions & elixir --------------------------------------------
    public async Task<ServiceResult<TurnDto>> EatAsync(string player, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        if (!_sections.Get(state.CurrentSection).CanEat)
            return ServiceResult<TurnDto>.Fail("Здесь нельзя подкрепиться.");
        if (state.Food <= 0) return ServiceResult<TurnDto>.Fail("Запасы еды закончились.");

        state.Food--;
        var end = state.Endurance.ToScore();
        end.Increase(GameRules.FoodEnduranceGain);
        state.Endurance.From(end);

        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state));
    }

    public async Task<ServiceResult<TurnDto>> UseElixirAsync(string player, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        if (state.Elixir is null) return ServiceResult<TurnDto>.Fail("У тебя нет эликсира.");
        if (state.ElixirUsesLeft <= 0) return ServiceResult<TurnDto>.Fail("Эликсир уже использован дважды.");

        switch (state.Elixir.Value)
        {
            case ElixirType.Agility:
                var a = state.Agility.ToScore(); a.RestoreToInitial(); state.Agility.From(a); break;
            case ElixirType.Endurance:
                var e = state.Endurance.ToScore(); e.RestoreToInitial(); state.Endurance.From(e); break;
            case ElixirType.Luck:
                var l = state.Luck.ToScore(); l.RestoreLuck(); state.Luck.From(l); break;
        }
        state.ElixirUsesLeft--;

        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state));
    }

    // ---- Manual narrative adjustments (faithful to the printed book) -----
    public async Task<ServiceResult<TurnDto>> AdjustAsync(string player, AdjustKind kind, string? text, int delta, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");
        var state = acc.State;

        if (!ApplyAdjustment(state, kind, text, delta))
            return ServiceResult<TurnDto>.Fail("Нечего изменять.");

        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state));
    }

    /// <summary>Applies one stat/gold/item change. Shared by the manual controls and
    /// the automatic per-section <see cref="EffectContent"/> effects. Returns false
    /// when there was nothing to change.</summary>
    private static bool ApplyAdjustment(GameState state, AdjustKind kind, string? text, int delta)
    {
        switch (kind)
        {
            case AdjustKind.Gold:
                state.Gold = Math.Max(0, state.Gold + delta); break;
            case AdjustKind.Food:
                state.Food = Math.Clamp(state.Food + delta, 0, GameRules.StartingFood); break;
            case AdjustKind.Agility:
                ApplyDelta(state.Agility, delta); break;
            case AdjustKind.Endurance:
                ApplyDelta(state.Endurance, delta); break;
            case AdjustKind.Luck:
                ApplyDelta(state.Luck, delta); break;
            case AdjustKind.AddItem when !string.IsNullOrWhiteSpace(text):
                state.Items.Add(text.Trim()); break;
            case AdjustKind.RemoveItem when !string.IsNullOrWhiteSpace(text):
                state.Items.Remove(text.Trim()); break;
            default:
                return false;
        }
        return true;
    }

    // ---- Restart ---------------------------------------------------------
    public async Task<ServiceResult<TurnDto>> RestartAsync(string player, string? elixir, CancellationToken ct = default)
    {
        var acc = await _store.FindAsync(player, ct);
        if (acc is null) return ServiceResult<TurnDto>.Fail("Игрок не найден.");

        var chosen = acc.State.Elixir ?? ElixirType.Agility;
        if (!string.IsNullOrWhiteSpace(elixir) && Enum.TryParse<ElixirType>(elixir, true, out var parsed))
            chosen = parsed;

        var fresh = _factory.Create(chosen);
        await _store.SaveStateAsync(player, fresh, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(fresh));
    }

    // ---- Helpers ---------------------------------------------------------
    private void MoveTo(GameState state, int target, List<AutoStepDto>? log = null, int depth = 0)
    {
        state.ActiveCombat = null;
        var wasVisited = state.VisitedSections.Contains(target);
        state.CurrentSection = target;
        if (!wasVisited) state.VisitedSections.Add(target);

        var section = _sections.Get(target);
        if (section.IsVictory) { state.IsFinished = true; state.Outcome = "victory"; return; }
        if (section.IsDeath) { state.IsFinished = true; state.Outcome = "death"; return; }
        if (section.Combat is { Monsters.Count: > 0 }) { _combat.Begin(state, section.Combat, target); return; }

        // Apply the section's unconditional stat/gold/item effects automatically.
        foreach (var e in section.Effects)
        {
            if (Enum.TryParse<AdjustKind>(e.Kind, ignoreCase: true, out var kind)
                && ApplyAdjustment(state, kind, e.Text, e.Delta))
                log?.Add(EffectStep(state, target, kind, e.Delta, e.Text));
        }

        // Auto-resolve dice rolls / ССС / "visited before?" so the player only ever
        // makes real decisions. Chains through, guarded against pathological loops.
        if (section.Auto is not null && depth < MaxAutoDepth)
        {
            var step = ResolveAuto(state, section, wasVisited);
            if (step is not null)
            {
                log?.Add(step);
                if (step.Target is int next && next != target)
                    MoveTo(state, next, log, depth + 1);
            }
        }
    }

    /// <summary>Build a log entry describing an auto-applied effect and the resulting value.</summary>
    private static AutoStepDto EffectStep(GameState state, int section, AdjustKind kind, int delta, string? text)
    {
        var sign = delta >= 0 ? "+" : "−";
        var mag = Math.Abs(delta);
        var detail = kind switch
        {
            AdjustKind.Gold      => $"💰 Золото {sign}{mag} (→{state.Gold})",
            AdjustKind.Food      => $"🍖 Еда {sign}{mag} (→{state.Food})",
            AdjustKind.Agility   => $"🗡️ Ловкость {sign}{mag} (Л→{state.Agility.Current})",
            AdjustKind.Endurance => $"❤️ Выносливость {sign}{mag} (В→{state.Endurance.Current})",
            AdjustKind.Luck      => $"🍀 Счастье {sign}{mag} (С→{state.Luck.Current})",
            AdjustKind.AddItem   => $"🎒 Получено: {text}",
            AdjustKind.RemoveItem => $"🎒 Потеряно: {text}",
            _ => ""
        };
        return new AutoStepDto(section, "effect", "", detail, null);
    }

    /// <summary>Evaluate a section's auto-resolve rule; returns the step taken (with
    /// its target to forward to), or null to stay put and show the normal choices.</summary>
    private AutoStepDto? ResolveAuto(GameState state, SectionContent s, bool wasVisited)
    {
        var a = s.Auto!;
        switch (a.Kind)
        {
            case "dice":
            {
                var roll = _dice.Roll(a.DiceCount);
                var hit = a.Op == "lte" ? roll <= a.Value : roll >= a.Value;
                var cmp = a.Op == "lte" ? $"≤{a.Value}" : $"≥{a.Value}";
                var target = hit ? a.OnTrue : a.OnFalse;
                var detail = $"🎲 {a.DiceCount}К = {roll} ({(hit ? "" : "не ")}{cmp}) → раздел {target}";
                return new AutoStepDto(s.Id, "dice", s.Text, detail, target);
            }
            case "luck":
            {
                var luck = state.Luck.ToScore();
                var ok = _resolver.PerformLuckCheck(luck);
                state.Luck.From(luck);
                var target = ok ? a.OnSuccess : a.OnFail;
                var detail = $"🍀 ССС — {(ok ? "удача" : "неудача")} (С→{state.Luck.Current}) → раздел {target}";
                return new AutoStepDto(s.Id, "luck", s.Text, detail, target);
            }
            case "visited":
            {
                if (wasVisited)
                    return new AutoStepDto(s.Id, "visited", s.Text,
                        $"🔁 Ты уже был здесь → раздел {a.OnVisited}", a.OnVisited);
                if (a.OnFirst is int first)
                    return new AutoStepDto(s.Id, "visited", s.Text,
                        $"🆕 Впервые здесь → раздел {first}", first);
                return null; // first visit with real choices — stay and let the player decide
            }
            default:
                return null;
        }
    }

    private static void ApplyDelta(StoredAttribute attr, int delta)
    {
        var score = attr.ToScore();
        if (delta >= 0) score.Increase(delta); else score.Decrease(-delta);
        attr.From(score);
    }

    private TurnDto BuildTurn(GameState state, IReadOnlyList<RoundResultDto>? rounds = null,
        IReadOnlyList<AutoStepDto>? autoSteps = null)
    {
        var section = _sections.Get(state.CurrentSection);
        CombatStateDto? combatDto = state.ActiveCombat is null
            ? null
            : state.ActiveCombat.ToDto(rounds ?? []);
        return new TurnDto(section.ToDto(), state.ToDto(), combatDto, autoSteps ?? []);
    }
}

public enum AdjustKind { Gold, Food, Agility, Endurance, Luck, AddItem, RemoveItem }
