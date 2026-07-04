using System.Security.Claims;
using Labyrinth.Application;
using Labyrinth.Application.Services;
using Labyrinth.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Labyrinth.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class GameController : ControllerBase
{
    private readonly GameService _game;
    public GameController(GameService game) => _game = game;

    private string Player => User.FindFirstValue(ClaimTypes.Name)
        ?? throw new InvalidOperationException("Missing player identity.");

    private ActionResult<TurnDto> Respond(ServiceResult<TurnDto> r) =>
        r.Success ? Ok(r.Value) : BadRequest(new { error = r.Error });

    /// <summary>Current section + full player state (+ combat if any).</summary>
    [HttpGet]
    public async Task<ActionResult<TurnDto>> Current(CancellationToken ct)
        => Respond(await _game.GetTurnAsync(Player, ct));

    /// <summary>Pick a numbered choice from the current section.</summary>
    [HttpPost("choose")]
    public async Task<ActionResult<TurnDto>> Choose(ChooseRequest req, CancellationToken ct)
        => Respond(await _game.ChooseAsync(Player, req.Target, ct));

    /// <summary>Resolve one combat round (optionally performing ССС).</summary>
    [HttpPost("combat/round")]
    public async Task<ActionResult<TurnDto>> CombatRound(CombatRoundRequest req, CancellationToken ct)
        => Respond(await _game.CombatRoundAsync(Player, req.UseLuck, ct));

    /// <summary>Flee the current battle (where the rules permit).</summary>
    [HttpPost("combat/flee")]
    public async Task<ActionResult<TurnDto>> Flee(CancellationToken ct)
        => Respond(await _game.FleeAsync(Player, ct));

    /// <summary>Eat one ration (+4 endurance) where the section allows it.</summary>
    [HttpPost("eat")]
    public async Task<ActionResult<TurnDto>> Eat(CancellationToken ct)
        => Respond(await _game.EatAsync(Player, ct));

    /// <summary>Drink the carried elixir (max twice per game).</summary>
    [HttpPost("elixir")]
    public async Task<ActionResult<TurnDto>> Elixir(CancellationToken ct)
        => Respond(await _game.UseElixirAsync(Player, ct));

    /// <summary>Apply a manual narrative adjustment the book asks the reader to make.</summary>
    [HttpPost("adjust")]
    public async Task<ActionResult<TurnDto>> Adjust(AdjustRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<AdjustKind>(req.Kind, ignoreCase: true, out var kind))
            return BadRequest(new { error = "Неизвестный тип изменения." });
        return Respond(await _game.AdjustAsync(Player, kind, req.Text, req.Delta, ct));
    }

    /// <summary>Start the whole adventure over (optionally choosing a new elixir).</summary>
    [HttpPost("restart")]
    public async Task<ActionResult<TurnDto>> Restart(RestartRequest req, CancellationToken ct)
        => Respond(await _game.RestartAsync(Player, req.Elixir, ct));
}

public record AdjustRequest(string Kind, string? Text, int Delta);
public record RestartRequest(string? Elixir);
