namespace Labyrinth.Application.Content;

/// <summary>Static, read-only game content for a single numbered section (from game_data.json).</summary>
public sealed class SectionContent
{
    public int Id { get; init; }
    public string Text { get; init; } = "";
    public IReadOnlyList<ChoiceContent> Choices { get; init; } = [];
    public CombatContent? Combat { get; init; }
    public bool HasLuckCheck { get; init; }
    public bool CanEat { get; init; }
    public bool IsVictory { get; init; }
    public bool IsDeath { get; init; }

    /// <summary>
    /// Optional "resolve on arrival" rule the engine evaluates itself so the player
    /// never has to roll dice, perform ССС or answer "were you here before?" by hand.
    /// When present (and the section isn't combat/victory/death) the engine rolls /
    /// checks state, picks the branch and forwards the player automatically.
    /// </summary>
    public AutoResolveContent? Auto { get; init; }

    /// <summary>
    /// Unconditional stat/gold/item changes the book states plainly ("Отними 1 балл
    /// выносливости", "получаешь 5 кусков золота", "(+2С)") — applied automatically on
    /// arrival. Only effects with no "если"/dice condition are encoded here; conditional
    /// ones stay on the manual controls.
    /// </summary>
    public IReadOnlyList<EffectContent> Effects { get; init; } = [];
}

/// <summary>One automatic stat/gold/item change applied when a section is entered.</summary>
public sealed class EffectContent
{
    /// <summary>agility | endurance | luck | gold | food | addItem | removeItem.</summary>
    public string Kind { get; init; } = "";
    public int Delta { get; init; }
    public string? Text { get; init; }
}

/// <summary>A branch the engine resolves automatically on entering a section.</summary>
public sealed class AutoResolveContent
{
    /// <summary>"dice" | "luck" | "visited".</summary>
    public string Kind { get; init; } = "";

    // dice: roll <DiceCount>К, then compare against <Value> using <Op> ("gte"|"lte").
    public int DiceCount { get; init; }
    public string Op { get; init; } = "";
    public int Value { get; init; }
    public int? OnTrue { get; init; }
    public int? OnFalse { get; init; }

    // luck (ССС): roll, apply −1 luck, branch on success/failure.
    public int? OnSuccess { get; init; }
    public int? OnFail { get; init; }

    // visited: branch on whether this section was already visited before this arrival.
    // OnFirst == null means "stay here and show the section's normal choices".
    public int? OnVisited { get; init; }
    public int? OnFirst { get; init; }
}

public sealed class ChoiceContent
{
    public int Target { get; init; }
    public string Label { get; init; } = "";
}

public sealed class CombatContent
{
    public IReadOnlyList<MonsterContent> Monsters { get; init; } = [];
    public bool CanFlee { get; init; }
    public int? FleeSection { get; init; }
}

public sealed class MonsterContent
{
    public string Name { get; init; } = "";
    public int Agility { get; init; }
    public int Endurance { get; init; }
}
