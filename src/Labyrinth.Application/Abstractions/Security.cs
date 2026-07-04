namespace Labyrinth.Application.Abstractions;

/// <summary>Hashes and verifies player PINs (implemented with PBKDF2 in Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string pin);
    bool Verify(string pin, string hash);
}

/// <summary>Issues bearer tokens identifying a player by name.</summary>
public interface ITokenService
{
    string CreateToken(string playerName);
}
