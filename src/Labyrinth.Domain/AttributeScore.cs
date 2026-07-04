namespace Labyrinth.Domain;

/// <summary>
/// An attribute that tracks both its initial (maximum) and current value.
/// Per the rulebook, an attribute may never exceed its initial level — except
/// the Luck elixir, which is allowed to overshoot by exactly one point.
/// </summary>
public sealed class AttributeScore
{
    public int Initial { get; private set; }
    public int Current { get; private set; }

    public AttributeScore(int initial, int? current = null)
    {
        Initial = initial;
        Current = current ?? initial;
    }

    /// <summary>Reduce the current value, never dropping below the death threshold.</summary>
    public void Decrease(int amount)
    {
        Current = Math.Max(GameRules.DeathThreshold, Current - amount);
    }

    /// <summary>
    /// Increase the current value, capped at the initial level.
    /// <paramref name="overshoot"/> allows the Luck-elixir exception (+1 above initial).
    /// </summary>
    public void Increase(int amount, int overshoot = 0)
    {
        Current = Math.Min(Initial + overshoot, Current + amount);
    }

    /// <summary>Restore to the initial level (used by elixirs of Agility/Endurance).</summary>
    public void RestoreToInitial() => Current = Initial;

    /// <summary>Restore Luck to its initial level, allowed to exceed it by one point.</summary>
    public void RestoreLuck() => Current = Initial + GameRules.LuckElixirOvershoot;

    public bool IsDepleted => Current <= GameRules.DeathThreshold;

    public override string ToString() => $"{Current}/{Initial}";
}
