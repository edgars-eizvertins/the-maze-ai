namespace Labyrinth.Domain;

/// <summary>
/// Pure implementation of the combat rules (rulebook §2 &amp; §4). It mutates the
/// supplied hero/monster endurance and luck, and returns a <see cref="RoundResult"/>
/// describing exactly what happened. All randomness comes through
/// <see cref="IDiceRoller"/> so the logic is fully unit-testable.
/// </summary>
public sealed class CombatResolver
{
    private readonly IDiceRoller _dice;

    public CombatResolver(IDiceRoller dice) => _dice = dice;

    /// <summary>
    /// Resolve a single round.
    /// </summary>
    /// <param name="heroAgility">Hero's current Ловкость (incl. temporary battle bonuses).</param>
    /// <param name="heroEndurance">Hero's Выносливость — mutated on a wound.</param>
    /// <param name="heroLuck">Hero's Счастье — mutated when <paramref name="useLuck"/> is true.</param>
    /// <param name="monster">Current opponent — its endurance is mutated on a wound.</param>
    /// <param name="useLuck">Whether the hero performs ССС this round (only relevant when a wound lands).</param>
    /// <param name="heroAttackBonus">Flat bonus to the hero's attack roll (e.g. magic weapon, +3В helmet bonus).</param>
    public RoundResult ResolveRound(
        int heroAgility,
        AttributeScore heroEndurance,
        AttributeScore heroLuck,
        Monster monster,
        bool useLuck,
        int heroAttackBonus = 0)
    {
        // §2.1  А = 2К + monster Л
        var monsterDice = _dice.Roll(GameRules.CombatDiceCount);
        var monsterAttack = monsterDice + monster.Agility;

        // §2.2  В = 2К + hero Л
        var playerDice = _dice.Roll(GameRules.CombatDiceCount);
        var playerAttack = playerDice + heroAgility + heroAttackBonus;

        // §2.3 / §2 Если А=В — repeat
        if (monsterAttack == playerAttack)
        {
            return new RoundResult(monsterDice, monsterAttack, playerDice, playerAttack,
                RoundOutcome.Tie, false, false, 0, 0,
                monster.Name, monster.Endurance.Current, heroEndurance.Current);
        }

        bool playerWoundsMonster = playerAttack > monsterAttack; // А < В → monster wounded
        bool luckSucceeded = false;
        int damageToMonster = 0;
        int damageToPlayer = 0;

        if (playerWoundsMonster)
        {
            if (useLuck)
            {
                luckSucceeded = PerformLuckCheck(heroLuck);
                damageToMonster = luckSucceeded
                    ? GameRules.LuckyWoundBonusToMonster   // §4: -4
                    : GameRules.UnluckyWoundToMonster;     // §4: -1
            }
            else
            {
                damageToMonster = GameRules.WoundPenalty;  // §2: -2
            }
            monster.Endurance.Decrease(damageToMonster);
        }
        else
        {
            if (useLuck)
            {
                luckSucceeded = PerformLuckCheck(heroLuck);
                damageToPlayer = luckSucceeded
                    ? GameRules.LuckyWoundToPlayer         // §4: -1
                    : GameRules.UnluckyWoundToPlayer;      // §4: -3
            }
            else
            {
                damageToPlayer = GameRules.WoundPenalty;   // §2: -2
            }
            heroEndurance.Decrease(damageToPlayer);
        }

        return new RoundResult(
            monsterDice, monsterAttack, playerDice, playerAttack,
            playerWoundsMonster ? RoundOutcome.PlayerHit : RoundOutcome.MonsterHit,
            useLuck, luckSucceeded, damageToMonster, damageToPlayer,
            monster.Name, monster.Endurance.Current, heroEndurance.Current);
    }

    /// <summary>
    /// ССС («Создание Своего Счастья», §4): roll 2К. Success when both dice are equal
    /// OR the sum ≤ current luck. Every check costs −1 luck regardless of outcome.
    /// </summary>
    public bool PerformLuckCheck(AttributeScore luck)
    {
        var d1 = _dice.RollOne();
        var d2 = _dice.RollOne();
        bool success = d1 == d2 || (d1 + d2) <= luck.Current;
        luck.Decrease(GameRules.LuckCostPerCheck);
        return success;
    }
}
