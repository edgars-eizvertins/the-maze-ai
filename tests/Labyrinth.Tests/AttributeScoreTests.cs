using Labyrinth.Domain;
using Xunit;

namespace Labyrinth.Tests;

public class AttributeScoreTests
{
    [Fact]
    public void Increase_IsCappedAtInitialLevel()
    {
        var a = new AttributeScore(initial: 10, current: 8);
        a.Increase(5);                      // +4 food etc.
        Assert.Equal(10, a.Current);        // never above initial
    }

    [Fact]
    public void Decrease_NeverDropsBelowZero()
    {
        var a = new AttributeScore(10, 1);
        a.Decrease(5);
        Assert.Equal(0, a.Current);
        Assert.True(a.IsDepleted);
    }

    [Fact]
    public void RestoreLuck_MayExceedInitialByOne()
    {
        var luck = new AttributeScore(9, 3);
        luck.RestoreLuck();
        Assert.Equal(10, luck.Current);     // initial + 1 (luck elixir exception)
    }

    [Fact]
    public void RestoreToInitial_SetsCurrentToInitial()
    {
        var a = new AttributeScore(12, 4);
        a.RestoreToInitial();
        Assert.Equal(12, a.Current);
    }
}
