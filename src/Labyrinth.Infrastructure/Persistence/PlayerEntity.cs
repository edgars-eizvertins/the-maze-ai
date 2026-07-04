namespace Labyrinth.Infrastructure.Persistence;

/// <summary>EF Core row for a player: account + their serialized save state.</summary>
public sealed class PlayerEntity
{
    public string Name { get; set; } = "";          // primary key
    public string PinHash { get; set; } = "";
    public string StateJson { get; set; } = "{}";    // serialized GameState

    // Denormalised columns for cheap querying / future leaderboards.
    public int CurrentSection { get; set; }
    public bool IsFinished { get; set; }
    public string? Outcome { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
