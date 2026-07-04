using Labyrinth.Application.Content;
using Labyrinth.Application.State;
using Labyrinth.Domain;
using Labyrinth.Shared;

namespace Labyrinth.Application.Mapping;

/// <summary>Maps internal run-state/content to the wire DTOs in Labyrinth.Shared.</summary>
public static class DtoMapper
{
    public static GameStateDto ToDto(this GameState s) => new(
        CurrentSection: s.CurrentSection,
        IsStarted: s.IsStarted,
        IsFinished: s.IsFinished,
        Outcome: s.Outcome,
        Agility: new AttributeDto(s.Agility.Initial, s.Agility.Current),
        Endurance: new AttributeDto(s.Endurance.Initial, s.Endurance.Current),
        Luck: new AttributeDto(s.Luck.Initial, s.Luck.Current),
        Gold: s.Gold,
        Food: s.Food,
        ElixirType: s.Elixir?.ToString(),
        ElixirUsesLeft: s.ElixirUsesLeft,
        Equipment: s.Equipment.AsReadOnly(),
        Items: s.Items.AsReadOnly(),
        VisitedSections: s.VisitedSections.AsReadOnly(),
        InCombat: s.InCombat);

    public static SectionDto ToDto(this SectionContent c) => new(
        Id: c.Id,
        Text: c.Text,
        Choices: c.Choices.Select(ch => new ChoiceDto(ch.Target, ch.Label)).ToList(),
        CanEat: c.CanEat,
        HasLuckCheck: c.HasLuckCheck,
        IsVictory: c.IsVictory,
        IsDeath: c.IsDeath,
        HasCombat: c.Combat is not null);

    public static CombatStateDto ToDto(this CombatRunState combat, IReadOnlyList<RoundResultDto> rounds)
    {
        var m = combat.Current;
        return new CombatStateDto(
            MonsterName: m?.Name ?? "",
            MonsterAgility: m?.Agility ?? 0,
            MonsterEnduranceCurrent: m?.EnduranceCurrent ?? 0,
            MonsterEnduranceMax: m?.EnduranceInitial ?? 0,
            MonstersRemaining: combat.MonstersRemaining,
            CanFlee: combat.CanFlee,
            FleeSection: combat.FleeSection,
            Finished: combat.Finished,
            Won: combat.Won,
            Rounds: rounds);
    }

    public static RoundResultDto ToDto(this RoundResult r) => new(
        MonsterDice: r.MonsterDice,
        MonsterAttack: r.MonsterAttack,
        PlayerDice: r.PlayerDice,
        PlayerAttack: r.PlayerAttack,
        Outcome: r.Outcome switch
        {
            RoundOutcome.PlayerHit => "player_hit",
            RoundOutcome.MonsterHit => "monster_hit",
            _ => "tie"
        },
        LuckUsed: r.LuckUsed,
        LuckSucceeded: r.LuckSucceeded,
        DamageToMonster: r.DamageToMonster,
        DamageToPlayer: r.DamageToPlayer,
        MonsterName: r.MonsterName,
        MonsterEnduranceAfter: r.MonsterEnduranceAfter,
        PlayerEnduranceAfter: r.PlayerEnduranceAfter);
}
