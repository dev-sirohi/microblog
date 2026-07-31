# Microblog — Full Stack Social Platform

A backend-focused microblogging platform built with **ASP.NET Core (.NET 10)**, **SQL Server**,
**Redis**, and **React + TypeScript**. The interesting engineering is the write path, the rate
limiter, and the infrastructure — not the feature set.

---

## Highlights

- **JWT auth with cookie-based sessions** — access + refresh tokens in HTTP-only, Secure,
  SameSite=Strict cookies; refresh tokens persisted in SQL Server via EF Core.
- **Redis sliding-window rate limiting** — per-endpoint policies applied with a `[RateLimit]` attribute.
- **Eventual-consistency write path** — likes and follows are written to Redis and drained into SQL
  Server in batches by a hosted `BackgroundService`, keeping user-facing latency off the database.
- **Pluggable infrastructure** — `IMessagePublisher` (Azure Service Bus) and `IStorageService`
  (Azure Blob Storage) behind interfaces.
- **Observability** — Prometheus metrics scraped into auto-provisioned Grafana dashboards
  (request latency, cache-hit ratio, background-sync queue depth).
- **Deployment** — Azure App Service with secrets in Azure Key Vault, shipped by a GitHub Actions pipeline.
- **Integration tests** — xUnit + Testcontainers covering auth, rate limiting, and the batched write path.

---

## Architecture

```
React SPA (web-client/) ──HTTP (auth cookies)──▶ ASP.NET Core API (src/Microblog.Api/)
                                                        │
                                                 Service layer
                                                  ╱            ╲
                                    Redis (fast read/write)   SQL Server (durable)
                                                  ╲            ╱
                                BackgroundSyncService drains Redis queues → SQL in batches
```

---

## API

| Method | Route | Auth | Rate limit |
|---|---|---|---|
| POST | `/api/auth/register` | – | 5 / 5 min |
| POST | `/api/auth/login` | – | 5 / min |
| POST | `/api/auth/refreshtoken` | cookie | 5 / min |
| POST | `/api/auth/logout` | cookie | – |
| GET | `/api/post/homefeed` | – | – |
| POST | `/api/post` | ✔ | 10 / min |
| GET · PATCH · DELETE | `/api/post/{id}` | – / ✔ / ✔ | – / 10 per min / – |
| POST | `/api/userlike/like/{id}` · `unlike/{id}` | ✔ | 60 / min |
| GET | `/api/userlike/{id}` | ✔ | – |
| POST | `/api/userfollow/follow/{id}` · `unfollow/{id}` | ✔ | 30 / min |
| GET | `/api/userfollow` · `/api/userfollow/following` | ✔ | – |
| GET | `/api/users/me` · `/api/users/{id}` | ✔ / – | – |
| POST | `/api/users/me/avatar` | ✔ | – |
| GET | `/metrics` | – | – |

---

## Running it

### Docker Compose

```powershell
cd src
docker-compose up
```

Starts the API (:8080), SQL Server (:1433), Redis (:6379), Azurite (:10000), Prometheus (:9090)
and Grafana (:3001, admin/admin). The API auto-migrates the database on startup.

### Locally

Requires SQL Server on `localhost:1433` and Redis on `localhost:6379` — the quickest way is to run
`docker-compose up sqlserver redis azurite` and start the API from your IDE.

```powershell
cd src/Microblog.Api
dotnet run          # https://localhost:7282 (Swagger at /swagger)

cd web-client
npm install
npm run dev         # http://localhost:3000
```

### Tests

```powershell
cd src
dotnet test Microblog.Tests/Microblog.Tests.csproj
```

Requires a running Docker daemon — Testcontainers starts throwaway SQL Server and Redis containers
and the tests exercise the real app through `WebApplicationFactory<Program>`.

---

## Key design decisions

- **Eventual consistency for likes/follows.** They hit Redis first and drain to SQL in the background.
  Batches are collapsed to the final intent per entity, and events are processed *before* being
  removed from the queue, so a crash mid-batch re-processes safely rather than losing writes.
- **A custom sliding-window rate limiter.** A fixed window lets a caller burst 2× the limit across the
  boundary; a sliding-window log over a Redis sorted set enforces the limit at every instant, and
  living in Redis means it holds across every API instance. It fails open if Redis is unavailable.
- **Config, not code, decides the environment.** The app only reads connection strings and feature
  flags, so the same build runs against Docker containers locally and Azure services in the cloud.
- **Interfaces where implementations actually swap** — storage, messaging, caching and rate limiting.
  Single-implementation domain services are injected as concrete classes.

---

## Future scope

Deliberately out of scope for now:

- **Comments** on posts.
- **Embedding-based recommendations** — "similar posts" via cosine similarity over OpenAI embeddings
  computed off the write path.
- **Cache stampede protection** — a distributed lock around cache-miss recomputation.
- **Health probes** — `/health/ready` (SQL + Redis) and `/health/live` for App Service.
- **Structured logging and tracing** — Serilog sinks and OpenTelemetry traces alongside the existing
  Prometheus metrics.
