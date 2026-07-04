namespace Labyrinth.Domain;

/// <summary>
/// Central, single source of truth for the constants and formulas taken verbatim
/// from the «ЛАБИРИНТ» rulebook (rules.md). Keeping every rule number here means
/// the rest of the codebase never hard-codes a magic value (SOLID: SRP / DRY).
/// </summary>
public static class GameRules
{
    // --- Section 1: starting attributes -------------------------------------
    // Ловкость:      Л = 1К + 6
    // Выносливость:  В = 2К + 12
    // Счастье:       С = 1К + 6
    public const int AgilityDiceCount = 1;
    public const int AgilityBonus = 6;
    public const int EnduranceDiceCount = 2;
    public const int EnduranceBonus = 12;
    public const int LuckDiceCount = 1;
    public const int LuckBonus = 6;

    // --- Section 2: combat --------------------------------------------------
    public const int CombatDiceCount = 2;          // А = 2К + Л,  В = 2К + Л
    public const int WoundPenalty = 2;             // normal wound = -2 endurance

    // --- Section 3: flight --------------------------------------------------
    public const int FleePenalty = 2;              // fleeing costs -2 endurance

    // --- Section 4: luck (ССС) ---------------------------------------------
    public const int LuckCostPerCheck = 1;         // every ССС costs -1 luck
    public const int LuckyWoundBonusToMonster = 4; // wound monster w/ luck: -4
    public const int UnluckyWoundToMonster = 1;    // wound monster w/o luck: -1
    public const int LuckyWoundToPlayer = 1;       // hero wounded w/ luck: -1
    public const int UnluckyWoundToPlayer = 3;     // hero wounded w/o luck: -3

    // --- Section 5/6: provisions & elixir ----------------------------------
    public const int StartingFood = 8;             // 8 порций
    public const int FoodEnduranceGain = 4;        // +4 per portion
    public const int MaxElixirUses = 2;            // elixir: 2 drinks per game
    public const int LuckElixirOvershoot = 1;      // luck elixir may exceed initial by 1

    /// <summary>Lowest possible value for any attribute; reaching 0 endurance = death.</summary>
    public const int DeathThreshold = 0;
}
