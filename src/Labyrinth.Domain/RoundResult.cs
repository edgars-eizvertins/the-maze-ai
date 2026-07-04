namespace Labyrinth.Domain;

/// <summary>Immutable record of one resolved combat round, for narration/logging.</summary>
public sealed record RoundResult(
    int MonsterDice,        // the 2К the monster rolled
    int MonsterAttack,      // А = 2К + monster Л (+ modifiers)
    int PlayerDice,         // the 2К the hero rolled
    int PlayerAttack,       // В = 2К + hero Л (+ modifiers)
    RoundOutcome Outcome,
    bool LuckUsed,
    bool LuckSucceeded,
    int DamageToMonster,
    int DamageToPlayer,
    string MonsterName,
    int MonsterEnduranceAfter,
    int PlayerEnduranceAfter);
