# Fridge Cheff — Backend

ASP.NET Core Web API that handles ingredient detection, recipe suggestions, usage limits, and the admin panel.

> Copyright (c) 2026 RAise & Perform L.P. All rights reserved.
> This software is proprietary and confidential. See [LICENSE](LICENSE) for details.

---

## Contents

- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Starting the backend](#starting-the-backend)
  - [Option A — Full Docker (recommended)](#option-a--full-docker-recommended)
  - [Option B — Local API + Docker DB](#option-b--local-api--docker-db)
  - [Option C — Fully local (no Docker)](#option-c--fully-local-no-docker)
- [Database migrations](#database-migrations)
- [API docs — Swagger and Scalar](#api-docs--swagger-and-scalar)
- [API reference](#api-reference)
- [Admin panel](#admin-panel)
- [Mock services](#mock-services)
- [Environment variables](#environment-variables)
- [iPhone / network access](#iphone--network-access)
- [curl examples](#curl-examples)
- [Production checklist](#production-checklist)
- [License](#license)

---

## Tech stack

| | |
|---|---|
| Runtime | .NET 10 |
| Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 9 (Pomelo MySQL provider) |
| Database | MySQL 8 |
| AI detection | OpenAI GPT-4o vision |
| Recipe data | Spoonacular API |
| Admin panel | Razor Pages + Bootstrap |
| Logging | Serilog → console + rolling file |
| API docs | Native ASP.NET Core OpenAPI + Swagger UI + Scalar UI |

> **EF Core version note:** EF Core is pinned to 9.x because `Pomelo.EntityFrameworkCore.MySql 9.0.0`
> does not yet support EF Core 10. When Pomelo 10.x ships, update all three EF Core packages together:
> `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.EntityFrameworkCore.Design`.

---

## Project structure

```
apps/backend/
├── docker-compose.yml          Docker services: backend + MySQL + Adminer
├── .env.example                Template for local environment variables
├── Dockerfile                  Multi-stage .NET 10 build
├── Fridge Cheff.sln
└── Fridge Cheff.Api/
    ├── Controllers/            Thin HTTP handlers — parse request, call service, return response
    │   ├── DevicesController.cs
    │   ├── FridgeScansController.cs
    │   ├── RecipesController.cs
    │   └── UsageController.cs
    ├── Services/               Business logic — use AppDbContext directly (no repository layer)
    │   ├── DeviceService.cs
    │   ├── FridgeScanService.cs
    │   ├── RecipeService.cs
    │   ├── UsageLimitService.cs
    │   ├── ImageStorageService.cs
    │   ├── IngredientNormalizationService.cs
    │   ├── OpenAI/             IOpenAiVisionService + real + mock
    │   └── Spoonacular/        ISpoonacularService + real + mock
    ├── Data/
    │   ├── AppDbContext.cs
    │   ├── DesignTimeDbContextFactory.cs
    │   ├── Entities/           EF Core entity classes
    │   └── Migrations/
    ├── Contracts/
    │   ├── Requests/           Inbound DTOs
    │   └── Responses/          Outbound DTOs
    ├── Options/                Typed config classes (bound from appsettings)
    ├── Exceptions/             RateLimitException, NotFoundException, AppValidationException
    ├── Middleware/             ApiExceptionMiddleware — maps exceptions to HTTP status codes
    ├── Areas/Admin/Pages/      Razor Pages admin panel
    ├── Program.cs
    └── appsettings.json
```

---

## Starting the backend

All three options serve the API at `http://localhost:5000`.

### Option A — Full Docker (recommended)

Runs the API, MySQL, and Adminer all in containers. Nothing needs to be installed locally except Docker.

```bash
cd apps/backend

# First time only — copy env file and set passwords
cp .env.example .env

# Start everything (add --build on first run or after code changes)
docker compose up --build

# Start in background
docker compose up -d --build

# View logs
docker compose logs -f backend

# Stop
docker compose down

# Stop and wipe the database volume
docker compose down -v
```

Services started:

| Service | URL | Notes |
|---|---|---|
| Backend API | `http://localhost:5000` | |
| Swagger UI | `http://localhost:5000/swagger` | Dev only |
| Scalar UI | `http://localhost:5000/scalar/v1` | Dev only |
| Admin panel | `http://localhost:5000/admin` | Password: see `ADMIN_PASSWORD` in `.env` |
| Adminer (DB browser) | `http://localhost:8080` | MySQL GUI — see note below |
| MySQL | `localhost:3306` | |

**Adminer login** (connect to MySQL via the browser UI):

| Field | Value |
|---|---|
| System | MySQL |
| Server | `mysql` |
| Username | `recipeapp` (or `MYSQL_USER` from `.env`) |
| Password | `MYSQL_PASSWORD` from `.env` |
| Database | `recipeapp` (or `MYSQL_DATABASE` from `.env`) |

> Use `mysql` as the server name — not `localhost`. Inside Docker, containers talk to each other by service name.

Database migrations run automatically on API startup.

---

### Option B — Local API + Docker DB

Useful when you want to run and debug the API directly (faster restarts, attaching a debugger).

```bash
# Terminal 1 — start only the database
cd apps/backend
docker compose up mysql adminer

# Terminal 2 — run the API locally (from WSL or Windows terminal)
cd apps/backend/Fridge Cheff.Api
dotnet run
```

The local connection string is pre-configured in `appsettings.Development.json`:

```
Server=localhost;Port=3306;Database=recipeapp;User=recipeapp;Password=recipeapp_dev;
```

To override any setting locally without touching the `.env` file, set environment variables directly:

```bash
# Example: enable real OpenAI in a local run
OPENAI_API_KEY=sk-... dotnet run
```

---

### Option C — Fully local (no Docker)

Install MySQL 8 locally, create the database and user manually, then run the API:

```sql
CREATE DATABASE recipeapp;
CREATE USER 'recipeapp'@'localhost' IDENTIFIED BY 'recipeapp_dev';
GRANT ALL PRIVILEGES ON recipeapp.* TO 'recipeapp'@'localhost';
```

```bash
cd apps/backend/Fridge Cheff.Api
dotnet run
```

---

## Database migrations

Migrations run automatically on API startup (`db.Database.MigrateAsync()` in `Program.cs`).

**Add a new migration** after changing entities:

```bash
cd apps/backend/Fridge Cheff.Api
dotnet ef migrations add <MigrationName>
```

**Apply migrations manually:**

```bash
dotnet ef database update
```

**Remove the last unapplied migration:**

```bash
dotnet ef migrations remove
```

---

## API docs — Swagger and Scalar

Both UIs are available in **Development mode only** and are powered by the same OpenAPI spec.
They are disabled automatically in Production (`ASPNETCORE_ENVIRONMENT=Production`).

### OpenAPI spec

The raw OpenAPI JSON spec is generated natively by ASP.NET Core and served at:

```
http://localhost:5000/openapi/v1.json
```

This is the source of truth. Both Swagger and Scalar read from it. The mobile app also uses it to generate a type-safe API client via `openapi-typescript`.

---

### Swagger UI

```
http://localhost:5000/swagger
```

The classic interactive API docs. Use it to:
- Browse all endpoints and their request/response shapes
- Try out requests directly from the browser (fill in fields, hit Execute)
- Inspect response bodies and status codes

Swagger UI runs entirely in your browser and makes requests directly to your local API. Nothing is sent to any external service.

**Good for:** quick local testing of individual endpoints during development.

---

### Scalar UI

```
http://localhost:5000/scalar/v1
```

A more modern API docs UI. Use it to:
- Browse endpoints with a cleaner interface
- Try out requests from the browser

**"Ask AI" button — important note:**

Scalar includes an "Ask AI" button in its UI. If you use it:
- Your question and your API spec are sent to **Scalar's own cloud servers**, not to your backend
- Scalar calls OpenAI using **Scalar's own API key and credits** — your `OPENAI_API_KEY` in `.env` is never involved
- Your `OPENAI_API_KEY` only ever lives on your server and is used exclusively for the `POST /api/fridge-scans` ingredient detection endpoint

For local development, use **Swagger UI** if you want a fully self-contained tool with no external calls.

---

## API reference

All endpoints return `application/json`. Errors return `{ "error": "message" }`.

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/devices/register` | Register a device — idempotent, returns existing if already registered |
| `GET` | `/api/devices/settings?deviceId=` | Get device's recipe-matching and pantry preferences |
| `PUT` | `/api/devices/settings` | Update device's recipe-matching and pantry preferences |
| `POST` | `/api/fridge-scans` | Upload 1–2 ingredient photos, detect ingredients with AI |
| `POST` | `/api/fridge-scans/{scanId}/confirm-ingredients` | Confirm or edit the detected ingredient list |
| `POST` | `/api/recipes/suggest` | Get recipe suggestions based on ingredients and goal |
| `GET` | `/api/recipes/{id}?deviceId=` | Get full recipe detail with macros, steps, missing ingredients |
| `POST` | `/api/recipes/{id}/save` | Save a recipe to the device's saved list |
| `GET` | `/api/usage/me?deviceId=` | Get today's usage counters for a device |
| `GET` | `/health` | Health check |

### Free tier limits

| Action | Default | Config key |
|---|---|---|
| Ingredient scans | 1 per day | `FREE_DAILY_SCAN_LIMIT` |
| Recipe searches | 3 per day | `FREE_DAILY_RECIPE_SEARCH_LIMIT` |
| Recipe detail views | 3 per day | `FREE_DAILY_RECIPE_DETAIL_LIMIT` |
| Images per scan | 2 max | `MAX_IMAGES_PER_SCAN` |
| Suggestions per search | 8 max | — |

Limits are tracked per device per day in the `UsageCounters` table.
Exceeded limit → `HTTP 429` with `{ "error": "..." }`.

### Recipe goal values

| Value | Behaviour |
|---|---|
| `protein_first` | Boosts recipes with protein-rich ingredients |
| `low_calories` | Boosts vegetable-forward recipes |
| `tasty_first` | Boosts by Spoonacular popularity score |

### Missing ingredient filter

By default only recipes where `missedIngredientCount == 0` are returned — i.e. the user has every required ingredient.

This is per-device configurable via `DeviceSettings` (`/api/devices/settings`):

| Field | Default | Effect |
|---|---|---|
| `allowMissingIngredients` | `false` | When `true`, the threshold below is used instead of `0` |
| `maxMissingIngredients` | `0` | Max ingredients a recipe may still need (0–10) |
| `ignorePantry` | `true` | Passed directly as Spoonacular's `findByIngredients` `ignorePantry` parameter — when on, common staples (salt, pepper, oil) don't count against a recipe |
| `alwaysAvailableIngredients` | `[]` | Custom per-device list (e.g. `pasta`, `rice`) — merged directly into the ingredient list sent to Spoonacular on every search |

**Always-available ingredients are merged into the search itself**, not filtered post-hoc. `SuggestAsync` concatenates `req.Ingredients` with `settings.AlwaysAvailableIngredients` before calling Spoonacular, so Spoonacular's own ingredient-matching (which handles synonyms and plurals far better than a simple string match) decides what counts as "used" vs "missing." The same merged-availability approach is used by `GetDetailAsync` when computing the detail page's `missingIngredients` — `userIngredients` (from the latest scan) plus `alwaysAvailableIngredients` together form the device's "available" set in both places.

A `DeviceSettings` row is created with these defaults on first access — no explicit creation step needed.

If no recipes qualify after filtering, the response includes an `emptyState` object with actionable suggestions (e.g. enabling `allowMissingIngredients` in Settings).

---

## Admin panel

Available at `http://localhost:5000/admin`. Cookie session, 8h expiry.

Default dev password: `admin123` — change via `ADMIN_PASSWORD` in `.env`.

| Page | Path | Shows |
|---|---|---|
| Dashboard | `/admin` | Today's counts: scans, searches, API calls, errors |
| Devices | `/admin/devices` | All registered devices, last seen, scan/search counts |
| Scans | `/admin/scans` | Fridge scans, status, detected/confirmed ingredient counts |
| API Calls | `/admin/api-calls` | External API call log — OpenAI and Spoonacular, status codes, duration |
| Usage | `/admin/usage` | Per-device daily usage counters |

---

## Mock services

When an API key is missing and `ASPNETCORE_ENVIRONMENT=Development`, the real service is replaced with a mock automatically. No code change needed — the swap is in `Program.cs`.

| Service | Activated when | Returns |
|---|---|---|
| `MockOpenAiVisionService` | `OPENAI_API_KEY` is blank | 7 realistic detected ingredients |
| `MockSpoonacularService` | `SPOONACULAR_API_KEY` is blank | 5 recipe summaries + 1 full recipe detail with macros and steps |

This lets the full app flow run end-to-end without any paid API keys.

---

## Environment variables

Copy `.env.example` to `.env` and fill in secrets before running.

| Variable | Default | Description |
|---|---|---|
| `MYSQL_ROOT_PASSWORD` | `changeme_root` | MySQL root password |
| `MYSQL_DATABASE` | `recipeapp` | Database name |
| `MYSQL_USER` | `recipeapp` | MySQL app user |
| `MYSQL_PASSWORD` | `recipeapp_dev` | MySQL app user password |
| `OPENAI_API_KEY` | _(blank)_ | OpenAI key — blank enables mock in Development |
| `OPENAI_VISION_MODEL` | `gpt-4o` | Vision model for ingredient detection |
| `SPOONACULAR_API_KEY` | _(blank)_ | Spoonacular key — blank enables mock in Development |
| `ADMIN_PASSWORD` | `admin123` | Admin panel login password |
| `FREE_DAILY_SCAN_LIMIT` | `1` | Max ingredient scans per device per day |
| `FREE_DAILY_RECIPE_SEARCH_LIMIT` | `3` | Max recipe searches per device per day |
| `FREE_DAILY_RECIPE_DETAIL_LIMIT` | `3` | Max recipe detail views per device per day |
| `MAX_IMAGES_PER_SCAN` | `2` | Max images allowed in a single scan upload |
| `MAX_IMAGE_UPLOAD_MB` | `8` | Max file size per image in MB |

---

## iPhone / network access

The API binds to `0.0.0.0:5000` so any device on your local network can reach it.

**Find your machine's LAN IP from WSL:**

```bash
hostname -I | awk '{print $1}'
# or
ip route get 1 | awk '{print $7; exit}'
```

**From Windows PowerShell:**

```powershell
(Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Wi-Fi").IPAddress
```

Set this in the mobile app's `.env`:

```
EXPO_PUBLIC_API_BASE_URL=http://192.168.1.XX:5000
```

Your iPhone must be on the same Wi-Fi network as your dev machine.

---

## curl examples

**Register a device:**
```bash
curl -s -X POST http://localhost:5000/api/devices/register \
  -H "Content-Type: application/json" \
  -d '{"deviceId": "test-device-001"}' | jq
```

**Upload images and detect ingredients:**
```bash
curl -s -X POST http://localhost:5000/api/fridge-scans \
  -F "deviceId=test-device-001" \
  -F "images=@/path/to/fridge.jpg" | jq
```

**Confirm ingredients:**
```bash
SCAN_ID="<scanId from previous response>"

curl -s -X POST "http://localhost:5000/api/fridge-scans/$SCAN_ID/confirm-ingredients" \
  -H "Content-Type: application/json" \
  -d '{"ingredients": ["eggs", "chicken breast", "spinach", "greek yogurt"]}' | jq
```

**Suggest recipes:**
```bash
curl -s -X POST http://localhost:5000/api/recipes/suggest \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "test-device-001",
    "scanId": "'$SCAN_ID'",
    "ingredients": ["eggs", "chicken breast", "spinach", "greek yogurt"],
    "goal": "protein_first"
  }' | jq
```

**Get recipe detail:**
```bash
curl -s "http://localhost:5000/api/recipes/716429?deviceId=test-device-001" | jq
```

**Save a recipe:**
```bash
curl -s -X POST "http://localhost:5000/api/recipes/716429/save" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "test-device-001",
    "title": "Chicken Spinach Skillet",
    "imageUrl": "https://img.spoonacular.com/recipes/716429-556x370.jpg"
  }' | jq
```

**Check usage:**
```bash
curl -s "http://localhost:5000/api/usage/me?deviceId=test-device-001" | jq
```

**Health check:**
```bash
curl -s http://localhost:5000/health | jq
```

---

## Production checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production` (disables Swagger, Scalar, CORS wildcard)
- [ ] Replace `AllowAnyOrigin` CORS policy with a strict origin allowlist
- [ ] Add HTTPS / TLS termination (nginx or Caddy in front of the container)
- [ ] Hash the admin password with bcrypt instead of storing plaintext
- [ ] Add JWT-based user authentication (replace anonymous deviceId)
- [ ] Move image uploads from local disk to S3 or Azure Blob Storage
- [ ] Move usage counters to Redis for multi-instance deployments
- [ ] Add IP-based rate limiting on the scan endpoint
- [ ] Set up structured log aggregation (Seq, Datadog, Grafana Loki)
- [ ] Add a `/health/ready` endpoint that checks MySQL connectivity
- [ ] Audit EF Core includes for N+1 query patterns
- [ ] Store `ADMIN_PASSWORD` in a secrets manager, not a plain env var

---

## License

Copyright (c) 2026 **RAise & Perform L.P.** All rights reserved.

This software and its source code are proprietary and confidential.
Unauthorized copying, distribution, modification, public display, or public
performance of this software, via any medium, is strictly prohibited without
the prior written permission of the copyright holder.

For licensing inquiries: [info@raiseperform.gr](mailto:info@raiseperform.gr)
