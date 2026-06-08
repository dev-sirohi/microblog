# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A full-stack microblogging platform. Backend-heavy, focused on caching strategies, background processing, and scalable API design. The project deliberately explores real-world trade-offs between performance (Redis) and durability (SQL Server).

## Commands

### Backend (ASP.NET Core — .NET 10)

```powershell
# Run the API
cd src/Microblog.Api
dotnet run --launch-profile https   # https://localhost:7282
dotnet run --launch-profile http    # http://localhost:5182

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Run via Docker (API + SQL Server + Redis)
cd src
docker-compose up
```

### Frontend (React/TypeScript — Vite)

```powershell
cd web-client
npm install
npm run dev      # Dev server (port 3000)
npm run build    # Type-check + bundle
npm run lint     # ESLint
npm run preview  # Preview production build
```

### Load Testing (NBomber)

```powershell
cd src/StressTestSuite
dotnet run
```

## Architecture

```
React (web-client/)  →  ASP.NET Core API (src/Microblog.Api/)
                                   ↓
                         Service Layer (Business Logic)
                                   ↓
                    SQL Server (source of truth) + Redis (cache/async layer)
```

### Backend Structure

- `Controllers/` — thin HTTP layer, delegates entirely to services
- `Services/` — all business logic; one service per domain (Post, Comment, UserFollow, UserLike, Auth, Media, UserProfile)
- `Services/BackgroundSyncService` + `SyncWorkerService` — hosted `IHostedService` that polls Redis queues every ~1 second and writes batched operations to SQL Server
- `Features/Recommendations/` — embedding generation and cosine-similarity post recommendations
- `Infrastructure/Caching/` — generic `ICacheService<T>` (cache-aside over Redis)
- `Infrastructure/RateLimiting/` — ASP.NET Core rate limiting middleware with per-endpoint policies; Redis-backed sliding window
- `Infrastructure/Messaging/` — `IMessagePublisher` abstraction with Kafka and Azure Service Bus implementations
- `Infrastructure/Storage/` — `IStorageService` abstraction with local and Azure Blob Storage implementations
- `Infrastructure/Observability/` — Prometheus metrics via `AppMetrics`
- `Database/AppDbContext.cs` — EF Core DbContext; migrations in `Migrations/`
- `Utils/AppConstants.cs` — enums for `InMemoryOperationType` and Redis cache config; **check here before adding new operation types or cache keys**
- `Utils/GlobalConfig.cs` — runtime config loaded from `appsettings.json`

### Frontend Structure

- `src/api/ApiUtils.ts` — core Axios wrapper; intercepts 401s, queues in-flight requests, triggers token refresh, then replays; all API modules go through this
- `src/utils/GlobalDialogProvider/` and `GlobalNavbarProvider/` — React context providers for shared UI state (no Redux/Zustand)
- `src/pages/` — one file per route; routing configured in `App.tsx`

### Key Patterns

**Eventual Consistency via Redis Queues**: Fast-path writes go to Redis first; `BackgroundSyncService` drains those queues to SQL Server asynchronously. This pattern is used for likes and follows. When reading these counts, check whether they're served from Redis or SQL to understand consistency guarantees.

**Redis Key Design**:

- `user:{id}:following` / `user:{id}:followers` — relationship sets
- `userLikes:eventsQueue` — background sync queue
- `{action}:userId:{id}` / `{action}:clientIp:{ip}` — rate limit counters

**Authentication**: JWT tokens stored in HTTP-only, Secure, SameSite=Strict cookies. Access token (60 min) + refresh token (7 days). Frontend queues requests on 401 and retries after refresh — see `ApiUtils.ts`.

**CORS**: All origins allowed in Development; only `localhost:3000` in Production — configured in `Program.cs`.

**DB Initialization**: `Program.cs` calls `dbContext.Database.MigrateAsync()` on startup, so migrations apply automatically.

## Prerequisites for Local Development

- SQL Server running (localhost:1433)
- Redis running (localhost:6379)
- `src/Microblog.Api/appsettings.Development.json` with correct connection strings
- Frontend expects API at `VITE_API_BASE_URL` (defaults to the .NET dev URLs)

Or use `docker-compose up` from `src/` to spin up all three services together.

## Feature Flags

Controlled via `appsettings.json` — never hardcode secrets, use `appsettings.Development.json` for local values:

- `Features:EnableEmbeddings` — bool; requires `OpenAI:ApiKey`
- `Features:MessagingProvider` — `"kafka"` or `"azure-service-bus"`
- `Features:EnableAzureStorage` — bool; requires `Azure:BlobConnectionString`
