using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Labyrinth.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Labyrinth.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "labyrinth";
    public string Audience { get; set; } = "labyrinth";
    public int ExpiryHours { get; set; } = 720; // 30 days
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opts;
    public JwtTokenService(JwtOptions opts) => _opts = opts;

    public string CreateToken(string playerName)
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: [new Claim(ClaimTypes.Name, playerName)],
            expires: DateTime.UtcNow.AddHours(_opts.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
