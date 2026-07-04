namespace Labyrinth.Domain;

/// <summary>A single opponent with its Agility (Л) and Endurance (В).</summary>
public sealed class Monster
{
    public string Name { get; }
    public int Agility { get; }
    public AttributeScore Endurance { get; }

    public Monster(string name, int agility, int endurance)
    {
        Name = name;
        Agility = agility;
        Endurance = new AttributeScore(endurance);
    }

    public bool IsDefeated => Endurance.IsDepleted;
}
