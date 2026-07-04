# ЛАБИРИНТ — web game

A multiplayer web version of the solo adventure game-book **«Лабиринт»**
(Russian translation of Jacek Ciesielski's *Dreszcz*, 1988). Each player gets a
named, PIN-protected save; the app automates dice, combat and the luck (ССС)
mechanics, and always shows your character sheet and a map of the sections you've
already explored.

> The game text and **all rules** come from the original book (see `rules.md`).
> Nothing in the story is invented.

## Architecture

Clean Architecture / SOLID, .NET 10:

| Project | Role |
|---|---|
| `Labyrinth.Domain` | Entities + pure game rules (combat, luck, attributes). No dependencies. |
| `Labyrinth.Application` | Use-case services + port interfaces (DIP). |
| `Labyrinth.Infrastructure` | EF Core/SQLite, JSON section loader, PBKDF2, JWT, dice. |
| `Labyrinth.Shared` | DTOs shared by API ↔ UI. |
| `Labyrinth.Api` | ASP.NET Core Web API (JWT auth). |
| `Labyrinth.UI` | Blazor WebAssembly client. |
| `Labyrinth.Tests` | xUnit tests for the rules engine. |

- **Persistence:** SQLite (one file), per-player save serialized as JSON.
- **Auth:** player name + PIN (PBKDF2-hashed), JWT bearer tokens.
- **Data:** `game_data.json` — 387 sections parsed from the PDF (choices + combat).

```
Browser ── Blazor WASM (nginx) ──/api proxy──► ASP.NET Core API ──► SQLite (/data)
```

---

## Run locally (without Docker)

Requires the **.NET 10 SDK**.

```bash
# 1) Terminal A — the API (http://localhost:5080)
cd src/Labyrinth.Api
dotnet run
# DB file labyrinth.db is created next to the project in Development.

# 2) Terminal B — the UI (http://localhost:5180)
cd src/Labyrinth.UI
dotnet run
```

Then open the UI URL printed by `dotnet run`. The dev UI is configured to call
the API at `http://localhost:5080` (see `src/Labyrinth.UI/wwwroot/appsettings.json`)
and the API allows that origin via CORS.

Run the rules tests:

```bash
dotnet test
```

---

## Run locally with Docker

Requires Docker with the Compose plugin.

```bash
cp .env.example .env
# edit .env → set a strong JWT_KEY (e.g. `openssl rand -base64 48`)

docker compose up -d --build
# open http://localhost:8090
```

Everything is served from the UI container on `UI_PORT` (default **8090**); the
UI's nginx reverse-proxies `/api` to the API container, so there's only one port
to expose and no CORS to configure. Saves persist in the `labyrinth-data` volume.

```bash
docker compose logs -f          # watch logs
docker compose down             # stop (keeps the data volume)
docker compose down -v          # stop and DELETE all saves
```

---

## Deploy to a Raspberry Pi

Tested target: **Raspberry Pi 4/5 with 64-bit Raspberry Pi OS (arm64)**.

### Prerequisites on the Pi

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER        # log out/in afterwards
# Docker's install includes the Compose plugin (`docker compose`).
```

### Option A — build on the Pi (simplest)

```bash
git clone <your-repo> labyrinth && cd labyrinth
cp .env.example .env && nano .env    # set JWT_KEY, optionally UI_PORT
docker compose up -d --build
```

The base images (`dotnet/sdk:10.0`, `dotnet/aspnet:10.0`, `nginx:alpine`) are
multi-arch and pull the arm64 variant automatically. First build is slow
(compiling on the Pi) — that's expected.

Open `http://<pi-ip>:8090` from any device on your network.

### Option B — cross-build on your PC (much faster), then ship to the Pi

```bash
# On your PC (one-time): enable buildx emulation
docker run --privileged --rm tonistiigi/binfmt --install arm64

# Build arm64 images and save them to tar files
docker buildx build --platform linux/arm64 -f src/Labyrinth.Api/Dockerfile -t labyrinth-api:latest --load .
docker buildx build --platform linux/arm64 -f src/Labyrinth.UI/Dockerfile  -t labyrinth-ui:latest  --load .
docker save labyrinth-api:latest labyrinth-ui:latest -o labyrinth-images.tar

# Copy to the Pi and load
scp labyrinth-images.tar docker-compose.yml .env pi@<pi-ip>:~/labyrinth/
ssh pi@<pi-ip>
cd labyrinth && docker load -i labyrinth-images.tar
docker compose up -d            # uses the pre-built images
```

> 32-bit Raspberry Pi OS (armv7) also works — use `--platform linux/arm/v7`.
> For best performance use the 64-bit OS.

### Run it on boot

`restart: unless-stopped` is already set in `docker-compose.yml`, so the
containers restart automatically after a reboot once Docker's service is enabled:

```bash
sudo systemctl enable docker
```

### Back up / restore saves

All player saves live in the `labyrinth-data` Docker volume:

```bash
# backup
docker run --rm -v labyrinth-data:/data -v "$PWD":/backup alpine \
  tar czf /backup/labyrinth-saves.tgz -C /data .
# restore
docker run --rm -v labyrinth-data:/data -v "$PWD":/backup alpine \
  sh -c "cd /data && tar xzf /backup/labyrinth-saves.tgz"
```

---

## Configuration reference

| Setting | Where | Default |
|---|---|---|
| `Jwt:Key` | API env `Jwt__Key` / `.env` `JWT_KEY` | placeholder — **must override** |
| `Labyrinth:SqliteConnectionString` | API env | `Data Source=/data/labyrinth.db` |
| `Labyrinth:GameDataPath` | API config | `Data/game_data.json` |
| `Labyrinth:CorsOrigins` | API config | dev UI origins |
| `ApiBaseUrl` | UI `wwwroot/appsettings.json` | `http://localhost:5080` (empty in Docker) |
| `UI_PORT` | `.env` | `8090` |

## Security notes

- Always set a strong, unique `JWT_KEY`.
- PINs are not high-security; this protects casual access to other players'
  saves, not against a determined attacker. Don't expose the Pi to the public
  internet without a reverse proxy + TLS.
