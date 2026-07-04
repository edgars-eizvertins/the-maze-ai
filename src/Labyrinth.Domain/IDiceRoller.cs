namespace Labyrinth.Domain;

/// <summary>
/// Abstraction over dice so combat/luck logic stays deterministic and testable
/// (SOLID: Dependency Inversion). Infrastructure supplies a real RNG; tests
/// supply a scripted sequence.
/// </summary>
public interface IDiceRoller
{
    /// <summary>Roll a single six-sided die (1К) → 1..6.</summary>
    int RollOne();

    /// <summary>Roll <paramref name="count"/> dice and return the sum (e.g. 2К).</summary>
    int Roll(int count);
}
