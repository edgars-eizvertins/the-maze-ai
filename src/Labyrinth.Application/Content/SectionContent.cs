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
