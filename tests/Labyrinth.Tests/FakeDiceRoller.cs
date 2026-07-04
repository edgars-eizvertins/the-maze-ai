using Labyrinth.Domain;

namespace Labyrinth.Tests;

/// <summary>Deterministic dice that serve a scripted queue of values.</summary>
public sealed class FakeDiceRoller : IDiceRoller
{
    private readonly Queue<int> _values;
    public FakeDiceRoller(params int[] values) => _values = new Queue<int>(values);

    public int RollOne() => _values.Dequeue();
    public int Roll(int count)
    {
        var sum = 0;
        for (var i = 0; i < count; i++) sum += _values.Dequeue();
        return sum;
    }
}
