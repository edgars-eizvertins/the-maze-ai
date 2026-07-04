using Labyrinth.Application.State;
using Labyrinth.Domain;

namespace Labyrinth.Application.Services;

/// <summary>Creates fresh playthroughs with rolled starting attributes (rulebook §1).</summary>
public sealed class GameStateFactory
{
    private readonly CharacterFactory _characters;

    public GameStateFactory(CharacterFactory characters) => _characters = characters;

    public GameState Create(ElixirType elixir)
    {
        var agi = _characters.RollAgility();
        var end = _characters.RollEndurance();
        var luck = _characters.RollLuck();
        return new GameState
        {
            CurrentSection = 1,
            IsStarted = true,
            Elixir = elixir,
            Agility = new StoredAttribute(agi.Initial, agi.Current),
            Endurance = new StoredAttribute(end.Initial, end.Current),
            Luck = new StoredAttribute(luck.Initial, luck.Current),
            VisitedSections = [1]
        };
    }
}
