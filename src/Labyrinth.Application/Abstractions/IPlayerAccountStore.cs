using Labyrinth.Application.State;

namespace Labyrinth.Application.Abstractions;

/// <summary>A player's account plus their single saved playthrough.</summary>
public sealed class PlayerAccount
{
    public string Name { get; set; } = "";
    public string PinHash { get; set; } = "";
    public GameState State { get; set; } = new();
}

/// <summary>Persistence port for player accounts/saves (implemented by Infrastructure/EF).</summary>
public interface IPlayerAccountStore
{
    Task<PlayerAccount?> FindAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsAsync(string name, CancellationToken ct = default);
    Task AddAsync(PlayerAccount account, CancellationToken ct = default);
    Task SaveStateAsync(string name, GameState state, CancellationToken ct = default);
}
