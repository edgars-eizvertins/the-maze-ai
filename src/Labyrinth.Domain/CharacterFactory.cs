namespace Labyrinth.Domain;

/// <summary>Rolls a fresh hero's starting attributes per rulebook §1.</summary>
public sealed class CharacterFactory
{
    private readonly IDiceRoller _dice;

    public CharacterFactory(IDiceRoller dice) => _dice = dice;

    public AttributeScore RollAgility() =>
        new(_dice.Roll(GameRules.AgilityDiceCount) + GameRules.AgilityBonus);

    public AttributeScore RollEndurance() =>
        new(_dice.Roll(GameRules.EnduranceDiceCount) + GameRules.EnduranceBonus);

    public AttributeScore RollLuck() =>
        new(_dice.Roll(GameRules.LuckDiceCount) + GameRules.LuckBonus);
}
