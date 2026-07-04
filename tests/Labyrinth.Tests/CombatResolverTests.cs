using Labyrinth.Domain;
using Xunit;

namespace Labyrinth.Tests;

public class CombatResolverTests
{
    // Helper: hero with given agility/endurance/luck.
    private static (AttributeScore end, AttributeScore luck) Hero(int endurance = 20, int luck = 9)
        => (new AttributeScore(endurance), new AttributeScore(luck));

    [Fact]
    public void PlayerWins_NoLuck_MonsterLosesTwo()
    {
        // monster 2K = 1+1=2 (+6 Л = 8); player 2K = 6+6=12 (+8 Л = 20) → player wins
        var dice = new FakeDiceRoller(1, 1, 6, 6);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero();
        var monster = new Monster("ОРК", agility: 6, endurance: 4);

        var r = resolver.ResolveRound(heroAgility: 8, end, luck, monster, useLuck: false);

        Assert.Equal(RoundOutcome.PlayerHit, r.Outcome);
        Assert.Equal(2, r.DamageToMonster);
        Assert.Equal(2, monster.Endurance.Current);  // 4 - 2
        Assert.Equal(20, end.Current);               // unharmed
    }

    [Fact]
    public void MonsterWins_NoLuck_PlayerLosesTwo()
    {
        // monster 6+6=12 (+10 = 22); player 1+1=2 (+8 = 10) → monster wins
        var dice = new FakeDiceRoller(6, 6, 1, 1);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero(endurance: 20);
        var monster = new Monster("ГОБЛИН", 10, 10);

        var r = resolver.ResolveRound(8, end, luck, monster, useLuck: false);

        Assert.Equal(RoundOutcome.MonsterHit, r.Outcome);
        Assert.Equal(2, r.DamageToPlayer);
        Assert.Equal(18, end.Current);
    }

    [Fact]
    public void Tie_RepeatsWithNoDamage()
    {
        // both 3+3=6; +equal agility → equal attack
        var dice = new FakeDiceRoller(3, 3, 4, 2);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero();
        var monster = new Monster("X", 8, 8);

        var r = resolver.ResolveRound(8, end, luck, monster, useLuck: false);

        Assert.Equal(RoundOutcome.Tie, r.Outcome);
        Assert.Equal(0, r.DamageToMonster);
        Assert.Equal(0, r.DamageToPlayer);
    }

    [Fact]
    public void PlayerWins_WithLuck_Success_MonsterLosesFour()
    {
        // attack rolls: monster 1+1, player 6+6 → player wins.
        // luck check rolls: 1+1 (equal → success). Lucky wound to monster = -4.
        var dice = new FakeDiceRoller(1, 1, 6, 6, 1, 1);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero(luck: 9);
        var monster = new Monster("ОРК", 6, 6);

        var r = resolver.ResolveRound(8, end, luck, monster, useLuck: true);

        Assert.True(r.LuckUsed);
        Assert.True(r.LuckSucceeded);
        Assert.Equal(4, r.DamageToMonster);
        Assert.Equal(2, monster.Endurance.Current);   // 6 - 4
        Assert.Equal(8, luck.Current);                // 9 - 1 (cost of ССС)
    }

    [Fact]
    public void PlayerWins_WithLuck_Fail_MonsterLosesOne()
    {
        // luck check 6+5=11 > luck 7 and not equal → fail. Unlucky wound to monster = -1.
        var dice = new FakeDiceRoller(1, 1, 6, 6, 6, 5);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero(luck: 7);
        var monster = new Monster("ОРК", 6, 6);

        var r = resolver.ResolveRound(8, end, luck, monster, useLuck: true);

        Assert.True(r.LuckUsed);
        Assert.False(r.LuckSucceeded);
        Assert.Equal(1, r.DamageToMonster);
        Assert.Equal(5, monster.Endurance.Current);   // 6 - 1
        Assert.Equal(6, luck.Current);                // 7 - 1
    }

    [Fact]
    public void MonsterWins_WithLuck_Success_PlayerLosesOne()
    {
        // monster wins; luck success (2+2 sum 4 <= luck 9) → hero wound reduced to -1
        var dice = new FakeDiceRoller(6, 6, 1, 1, 2, 2);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero(endurance: 20, luck: 9);
        var monster = new Monster("ГОБЛИН", 10, 10);

        var r = resolver.ResolveRound(8, end, luck, monster, useLuck: true);

        Assert.Equal(RoundOutcome.MonsterHit, r.Outcome);
        Assert.True(r.LuckSucceeded);
        Assert.Equal(1, r.DamageToPlayer);
        Assert.Equal(19, end.Current);
    }

    [Fact]
    public void MonsterWins_WithLuck_Fail_PlayerLosesThree()
    {
        // monster wins; luck fail (5+6=11 > luck 9, dice unequal) → hero wound -3
        var dice = new FakeDiceRoller(6, 6, 1, 1, 5, 6);
        var resolver = new CombatResolver(dice);
        var (end, luck) = Hero(endurance: 20, luck: 9);
        var monster = new Monster("ГОБЛИН", 10, 10);

        var r = resolver.ResolveRound(8, end, luck, monster, useLuck: true);

        Assert.False(r.LuckSucceeded);
        Assert.Equal(3, r.DamageToPlayer);
        Assert.Equal(17, end.Current);
    }

    [Fact]
    public void LuckCheck_AlwaysCostsOneLuck()
    {
        var dice = new FakeDiceRoller(2, 4); // sum 6
        var resolver = new CombatResolver(dice);
        var luck = new AttributeScore(8);

        var ok = resolver.PerformLuckCheck(luck);

        Assert.True(ok);              // 6 <= 8
        Assert.Equal(7, luck.Current); // -1 regardless
    }
}
