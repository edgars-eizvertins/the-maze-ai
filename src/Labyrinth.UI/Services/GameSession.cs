using Labyrinth.Shared;

namespace Labyrinth.UI.Services;

/// <summary>
/// Holds the current turn and player identity for the running session, and exposes
/// game actions. Components subscribe to <see cref="OnChange"/> to re-render.
/// </summary>
public sealed class GameSession
{
    private readonly ApiClient _api;
    private readonly TokenStore _tokens;

    public GameSession(ApiClient api, TokenStore tokens)
    {
        _api = api;
        _tokens = tokens;
    }

    public string? PlayerName { get; private set; }
    public TurnDto? Turn { get; private set; }
    public string? LastError { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool Busy { get; private set; }

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    /// <summary>Restore a session from a saved token (called on app start).</summary>
    public async Task<bool> TryRestoreAsync()
    {
        var token = await _tokens.GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;
        PlayerName = await _tokens.GetNameAsync();
        IsAuthenticated = true;
        await RefreshAsync();
        return Turn is not null;
    }

    public async Task<bool> RegisterAsync(string name, string pin, string elixir)
    {
        var (data, error) = await _api.RegisterAsync(new RegisterRequest(name, pin, elixir));
        return await AfterAuthAsync(data, error);
    }

    public async Task<bool> LoginAsync(string name, string pin)
    {
        var (data, error) = await _api.LoginAsync(new LoginRequest(name, pin));
        return await AfterAuthAsync(data, error);
    }

    public async Task LogoutAsync()
    {
        await _tokens.ClearAsync();
        PlayerName = null; Turn = null; IsAuthenticated = false;
        Notify();
    }

    // ---- Game actions ----
    public Task RefreshAsync() => RunAsync(() => _api.GetTurnAsync());
    public Task ChooseAsync(int target) => RunAsync(() => _api.ChooseAsync(target));
    public Task GoToAsync(int target) => RunAsync(() => _api.GoToAsync(target));
    public Task CombatRoundAsync(bool useLuck) => RunAsync(() => _api.CombatRoundAsync(useLuck));
    public Task FleeAsync() => RunAsync(() => _api.FleeAsync());
    public Task EatAsync() => RunAsync(() => _api.EatAsync());
    public Task ElixirAsync() => RunAsync(() => _api.ElixirAsync());
    public Task AdjustAsync(string kind, int delta = 0, string? text = null)
        => RunAsync(() => _api.AdjustAsync(kind, delta, text));
    public Task RestartAsync(string? elixir) => RunAsync(() => _api.RestartAsync(elixir));

    private async Task<bool> AfterAuthAsync(AuthResponse? data, string? error)
    {
        if (data is null) { LastError = error; Notify(); return false; }
        await _tokens.SaveAsync(data.Token, data.Name);
        PlayerName = data.Name;
        IsAuthenticated = true;
        LastError = null;
        await RefreshAsync();
        return true;
    }

    private async Task RunAsync(Func<Task<(TurnDto? data, string? error)>> action)
    {
        Busy = true; LastError = null; Notify();
        var (data, error) = await action();
        if (data is not null) Turn = data;
        else LastError = error;
        Busy = false; Notify();
    }
}
