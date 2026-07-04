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
    private readonly ISectionRepository _sections;
    private readonly IPlayerAccountStore _store;
    private readonly CombatService _combat;
    private readonly GameStateFactory _factory;

    public GameService(ISectionRepository sections, IPlayerAccountStore store,
        CombatService combat, GameStateFactory factory)
    {
        _sections = sections;
        _store = store;
        _combat = combat;
        _factory = factory;
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

        MoveTo(state, target);
        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state));
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

        if (!state.IsFinished) MoveTo(state, flee.Value);
        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state));
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

        switch (kind)
        {
            case AdjustKind.Gold:
                state.Gold = Math.Max(0, state.Gold + delta); break;
            case AdjustKind.Food:
                state.Food = Math.Max(0, state.Food + delta); break;
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
                return ServiceResult<TurnDto>.Fail("Нечего изменять.");
        }

        await _store.SaveStateAsync(player, state, ct);
        return ServiceResult<TurnDto>.Ok(BuildTurn(state));
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
    private void MoveTo(GameState state, int target)
    {
        state.ActiveCombat = null;
        state.CurrentSection = target;
        if (!state.VisitedSections.Contains(target))
            state.VisitedSections.Add(target);

        var section = _sections.Get(target);
        if (section.IsVictory) { state.IsFinished = true; state.Outcome = "victory"; }
        else if (section.IsDeath) { state.IsFinished = true; state.Outcome = "death"; }
        else if (section.Combat is { Monsters.Count: > 0 })
            _combat.Begin(state, section.Combat, target);
    }

    private static void ApplyDelta(StoredAttribute attr, int delta)
    {
        var score = attr.ToScore();
        if (delta >= 0) score.Increase(delta); else score.Decrease(-delta);
        attr.From(score);
    }

    private TurnDto BuildTurn(GameState state, IReadOnlyList<RoundResultDto>? rounds = null)
    {
        var section = _sections.Get(state.CurrentSection);
        CombatStateDto? combatDto = state.ActiveCombat is null
            ? null
            : state.ActiveCombat.ToDto(rounds ?? []);
        return new TurnDto(section.ToDto(), state.ToDto(), combatDto);
    }
}

public enum AdjustKind { Gold, Food, Agility, Endurance, Luck, AddItem, RemoveItem }
