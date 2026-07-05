using Labyrinth.Application.Content;
using Labyrinth.Application.State;
using Labyrinth.Domain;
using Labyrinth.Shared;

namespace Labyrinth.Application.Services;

/// <summary>
/// Drives interactive, round-by-round battles on top of the pure
/// <see cref="CombatResolver"/>. Operates directly on the player's
/// <see cref="GameState.ActiveCombat"/> so progress survives between requests.
/// </summary>
public sealed class CombatService
{
    private readonly CombatResolver _resolver;

    public CombatService(CombatResolver resolver) => _resolver = resolver;

    /// <summary>Initialise a fresh battle from a section's combat content. <paramref
    /// name="victorySection"/> is the "continue here after winning" target for detour
    /// fights with no printed exit (§238); null for ordinary battles.</summary>
    public void Begin(GameState state, CombatContent combat, int sectionId, int? victorySection = null)
    {
        state.ActiveCombat = new CombatRunState
        {
            SectionId = sectionId,
            CanFlee = combat.CanFlee,
            FleeSection = combat.FleeSection,
            VictorySection = victorySection,
            Monsters = combat.Monsters.Select(m => new StoredMonster
            {
                Name = m.Name,
                Agility = m.Agility,
                EnduranceInitial = m.Endurance,
                EnduranceCurrent = m.Endurance
            }).ToList()
        };
    }

    /// <summary>Resolve one round. Returns the round result, or null if no battle is active.</summary>
    public RoundResult? PlayRound(GameState state, bool useLuck)
    {
        var combat = state.ActiveCombat;
        if (combat is null || combat.Finished) return null;
        var sm = combat.Current;
        if (sm is null) return null;

        // Bridge persisted snapshots into domain objects for the pure resolver.
        var monster = new Monster(sm.Name, sm.Agility, sm.EnduranceCurrent);
        var heroEnd = state.Endurance.ToScore();
        var heroLuck = state.Luck.ToScore();

        var result = _resolver.ResolveRound(
            heroAgility: state.Agility.Current,
            heroEndurance: heroEnd,
            heroLuck: heroLuck,
            monster: monster,
            useLuck: useLuck);

        // Persist the mutated values back.
        state.Endurance.From(heroEnd);
        state.Luck.From(heroLuck);
        sm.EnduranceCurrent = monster.Endurance.Current;
        combat.RoundCount++;

        // Death of the hero ends everything.
        if (state.Endurance.Current <= GameRules.DeathThreshold)
        {
            combat.Finished = true;
            combat.Won = false;
            state.IsFinished = true;
            state.Outcome = "death";
            return result;
        }

        // Monster down → advance to the next in the queue (if any).
        if (monster.IsDefeated)
        {
            combat.CurrentIndex++;
            if (combat.Current is null)
            {
                combat.Finished = true;
                combat.Won = true;
            }
        }

        return result;
    }

    /// <summary>Flee the battle: −2 endurance (rulebook §3), then route to the flee section.</summary>
    public ServiceResult<int> Flee(GameState state)
    {
        var combat = state.ActiveCombat;
        if (combat is null || combat.Finished)
            return ServiceResult<int>.Fail("Сейчас нет битвы.");
        if (!combat.CanFlee || combat.FleeSection is null)
            return ServiceResult<int>.Fail("Бегство в этой битве невозможно.");

        state.Endurance.From(Wound(state.Endurance, GameRules.FleePenalty));
        combat.Finished = true;
        combat.Won = false;

        if (state.Endurance.Current <= GameRules.DeathThreshold)
        {
            state.IsFinished = true;
            state.Outcome = "death";
        }
        return ServiceResult<int>.Ok(combat.FleeSection.Value);
    }

    private static AttributeScore Wound(StoredAttribute a, int amount)
    {
        var score = a.ToScore();
        score.Decrease(amount);
        return score;
    }
}
