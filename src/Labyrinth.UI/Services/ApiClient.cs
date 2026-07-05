using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Labyrinth.Shared;

namespace Labyrinth.UI.Services;

/// <summary>Typed wrapper over the game HTTP API. Returns (data, error) tuples.</summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;

    public ApiClient(HttpClient http, TokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    private async Task ApplyAuthAsync()
    {
        var token = await _tokens.GetTokenAsync();
        _http.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    // ---- Auth ----
    public Task<(AuthResponse? data, string? error)> RegisterAsync(RegisterRequest req) =>
        PostAsync<AuthResponse>("api/auth/register", req, auth: false);

    public Task<(AuthResponse? data, string? error)> LoginAsync(LoginRequest req) =>
        PostAsync<AuthResponse>("api/auth/login", req, auth: false);

    // ---- Game ----
    public async Task<(TurnDto? data, string? error)> GetTurnAsync()
    {
        await ApplyAuthAsync();
        try
        {
            var resp = await _http.GetAsync("api/game");
            return await ReadAsync<TurnDto>(resp);
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public Task<(TurnDto? data, string? error)> ChooseAsync(int target) =>
        PostAsync<TurnDto>("api/game/choose", new ChooseRequest(target));

    public Task<(TurnDto? data, string? error)> GoToAsync(int target) =>
        PostAsync<TurnDto>("api/game/goto", new GotoRequest(target));

    public Task<(TurnDto? data, string? error)> CombatRoundAsync(bool useLuck) =>
        PostAsync<TurnDto>("api/game/combat/round", new CombatRoundRequest(useLuck));

    public Task<(TurnDto? data, string? error)> FleeAsync() =>
        PostAsync<TurnDto>("api/game/combat/flee", new { });

    public Task<(TurnDto? data, string? error)> EatAsync() =>
        PostAsync<TurnDto>("api/game/eat", new { });

    public Task<(TurnDto? data, string? error)> ElixirAsync() =>
        PostAsync<TurnDto>("api/game/elixir", new { });

    public Task<(TurnDto? data, string? error)> AdjustAsync(string kind, int delta = 0, string? text = null) =>
        PostAsync<TurnDto>("api/game/adjust", new { Kind = kind, Text = text, Delta = delta });

    public Task<(TurnDto? data, string? error)> RestartAsync(string? elixir) =>
        PostAsync<TurnDto>("api/game/restart", new { Elixir = elixir });

    // ---- Plumbing ----
    private async Task<(T? data, string? error)> PostAsync<T>(string url, object body, bool auth = true)
    {
        if (auth) await ApplyAuthAsync();
        try
        {
            var resp = await _http.PostAsJsonAsync(url, body);
            return await ReadAsync<T>(resp);
        }
        catch (Exception ex) { return (default, ex.Message); }
    }

    private static async Task<(T? data, string? error)> ReadAsync<T>(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode)
            return (await resp.Content.ReadFromJsonAsync<T>(), null);

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            return (default, "Требуется вход в игру.");

        try
        {
            var err = await resp.Content.ReadFromJsonAsync<ErrorDto>();
            return (default, err?.Error ?? $"Ошибка {(int)resp.StatusCode}.");
        }
        catch { return (default, $"Ошибка {(int)resp.StatusCode}."); }
    }

    private sealed record ErrorDto(string Error);
}
