using Labyrinth.Application.Services;
using Labyrinth.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Labyrinth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Register a new player (name + PIN + chosen elixir). Rolls starting attributes.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        return result.Success ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Log in an existing player.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        return result.Success ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }
}
