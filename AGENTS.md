# AGENTS.md

Guidance for Codex (and humans) working in this repository. Local-only working notes and an
interview crib sheet.

---

## 1. What this project is

A microblogging platform built to explore the trade-off between **performance (Redis)** and
**durability (SQL Server)**. Users register, log in, post, like, and follow. Scope is deliberately
narrow — see section 7 for what was removed on purpose.

Stack: **ASP.NET Core (.NET 10)** · **SQL Server** · **Redis** · **React + TypeScript (Vite)** ·
**Docker Compose** · **Prometheus/Grafana** · **Azure**.

---

## 2. Repository layout

```
microblog/
├─ .github/workflows/ci-cd.yml    # build → test → publish → deploy to Azure
├─ src/
│  ├─ Microblog.Api/
│  │  ├─ Controllers/             # Auth, Post, UserLike, UserFollow, Users
│  │  ├─ Services/
│  │  │  ├─ AuthService, PostService, UserService
│  │  │  ├─ UserLikeService, UserFollowService   # fast-path writes → Redis
│  │  │  ├─ RateLimiter.cs                       # IRateLimiter + sliding-window log
│  │  │  └─ BackgroundProcesses/
│  │  │     ├─ BackgroundSyncService.cs          # drains Redis queues → SQL in batches
│  │  │     └─ SyncQueue.cs                      # every Redis key lives here
│  │  ├─ Filters/                 # RateLimitFilter + [RateLimit] attribute
│  │  ├─ Infrastructure/
│  │  │  ├─ Caching/              # ICacheService + RedisCacheService (cache-aside)
│  │  │  ├─ Messaging/            # IMessagePublisher + ServiceBusPublisher
│  │  │  ├─ Storage/              # IStorageService + AzureBlobStorageService
│  │  │  └─ Observability/        # AppMetrics.cs (Prometheus counters/gauges)
│  │  ├─ Database/AppDbContext.cs # EF Core; Migrations/ auto-applied on startup
│  │  ├─ Models/ DTOs/ Utils/ Middlewares/
│  │  └─ Program.cs               # composition root + pipeline
│  ├─ Microblog.Tests/            # xUnit + Testcontainers (SQL Server + Redis)
│  ├─ docker-compose.yml          # the only compose file
│  ├─ prometheus.yml
│  └─ grafana/                    # auto-provisioned datasource + dashboard
└─ web-client/src/                # 10 files: api.ts, types.ts, App.tsx, main.tsx, 5 pages
```

**Before changing things:**
- New Redis key? → `Services/BackgroundProcesses/SyncQueue.cs`.
- New rate-limited endpoint? → add an `AppConstants.ApiRequestAction`, a policy in
  `RateLimiter.GetPolicy`, then `[RateLimit(action)]` on the action.
- New background-drained write? → follow the like/follow pattern (service enqueues, worker drains).

---

## 3. Commands

```powershell
# Everything in containers
cd src; docker-compose up

# API only (needs SQL + Redis running)
cd src/Microblog.Api; dotnet run          # https://localhost:7282, Swagger at /swagger

# Frontend
cd web-client; npm install; npm run dev   # http://localhost:3000

# Integration tests (needs Docker for Testcontainers)
cd src; dotnet test Microblog.Tests/Microblog.Tests.csproj

# EF migrations
cd src/Microblog.Api; dotnet ef migrations add <Name>
```

---

## 4. Configuration

**One file: `appsettings.json`.** There is no `appsettings.Development.json` and no environment
branching — everything runs as `Production`, locally and in Azure.

Every setting is overridable by an environment variable using `__` for nesting
(`ConnectionStrings__DefaultConnection`), which is exactly how `docker-compose.yml` and Azure App
Service Application Settings inject their values. Same build, three sets of connection strings.

Notable keys:
- `RateLimiting:Disabled` — `false`, so the limiter is always on. Tests set it explicitly too.
- `Features:MessagingProvider` — `"azure-service-bus"` (needs `Azure:ServiceBusConnectionString`) or `"none"`.
- `Azure:KeyVaultUri` — when set, `Program.cs` appends Key Vault as the **last** config source, so its
  secrets win over both `appsettings.json` and environment variables (last source registered wins).

### Azure deployment (free tier)

```bash
az group create -n microblog-rg -l eastus
az appservice plan create -n microblog-plan -g microblog-rg --is-linux --sku F1
az webapp create -g microblog-rg -p microblog-plan -n <APP_NAME> --runtime "DOTNETCORE:10.0"

az keyvault create -n <VAULT_NAME> -g microblog-rg -l eastus
az keyvault secret set --vault-name <VAULT_NAME> --name "ConnectionStrings--DefaultConnection" --value "<sql-cs>"
az keyvault secret set --vault-name <VAULT_NAME> --name "Redis--ConnectionString" --value "<redis-cs>"
az keyvault secret set --vault-name <VAULT_NAME> --name "Jwt--Key" --value "<random 32+ chars>"

az webapp identity assign -g microblog-rg -n <APP_NAME>
az keyvault set-policy -n <VAULT_NAME> --object-id <IDENTITY_OBJECT_ID> --secret-permissions get list
az webapp config appsettings set -g microblog-rg -n <APP_NAME> \
  --settings Azure__KeyVaultUri="https://<VAULT_NAME>.vault.azure.net/"
```

Key Vault secret names use `--`, which the config system maps to `:`.

CI/CD needs repo **secret** `AZURE_WEBAPP_PUBLISH_PROFILE` and **variable** `AZURE_WEBAPP_NAME`.
`dotnet publish` is mandatory — App Service runs compiled output, there is no source-deploy path.

**Cost note:** App Service F1 and Azure SQL's free serverless tier are genuinely $0. Azure Cache for
Redis and Service Bus have no free tier — deploy without them and the app degrades by design (the
rate limiter fails open, likes/follows fall back to SQL, messaging is off via the feature flag).

---

## 5. Endpoints

| Method | Route | Auth | Rate limit |
|---|---|---|---|
| POST | `/api/auth/register` | – | Register 5/5min |
| POST | `/api/auth/login` | – | Login 5/min |
| POST | `/api/auth/refreshtoken` | cookie | Login 5/min |
| POST | `/api/auth/logout` | cookie | – |
| GET | `/api/post/homefeed` | – | – |
| POST | `/api/post` | ✔ | CreatePost 10/min |
| GET / PATCH / DELETE | `/api/post/{id}` | – / ✔ / ✔ | – / UpdatePost 10/min / – |
| POST | `/api/userlike/like/{id}` · `unlike/{id}` | ✔ | 60/min |
| GET | `/api/userlike/{id}` | ✔ | – |
| POST | `/api/userfollow/follow/{id}` · `unfollow/{id}` | ✔ | 30/min |
| GET | `/api/userfollow` · `/following` | ✔ | – |
| GET | `/api/users/me` · `/api/users/{id}` | ✔ / – | – |
| POST | `/api/users/me/avatar` | ✔ | – |
| GET | `/metrics` | – | – |

---

## 6. Key patterns (know these cold)

**Auth.** `AuthController` writes the JWT to an HTTP-only, `Secure`, `SameSite=Strict` cookie.
`Program.cs` hooks `JwtBearerEvents.OnMessageReceived` to read the token **from the `accessToken`
cookie** — the default bearer handler only reads the `Authorization` header, so without this hook
cookie auth authenticates nothing. Refresh tokens are random 32 bytes stored in SQL (`AuthTokens`);
refresh rotates them (delete all rows for the user, issue a new one) and logout deletes them.

**Rate limiting.** `RateLimiter.cs` is a **Redis sliding-window log**: one sorted set per
`(action, caller)` scored by request timestamp. Each call `ZREMRANGEBYSCORE`s entries older than the
window, `ZCARD`s what remains, rejects with 429 if that already hit the limit, else `ZADD`s itself
and refreshes the TTL. Applied via `[RateLimit(action)]` → `RateLimitFilter` → `IRateLimiter`.
Caller = user id when authenticated, client IP otherwise. **Fails open** if Redis is down.

**Eventual-consistency write path.** `UserLikeService`/`UserFollowService` update the Redis read
structures (`post:{id}:likers` sorted set, `user:{id}:following`/`followers` sets) and push an event
onto a Redis sorted-set queue, then return. `BackgroundSyncService` polls every second, pulls a batch
oldest-first, **collapses to the last intent per entity** (like→unlike in one batch is a no-op),
applies the net change in one `SaveChanges`, and only *then* removes the events from Redis.
Processing-before-removal plus idempotent applies means a crash mid-batch safely re-processes.

**Caching.** `RedisCacheService` is cache-aside. `PostService.GetPostByIdAsync` reads through it and
invalidates on update/delete. `GetAsync` increments `microblog_cache_hits_total` /
`microblog_cache_misses_total`, which is what the Grafana cache-hit-ratio panel graphs.

**Storage / messaging.** `IStorageService` → `AzureBlobStorageService` (Azurite locally), used by
`POST /api/users/me/avatar`. `IMessagePublisher` → `ServiceBusPublisher`, registered only when
`Features:MessagingProvider = "azure-service-bus"`; publishes are fire-and-forget and best-effort,
so the app runs identically with messaging off.

**Redis keys** (all in `SyncQueue.cs`): `post:{id}:likers`, `user:{id}:following`,
`user:{id}:followers`, `sync:likeEvents`, `sync:followEvents`, plus `rl:{action}:{caller}` from the
rate limiter and `post:{id}` from the cache.

---

## 7. Future scope (removed on purpose — do not re-add unasked)

- **Comments**, **embedding-based recommendations** (OpenAI), **cache stampede protection** (RedLock),
  **health probes**, **Serilog**, **OpenTelemetry**, **Polly**, **NBomber load tests**.
- Domain services are injected as concrete classes; interfaces exist only where an implementation
  genuinely swaps (`ICacheService`, `IStorageService`, `IMessagePublisher`, `IRateLimiter`).
- Source files carry no explanatory comments by design.

---

## 8. Fixes already made (don't reintroduce the bugs)

- JWT bearer reads the token from the `accessToken` cookie.
- `AuthService.LoginAsync` uses translatable LINQ (`==`, not `string.Equals(StringComparison)`).
- `ExceptionHandlerMiddleware` sets a 500 on unhandled exceptions (used to return 200).
- CORS uses the credentialed `CorsPolicy` — `AllowAnyOrigin` + credentials is invalid and breaks login.
- `AzureBlobStorageService` does no I/O in its constructor; the container is created lazily on upload.
  (Doing it in the constructor 500s every endpoint that injects `IStorageService` when Azurite is absent.)
- `docker-compose.override.yml` was deleted — Compose auto-merges that filename, which silently forced
  Development mode and mounted Windows-only `%APPDATA%` paths.
