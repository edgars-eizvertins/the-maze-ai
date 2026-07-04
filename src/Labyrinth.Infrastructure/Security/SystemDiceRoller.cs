using System.Security.Cryptography;
using Labyrinth.Domain;

namespace Labyrinth.Infrastructure.Security;

/// <summary>Cryptographically-seeded six-sided dice. Registered as a singleton.</summary>
public sealed class SystemDiceRoller : IDiceRoller
{
    public int RollOne() => RandomNumberGenerator.GetInt32(1, 7);

    public int Roll(int count)
    {
        var sum = 0;
        for (var i = 0; i < count; i++) sum += RollOne();
        return sum;
    }
}
