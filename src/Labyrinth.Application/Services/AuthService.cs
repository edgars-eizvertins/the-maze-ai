using Labyrinth.Application.Abstractions;
using Labyrinth.Application.State;
using Labyrinth.Domain;
using Labyrinth.Shared;

namespace Labyrinth.Application.Services;

/// <summary>Registration &amp; login. New players get rolled attributes and a fresh save at §1.</summary>
public sealed class AuthService
{
    private readonly IPlayerAccountStore _store;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly GameStateFactory _games;

    public AuthService(IPlayerAccountStore store, IPasswordHasher hasher,
        ITokenService tokens, GameStateFactory games)
    {
        _store = store;
        _hasher = hasher;
        _tokens = tokens;
        _games = games;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var name = req.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 32)
            return ServiceResult<AuthResponse>.Fail("Имя должно быть от 2 до 32 символов.");
        if (string.IsNullOrWhiteSpace(req.Pin) || req.Pin.Length < 4)
            return ServiceResult<AuthResponse>.Fail("PIN должен быть не короче 4 символов.");
        if (!Enum.TryParse<ElixirType>(req.Elixir, ignoreCase: true, out var elixir))
            return ServiceResult<AuthResponse>.Fail("Неверный выбор эликсира.");
        if (await _store.ExistsAsync(name, ct))
            return ServiceResult<AuthResponse>.Fail("Игрок с таким именем уже существует.");

        var account = new PlayerAccount
        {
            Name = name,
            PinHash = _hasher.Hash(req.Pin),
            State = _games.Create(elixir)
        };
        await _store.AddAsync(account, ct);

        return ServiceResult<AuthResponse>.Ok(
            new AuthResponse(_tokens.CreateToken(name), name, HasActiveGame: true));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var name = req.Name?.Trim() ?? "";
        var account = await _store.FindAsync(name, ct);
        if (account is null || !_hasher.Verify(req.Pin ?? "", account.PinHash))
            return ServiceResult<AuthResponse>.Fail("Неверное имя или PIN.");

        return ServiceResult<AuthResponse>.Ok(
            new AuthResponse(_tokens.CreateToken(name), name,
                HasActiveGame: account.State.IsStarted && !account.State.IsFinished));
    }
}
