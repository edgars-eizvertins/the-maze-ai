---
name: project-labyrinth
description: Лабиринт gamebook — now a .NET 10 Blazor web app (was a Claude-as-GM console game)
metadata:
  type: project
---

Web version of the solo gamebook «Лабиринт» (Jacek Ciesielski "Dreszcz", 1988).
PDF source: /home/edgars/projects/the-maze-ai/Docs/Лабиринт.pdf (387 sections).

**Why:** User first played it with Claude as Game Master in the console, then asked
to build a proper multi-user web app (found GM-by-chat too slow).

**How to apply:** Use only text/rules from the PDF — never invent story or rules.
GM-facing text is Russian; code is English. See `CLAUDE.md` and `README.md`.

## Stack (built & verified)
.NET 10, Clean Architecture / SOLID. Blazor WASM → ASP.NET Core API → SQLite (EF Core).
JWT auth = player name + PBKDF2 PIN. Multi-user, each with own save + explored-map.
- `src/Labyrinth.{Domain,Application,Infrastructure,Shared,Api,UI}`, `tests/Labyrinth.Tests`
- Domain = pure rules engine (combat/luck/attributes), 12 xUnit tests pass.
- `game_sections.json` = raw PDF parse; `game_data.json` = enhanced (choices+combat),
  loaded by API from `src/Labyrinth.Api/Data/`. Regenerate scripts live in scratchpad.
- Docker: per-project Dockerfiles + `docker-compose.yml` (UI nginx proxies /api → api).
  Both images build; full stack smoke-tested; saves persist on `labyrinth-data` volume.

## Key facts / decisions
- Section 387 = victory; 373 (+redirects) = death.
- Engine auto-applies only combat/ССС/eat/elixir. Narrative effects (gold, ±attrs,
  items) are MANUAL via UI +/- controls — faithful to the book, avoids mis-parsing.
- Toolchain: host has .NET 10 SDK (10.0.109) at /usr/lib/dotnet + SDK 8; `global.json`
  pins 10.0.109. Docker daemon works; no `docker compose` plugin locally (use docker run).
- Deploy target: Raspberry Pi (arm64). README has build-on-Pi and buildx cross-build.

Player English commands existed in the old console version (stats/inventory/eat/...);
the web UI replaces them with buttons + an always-visible stats/map panel.
