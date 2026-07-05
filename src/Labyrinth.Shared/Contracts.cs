namespace Labyrinth.Shared;

// ---- Auth ----------------------------------------------------------------
public record RegisterRequest(string Name, string Pin, string Elixir);
public record LoginRequest(string Name, string Pin);
public record AuthResponse(string Token, string Name, bool HasActiveGame);

// ---- Attributes & state --------------------------------------------------
public record AttributeDto(int Initial, int Current);

public record GameStateDto(
    int CurrentSection,
    bool IsStarted,
    bool IsFinished,
    string? Outcome,                 // "victory" | "death" | null
    AttributeDto Agility,
    AttributeDto Endurance,
    AttributeDto Luck,
    int Gold,
    int Food,
    string? ElixirType,
    int ElixirUsesLeft,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> Items,
    IReadOnlyList<int> VisitedSections,
    bool InCombat);

// ---- Sections ------------------------------------------------------------
public record ChoiceDto(int Target, string Label);

public record SectionDto(
    int Id,
    string Text,
    IReadOnlyList<ChoiceDto> Choices,
    bool CanEat,
    bool HasLuckCheck,
    bool IsVictory,
    bool IsDeath,
    bool HasCombat);

/// <summary>Everything the UI needs to render a turn in one payload.</summary>
public record TurnDto(SectionDto Section, GameStateDto State, CombatStateDto? Combat);

// ---- Combat --------------------------------------------------------------
public record RoundResultDto(
    int MonsterDice,
    int MonsterAttack,
    int PlayerDice,
    int PlayerAttack,
    string Outcome,                  // "player_hit" | "monster_hit" | "tie"
    bool LuckUsed,
    bool LuckSucceeded,
    int DamageToMonster,
    int DamageToPlayer,
    string MonsterName,
    int MonsterEnduranceAfter,
    int PlayerEnduranceAfter);

public record CombatStateDto(
    string MonsterName,
    int MonsterAgility,
    int MonsterEnduranceCurrent,
    int MonsterEnduranceMax,
    int MonstersRemaining,
    bool CanFlee,
    int? FleeSection,
    bool Finished,
    bool Won,
    IReadOnlyList<RoundResultDto> Rounds);

// ---- Action requests -----------------------------------------------------
public record ChooseRequest(int Target);
public record GotoRequest(int Target);
public record CombatRoundRequest(bool UseLuck);
