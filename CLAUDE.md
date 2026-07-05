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
(too conditional/risky). The UI provides manual +/- controls so the player applies
them by hand — faithful to the printed book ("честность перед самим собой").

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
