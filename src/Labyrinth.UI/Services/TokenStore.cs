using Microsoft.JSInterop;

namespace Labyrinth.UI.Services;

/// <summary>Persists the JWT and player name in browser localStorage.</summary>
public sealed class TokenStore
{
    private const string TokenKey = "labyrinth.token";
    private const string NameKey = "labyrinth.name";
    private readonly IJSRuntime _js;

    public TokenStore(IJSRuntime js) => _js = js;

    public async Task<string?> GetTokenAsync() =>
        await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    public async Task<string?> GetNameAsync() =>
        await _js.InvokeAsync<string?>("localStorage.getItem", NameKey);

    public async Task SaveAsync(string token, string name)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await _js.InvokeVoidAsync("localStorage.setItem", NameKey, name);
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", NameKey);
    }
}
