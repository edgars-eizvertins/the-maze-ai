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
        Choices = j.Choices.Select(c => new ChoiceContent { Target = c.Target, Label = c.Label, VictoryTarget = c.VictoryTarget }).ToList(),
        Combat = j.Combat is null ? null : new CombatContent
        {
            CanFlee = j.Combat.CanFlee,
            FleeSection = j.Combat.FleeSection,
            Monsters = j.Combat.Monsters
                .Select(m => new MonsterContent { Name = m.Name, Agility = m.Agility, Endurance = m.Endurance })
                .ToList()
        },
        Auto = j.Auto is null ? null : new AutoResolveContent
        {
            Kind = j.Auto.Kind,
            DiceCount = j.Auto.DiceCount,
            Op = j.Auto.Op ?? "",
            Value = j.Auto.Value,
            OnTrue = j.Auto.OnTrue,
            OnFalse = j.Auto.OnFalse,
            OnSuccess = j.Auto.OnSuccess,
            OnFail = j.Auto.OnFail,
            OnVisited = j.Auto.OnVisited,
            OnFirst = j.Auto.OnFirst
        },
        Effects = j.Effects is null
            ? []
            : j.Effects.Select(e => new EffectContent { Kind = e.Kind, Delta = e.Delta, Text = e.Text }).ToList()
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
        [property: JsonPropertyName("isDeath")] bool IsDeath,
        [property: JsonPropertyName("auto")] AutoJson? Auto = null,
        [property: JsonPropertyName("effects")] List<EffectJson>? Effects = null);

    private sealed record EffectJson(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("delta")] int Delta = 0,
        [property: JsonPropertyName("text")] string? Text = null);

    private sealed record AutoJson(
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("diceCount")] int DiceCount = 0,
        [property: JsonPropertyName("op")] string? Op = null,
        [property: JsonPropertyName("value")] int Value = 0,
        [property: JsonPropertyName("onTrue")] int? OnTrue = null,
        [property: JsonPropertyName("onFalse")] int? OnFalse = null,
        [property: JsonPropertyName("onSuccess")] int? OnSuccess = null,
        [property: JsonPropertyName("onFail")] int? OnFail = null,
        [property: JsonPropertyName("onVisited")] int? OnVisited = null,
        [property: JsonPropertyName("onFirst")] int? OnFirst = null);

    private sealed record ChoiceJson(
        [property: JsonPropertyName("target")] int Target,
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("victoryTarget")] int? VictoryTarget = null);

    private sealed record CombatJson(
        [property: JsonPropertyName("monsters")] List<MonsterJson> Monsters,
        [property: JsonPropertyName("canFlee")] bool CanFlee,
        [property: JsonPropertyName("fleeSection")] int? FleeSection);

    private sealed record MonsterJson(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("agility")] int Agility,
        [property: JsonPropertyName("endurance")] int Endurance);
}
