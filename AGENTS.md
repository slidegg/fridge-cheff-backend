# AGENTS.md — Fridge Cheff Backend

This file provides context for AI coding agents (Claude, Copilot, Cursor, etc.) working on this repository.
Read it before making any changes.

---

## What this project is

A proprietary ASP.NET Core Web API for an AI-powered recipe suggestion app.

The core flow:
1. User uploads 1–2 photos of available ingredients
2. Backend sends images to OpenAI GPT-4o → detects ingredients
3. User confirms the ingredient list
4. Backend queries Spoonacular → returns recipes using only available ingredients
5. User views recipe detail with macros, steps, and missing ingredients

**Owner:** RAise & Perform L.P. — proprietary and confidential. Do not make the repository public.

---

## Tech stack

- **.NET 10** — target framework is `net10.0`
- **ASP.NET Core Web API** — controller-based (not Minimal API)
- **Entity Framework Core 9** — pinned to 9.x (see version note below)
- **Pomelo.EntityFrameworkCore.MySql 9.0.0** — MySQL provider
- **MySQL 8** — via Docker in development
- **Serilog** — structured logging to console and rolling file
- **Native ASP.NET Core OpenAPI** — spec at `/openapi/v1.json`
- **Swagger UI** (`Swashbuckle.AspNetCore.SwaggerUI`) — at `/swagger`
- **Scalar UI** (`Scalar.AspNetCore`) — at `/scalar/v1`
- **Razor Pages** — admin panel only, under `Areas/Admin/Pages/`

---

## Architecture

### Layer structure

```
Controller → Service → AppDbContext
```

- **Controllers** are thin. They parse the HTTP request, call one service method, and return the result. No business logic.
- **Services** contain all business logic. They inject and use `AppDbContext` directly.
- **No Repository layer.** EF Core's `DbContext` and `DbSet<T>` already implement the repository and unit-of-work patterns. Adding another layer is unnecessary here.
- **Interfaces only where multiple implementations exist.** `IOpenAiVisionService` and `ISpoonacularService` have real and mock implementations. Plain services like `DeviceService` and `FridgeScanService` are concrete classes — do not add interfaces to them without a reason.

### Dependency injection conventions

- All services are registered as `Scoped`.
- Use primary constructor injection throughout: `public class MyService(AppDbContext db, ILogger<MyService> logger)`.
- Never use `ServiceLocator` or resolve from `IServiceProvider` manually inside services.

### DTOs

- Request DTOs live in `Contracts/Requests/`, response DTOs in `Contracts/Responses/`.
- Use `record` types for all DTOs.
- Request DTOs use `{ get; init; }` properties with `[Required]` and other validation attributes.
- Response DTOs use positional records (constructor syntax).
- Entities are never returned directly from controllers. Always map to a response DTO.

### Error handling

Three custom exceptions drive the HTTP response via `ApiExceptionMiddleware`:

| Exception | HTTP status |
|---|---|
| `AppValidationException` | 400 |
| `NotFoundException` | 404 |
| `RateLimitException` | 429 |

Throw these from services. Do not set `Response.StatusCode` manually in controllers.
Unhandled exceptions → 500 with a safe generic message (no stack trace exposed).

### External services

- `IOpenAiVisionService` — ingredient detection only. Never use OpenAI to generate recipes.
- `ISpoonacularService` — recipe search and detail. Never scrape recipe websites.
- The mobile app **never** calls OpenAI or Spoonacular directly. All external calls go through this backend.
- API keys are server-side only. They are never returned in any API response.

### Mock services

When an API key is missing and `ASPNETCORE_ENVIRONMENT=Development`, the mock is registered automatically in `Program.cs`. Do not add `if (isDev)` checks inside service logic — the swap happens purely at DI registration time.

---

## Key files

| File | Purpose |
|---|---|
| `Program.cs` | App bootstrap, DI registration, middleware pipeline |
| `Data/AppDbContext.cs` | EF Core context, all `DbSet<T>` properties, model configuration |
| `Data/Entities/Enums.cs` | All enums used across the domain |
| `Data/DesignTimeDbContextFactory.cs` | Allows `dotnet ef migrations add` without a live DB |
| `Options/*.cs` | Typed config — always inject `IOptions<T>`, never `IConfiguration` directly |
| `Middleware/ApiExceptionMiddleware.cs` | Catches domain exceptions, returns consistent JSON errors |
| `Services/OpenAI/IOpenAiVisionService.cs` | Contract for ingredient detection |
| `Services/Spoonacular/ISpoonacularService.cs` | Contract for recipe search and detail |

---

## EF Core version constraint

**EF Core is pinned to 9.x.** Do not upgrade `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, or `Microsoft.EntityFrameworkCore.Design` beyond `9.x` until `Pomelo.EntityFrameworkCore.MySql` releases a version that supports EF Core 10.

When Pomelo 10.x ships, upgrade all three EF Core packages together in one PR.

---

## Coding conventions

- **No comments explaining what the code does.** Only add a comment when the *why* is non-obvious — a constraint, a workaround, or a subtle invariant.
- **Async all the way down.** Every database or HTTP call must be `await`ed. No `.Result` or `.Wait()`.
- **Do not over-engineer.** No premature abstractions, no helper classes for things used once, no generic base classes unless there is a clear repeated pattern.
- **Do not add features beyond what is asked.** A bug fix does not need surrounding cleanup.
- **Nullable reference types are enabled.** Use `?` explicitly for nullable properties. Do not suppress warnings with `!` unless unavoidable.
- **Use `DateOnly` for dates without time** (e.g. `UsageCounter.DateUtc`). Use `DateTime` (UTC) for timestamps.
- **Enum values are stored as strings in the database** (configured in `AppDbContext.OnModelCreating`). Do not change this to integers — string values are readable in Adminer and log output.

---

## Running the project

**Full Docker (recommended):**
```bash
cd apps/backend
cp .env.example .env   # fill in passwords
docker compose up --build
```

**Local API + Docker DB:**
```bash
# Terminal 1
docker compose up mysql adminer

# Terminal 2
cd Fridge Cheff.Api
dotnet run
```

**Add a migration after changing entities:**
```bash
cd Fridge Cheff.Api
dotnet ef migrations add <MigrationName>
```

**Build check:**
```bash
cd Fridge Cheff.Api
dotnet build
```

Expected: `0 Error(s), 0 Warning(s)`.

---

## What not to do

- Do not add a Repository layer. Services use `AppDbContext` directly by design.
- Do not inject `IConfiguration` into services. Use `IOptions<T>` via the `Options/` classes.
- Do not return entity classes from controllers. Always use response DTOs.
- Do not cache Spoonacular recipe content permanently. Spoonacular's terms prohibit it.
- Do not use OpenAI to generate recipes. OpenAI is used only for image ingredient detection.
- Do not add `if (isDev)` branching inside service logic. Mock vs real is resolved at DI registration in `Program.cs`.
- Do not expose API keys, connection strings, or admin passwords in any API response or log output.
- Do not push to a public repository. This codebase is proprietary.
