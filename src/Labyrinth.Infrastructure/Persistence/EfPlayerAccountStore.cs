using System.Text.Json;
using Labyrinth.Application.Abstractions;
using Labyrinth.Application.State;
using Microsoft.EntityFrameworkCore;

namespace Labyrinth.Infrastructure.Persistence;

/// <summary>EF Core-backed <see cref="IPlayerAccountStore"/>; GameState is stored as JSON.</summary>
public sealed class EfPlayerAccountStore : IPlayerAccountStore
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;

    public EfPlayerAccountStore(AppDbContext db) => _db = db;

    public async Task<PlayerAccount?> FindAsync(string name, CancellationToken ct = default)
    {
        var row = await _db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Name == name, ct);
        if (row is null) return null;
        return new PlayerAccount
        {
            Name = row.Name,
            PinHash = row.PinHash,
            State = JsonSerializer.Deserialize<GameState>(row.StateJson, JsonOpts) ?? new GameState()
        };
    }

    public Task<bool> ExistsAsync(string name, CancellationToken ct = default) =>
        _db.Players.AnyAsync(p => p.Name == name, ct);

    public async Task AddAsync(PlayerAccount account, CancellationToken ct = default)
    {
        _db.Players.Add(new PlayerEntity
        {
            Name = account.Name,
            PinHash = account.PinHash,
            StateJson = JsonSerializer.Serialize(account.State, JsonOpts),
            CurrentSection = account.State.CurrentSection,
            IsFinished = account.State.IsFinished,
            Outcome = account.State.Outcome,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveStateAsync(string name, GameState state, CancellationToken ct = default)
    {
        var row = await _db.Players.FirstOrDefaultAsync(p => p.Name == name, ct)
                  ?? throw new InvalidOperationException($"Player '{name}' not found.");
        row.StateJson = JsonSerializer.Serialize(state, JsonOpts);
        row.CurrentSection = state.CurrentSection;
        row.IsFinished = state.IsFinished;
        row.Outcome = state.Outcome;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
