using Labyrinth.Domain;

namespace Labyrinth.Application.State;

/// <summary>
/// The full mutable run-state for one player's playthrough. Serialized to JSON and
/// stored in SQLite. Kept persistence-ignorant (plain properties) so it can be
/// round-tripped without EF leaking into the Application layer.
/// </summary>
public sealed class GameState
{
    public int CurrentSection { get; set; } = 1;
    public bool IsStarted { get; set; }
    public bool IsFinished { get; set; }
    public string? Outcome { get; set; }   // "victory" | "death" | null

    public StoredAttribute Agility { get; set; } = new();
    public StoredAttribute Endurance { get; set; } = new();
    public StoredAttribute Luck { get; set; } = new();

    public int Gold { get; set; }
    public int Food { get; set; } = GameRules.StartingFood;

    public ElixirType? Elixir { get; set; }
    public int ElixirUsesLeft { get; set; } = GameRules.MaxElixirUses;

    public List<string> Equipment { get; set; } = ["меч", "щит", "фонарь"];
    public List<string> Items { get; set; } = [];
    public List<int> VisitedSections { get; set; } = [];

    public CombatRunState? ActiveCombat { get; set; }

    public bool InCombat => ActiveCombat is { Finished: false };
}

/// <summary>JSON-friendly snapshot of an <see cref="AttributeScore"/>.</summary>
public sealed class StoredAttribute
{
    public int Initial { get; set; }
    public int Current { get; set; }

    public StoredAttribute() { }
    public StoredAttribute(int initial, int current) { Initial = initial; Current = current; }

    public AttributeScore ToScore() => new(Initial, Current);
    public void From(AttributeScore s) { Initial = s.Initial; Current = s.Current; }
}
