namespace Labyrinth.Domain;

/// <summary>The three character attributes (Признаки) from the rulebook.</summary>
public enum AttributeType
{
    Agility,    // Ловкость (Л)
    Endurance,  // Выносливость (В)
    Luck        // Счастье (С)
}

/// <summary>The single elixir a hero may carry (one of three).</summary>
public enum ElixirType
{
    Agility,    // Эликсир Ловкости
    Endurance,  // Эликсир Выносливости
    Luck        // Эликсир Счастья
}

/// <summary>Outcome of a single combat round.</summary>
public enum RoundOutcome
{
    PlayerHit,   // А &lt; В: monster wounded
    MonsterHit,  // А &gt; В: hero wounded
    Tie          // А = В: repeat
}
