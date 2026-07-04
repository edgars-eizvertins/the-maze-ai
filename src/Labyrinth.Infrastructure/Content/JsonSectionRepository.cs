using System.Text.Json;
using System.Text.Json.Serialization;
using Labyrinth.Application.Abstractions;
using Labyrinth.Application.Content;

namespace Labyrinth.Infrastructure.Content;

/// <summary>
/// Loads the 387 sections from game_data.json once at startup into an immutable
/// dictionary. Read-only and thread-safe for the lifetime of the app (singleton).
/// </summary>
public sealed class JsonSectionRepository : ISectionRepository
{
    private readonly IReadOnlyDictionary<int, SectionContent> _sections;

    public JsonSectionRepository(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл данных игры не найден: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        var root = JsonSerializer.Deserialize<Root>(json, Opts)
                   ?? throw new InvalidOperationException("Не удалось разобрать game_data.json.");

        _sections = root.Sections.ToDictionary(
            kvp => int.Parse(kvp.Key),
            kvp => Map(kvp.Value));
    }

    public SectionContent Get(int id) =>
        _sections.TryGetValue(id, out var s)
            ? s
            : throw new KeyNotFoundException($"Раздел {id} не существует.");

    public bool Exists(int id) => _sections.ContainsKey(id);
    public int Count => _sections.Count;

    private static SectionContent Map(SectionJson j) => new()
    {
        Id = j.Id,
        Text = j.Text,
        HasLuckCheck = j.HasLuckCheck,
        CanEat = j.CanEat,
        IsVictory = j.IsVictory,
        IsDeath = j.IsDeath,
        Choices = j.Choices.Select(c => new ChoiceContent { Target = c.Target, Label = c.Label }).ToList(),
        Combat = j.Combat is null ? null : new CombatContent
        {
            CanFlee = j.Combat.CanFlee,
            FleeSection = j.Combat.FleeSection,
            Monsters = j.Combat.Monsters
                .Select(m => new MonsterContent { Name = m.Name, Agility = m.Agility, Endurance = m.Endurance })
                .ToList()
        }
    };

    private static readonly JsonSerializerOptions Opts = new(JsonSerializerDefaults.Web);

    // ---- JSON shape (matches game_data.json) ----
    private sealed record Root([property: JsonPropertyName("sections")] Dictionary<string, SectionJson> Sections);

    private sealed record SectionJson(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("choices")] List<ChoiceJson> Choices,
        [property: JsonPropertyName("combat")] CombatJson? Combat,
        [property: JsonPropertyName("hasLuckCheck")] bool HasLuckCheck,
        [property: JsonPropertyName("canEat")] bool CanEat,
        [property: JsonPropertyName("isVictory")] bool IsVictory,
        [property: JsonPropertyName("isDeath")] bool IsDeath);

    private sealed record ChoiceJson(
        [property: JsonPropertyName("target")] int Target,
        [property: JsonPropertyName("label")] string Label);

    private sealed record CombatJson(
        [property: JsonPropertyName("monsters")] List<MonsterJson> Monsters,
        [property: JsonPropertyName("canFlee")] bool CanFlee,
        [property: JsonPropertyName("fleeSection")] int? FleeSection);

    private sealed record MonsterJson(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("agility")] int Agility,
        [property: JsonPropertyName("endurance")] int Endurance);
}
