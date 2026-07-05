# ЛАБИРИНТ — project guide for Claude

## What this is

A web version of the solo adventure game-book **«Лабиринт»** (Russian translation
of Jacek Ciesielski's *Dreszcz*, 1988). Multiplayer: each player has a named,
PIN-protected save. The app automates dice/combat/luck and shows a live character
sheet + map of explored sections.

**Source of truth for content & rules:** the PDF in `Docs/` and `rules.md`.
Never invent story text or rules — everything comes from the book.

## Tech stack

.NET 10, Clean Architecture / SOLID. Blazor WebAssembly UI → ASP.NET Core Web API
→ SQLite (EF Core). JWT auth (name + PBKDF2-hashed PIN). See `README.md` for the
full architecture table and deploy steps.

```
src/Labyrinth.Domain          rules engine (combat, luck, attributes) — pure, tested
src/Labyrinth.Application      services (GameService, CombatService, AuthService) + ports
src/Labyrinth.Infrastructure   EF Core/SQLite, JSON loader, JWT, dice, DI wiring
src/Labyrinth.Shared           DTOs shared API ↔ UI
src/Labyrinth.Api              controllers (AuthController, GameController)
src/Labyrinth.UI               Blazor WASM (Components/, Services/, Pages/Home.razor)
tests/Labyrinth.Tests          xUnit — CombatResolver & AttributeScore
```

## Data files

| File | Role |
|---|---|
| `game_sections.json` | Intermediate parse of the PDF (387 sections, raw links). Keep as source. |
| `game_data.json` | Enhanced data the **app loads**: structured choices + combat (monsters, flee). Copied into `src/Labyrinth.Api/Data/`. |
| `rules.md` | Human-readable rules reference (RU). |

If you change `game_data.json`, also copy it to `src/Labyrinth.Api/Data/game_data.json`
(the API loads from there; it's `CopyToOutputDirectory`).

### Regenerating game_data.json

The parser/enhancer scripts live in the scratchpad (not committed). The enhancer:
- targets: `см. N`, plus `то/иначе/тогда N` (the one bare case is 200→301).
  **Watch OCR "см" variants** or sections dead-end: Latin `c` in `cм.` (16,19,26,45,
  46,47,49,54,56,68,73), comma punctuation `см,` / `см.,` (89,316), and no-space
  `см.87` (368). These were hand-patched into `game_data.json` (§14,27,41,164,365 too).
  §76 (go to the sum of your 3 keys) and §315 (die-roll subroutine; caller says
  "…а потом N") have **no fixed target by design** — reached via the manual "go to
  section" control (`POST /api/game/goto`), not choices. Large parts of the book,
  incl. victory §387, are only reachable through those note-the-number return jumps.
  **Detour battles** (the shared monster fight §238, reached from §20/§27/§273) also
  have no printed exit after victory: the onward number lives on the *origin* section
  (its "avoid" branch — 316/316/103). The choice leading into such a fight carries
  `victoryTarget` (see below) so the engine shows a **"Далее"** button on the win
  instead of forcing a manual jump.
- combat: parses all `NAME NЛ, MВ` (handles OCR `З`→3, `О`→0); multi-monster
  sections 238/277/312; flee target from "бегством … см. N".
- labels: direction words → "На север/юг/запад/восток"; yes/no; else trimmed phrase.

## Game rules implemented (from the book — do not change without the PDF)

- Start: Л=1К+6, В=2К+12, С=1К+6. Gear: меч/щит/фонарь + 8 food.
- Combat round: А=2К+monster Л vs В=2К+hero Л. Higher wins, loser −2 В. Tie repeats.
- ССС (luck): roll 2К, success if dice equal OR sum ≤ luck; always −1 luck after.
  In combat: lucky wound to monster −4 / unlucky −1; hero wounded lucky −1 / unlucky −3.
- Flee (only if text allows): −2 В, go to flee section.
- Food: +4 В, only where section allows, 8 max. Elixir: 2 uses; restores to initial
  (luck elixir may exceed initial by +1). No attribute may exceed its initial otherwise.
- Section 387 = victory. Section 373 (+ its redirects) = death.

### Design decision: manual narrative effects

The engine auto-applies only what it can do reliably: combat, ССС, eat, elixir.
Inline narrative effects (gold, ±attributes, item pickups) are **not auto-parsed**
(too conditional/risky). The UI provides manual +/- controls (collapsed behind
"✏️ Изменить вручную" — a hidden override) so the player applies them by hand —
faithful to the printed book ("честность перед самим собой").

### Auto-resolved navigation branches (`auto` field in game_data.json)

So the player only ever makes *real* decisions, sections whose only "choice" is a
mechanic the engine can do itself carry an optional `auto` block, resolved in
`GameService.MoveTo` on arrival (chained, depth-capped) and surfaced to the UI as
`TurnDto.AutoSteps` (each keeps the source section's text so no narrative is lost):
- `{"kind":"dice","diceCount":N,"op":"gte|lte","value":V,"onTrue":A,"onFalse":B}` — §112.
- `{"kind":"luck","onSuccess":A,"onFail":B}` — narrative ССС (rolls, −1 luck, branches):
  30,36,41,62,126,138,149,225,229,242,274.
- `{"kind":"visited","onVisited":A,"onFirst":B|null}` — "был ли ты тут?" via visit
  history: 38,53,99,125,310,331; §164 uses `onFirst:null` (stay & show real choices,
  auto-jump only on revisit). §290 was removed from §164's choices.
Sections whose branch is entangled with a stat/gold effect (gambling rolls, prereq
jumps) are **left manual** until stat-automation is done.

### Auto-applied effects (`effects` field in game_data.json)

Unconditional stat/gold/item changes are applied on arrival in `GameService.MoveTo`
(before auto-resolve) via `ApplyAdjustment` — the same code the manual controls use —
and logged as `effect` steps in `TurnDto.AutoSteps`. Shape:
`"effects":[{"kind":"endurance","delta":-1},{"kind":"addItem","text":"ключ №12"}]`
(kinds: agility|endurance|luck|gold|food|addItem|removeItem). Encoded so far (33):
13,19,26,45,56,81,88,89,96,105,119,122,142,153,163,166,177,193,198,199,223,244,254,
263,271,291,294,306,318,326,346,376,380.
**Only encode effects with NO "если"/dice/visited condition** — conditional ones stay
on the manual override. Partial encodes are noted in the patch (§193 skips the
per-monster gold; §153 skips the unmodelled "+1 to attack" sword bonus; §166 skips the
unconditional "уступаешь бабочку" item loss and §346 the broken молот — item strings are
player-entered so auto-removal would be an unreliable no-op). Left **manual** because the
effect is conditional/entangled: §31 (implicit gold, no "возьми"), §162/§284/§315 (ССС/dice),
§207 (needs заколдованная вода), §350 (first-visit only — a visited-branch, not a flat effect).
Attributes
cap at their initial value (via `AttributeScore`); food caps at 8; gold floors at 0.

### Post-victory continuation for detour battles (`victoryTarget` on a choice)

A few combats are shared "detours" the book reaches from several sections and that
print **no onward link after victory** — you win, then return to the number you were
told to write down on the *origin* section. §238 is the case: entered from §20/§27
(continue at §316) or §273 (continue at §103). To spare the player a manual jump, the
**choice that leads into the fight** carries `victoryTarget`:
`{"target":238,"label":"Сразиться","victoryTarget":316}` (in §20/§27; §273 uses 103).
`GameService.ChooseAsync` stashes it into `CombatRunState.VictorySection` (via
`CombatService.Begin`); on a won battle `BuildTurn` appends a synthetic **"Далее"**
choice to that target, and `ChooseAsync` accepts it even though it isn't a printed
choice. Death/flee clear the battle first, so no button appears there. Encoded so far
on choices into §238: §20, §27, §273.

## Build / run / test

```bash
dotnet build                               # whole solution (.slnx)
dotnet test                                # rules tests
cd src/Labyrinth.Api && dotnet run         # API on :5080
cd src/Labyrinth.UI  && dotnet run         # UI  on :5180
docker compose up -d --build               # full stack on :8090
```

`global.json` pins SDK 10.0.109. Host also has SDK 8 — keep `global.json` so the
right SDK is selected.

## Conventions

- GM-facing player text is **Russian**; code identifiers/comments are English.
- Keep the Domain layer dependency-free and unit-tested; randomness goes through
  `IDiceRoller` so combat stays deterministic in tests.
- Services return `ServiceResult<T>` (no throwing for expected failures); error
  messages are user-facing Russian strings.
