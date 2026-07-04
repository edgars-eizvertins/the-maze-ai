namespace Labyrinth.Application.State;

/// <summary>
/// Persisted state of an in-progress, round-by-round battle so it survives between
/// HTTP requests. A section may chain several monsters (e.g. §238, §277, §312).
/// </summary>
public sealed class CombatRunState
{
    public int SectionId { get; set; }
    public List<StoredMonster> Monsters { get; set; } = [];
    public int CurrentIndex { get; set; }
    public bool CanFlee { get; set; }
    public int? FleeSection { get; set; }
    public bool Finished { get; set; }
    public bool Won { get; set; }
    public int RoundCount { get; set; }

    public StoredMonster? Current =>
        CurrentIndex < Monsters.Count ? Monsters[CurrentIndex] : null;

    public int MonstersRemaining => Math.Max(0, Monsters.Count - CurrentIndex);
}

public sealed class StoredMonster
{
    public string Name { get; set; } = "";
    public int Agility { get; set; }
    public int EnduranceInitial { get; set; }
    public int EnduranceCurrent { get; set; }
}
