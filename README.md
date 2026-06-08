# Microblog — Full Stack Social Platform

A full-stack microblogging platform built with **.NET / ASP.NET Core**, **SQL Server**, **Redis**, and **React + TypeScript**. Backend-heavy, focused on caching strategies, observability, async messaging, and scalable API design.

---

**API Preview**

<img width="929" height="893" alt="image" src="https://github.com/user-attachments/assets/22d5a004-f5fd-4683-aad5-8ce152997c39" />

---

## Tech Stack

**Backend**
- ASP.NET Core / .NET 10
- SQL Server (primary database)
- Redis (caching, relationship sets, event queues, pub/sub)
- JWT authentication via HTTP-only cookies
- Polly (retry + circuit breaker)

**Frontend**
- React + TypeScript (Vite)
- Tailwind CSS — dark mode, responsive
- Optimistic UI updates for likes and follows

**Infrastructure**
- Kafka or Azure Service Bus (configurable event streaming)
- Azure Blob Storage (media uploads, SAS URLs)
- Prometheus metrics + Grafana
- Docker Compose for local dev

---

## Architecture

```
React (web-client/)  →  ASP.NET Core API (src/Microblog.Api/)
                                   ↓
                         Service Layer (Business Logic)
                                   ↓
                    SQL Server (source of truth) + Redis (cache / async layer)
                                   ↓
                    Kafka / Azure Service Bus (durable event streaming)
```

---

## Features

**Core**
- User auth — register, login, logout, JWT refresh
- Posts, comments, likes
- Follow/unfollow system
- Media uploads (local or Azure Blob Storage)

**Caching**
- Generic `ICacheService<T>` cache-aside wrapper over Redis
- Redis sets for follower/like relationships
- Background sync service drains Redis queues to SQL Server asynchronously (eventual consistency)

**Messaging**
- `IMessagePublisher` abstraction — switch between Kafka and Azure Service Bus via `appsettings.json`
- Events: `post.created`, `post.liked`, `user.followed`

**AI Recommendations**
- OpenAI `text-embedding-3-small` generates embeddings on post creation (async, off write path)
- `GET /api/posts/{id}/recommendations` — cosine similarity search, top 5–10 similar posts
- Results cached in Redis (10 min TTL)
- Disabled if `Features:EnableEmbeddings` is false or no API key is set

**Observability**
- Prometheus metrics endpoint (`/metrics`) — request latency, cache hit/miss, sync queue depth
- Health checks at `/health`, `/health/ready`, `/health/live` (Redis + SQL Server)

**Rate Limiting**
- ASP.NET Core rate limiting middleware, Redis-backed sliding window
- Per-endpoint policies: auth (5 req/min), post creation (20 req/min), feed (60 req/min)
- Returns `429 Too Many Requests` with `Retry-After` header

---

## Running the Project

### Docker (recommended)

```powershell
cd src
docker-compose up
```

Starts API, SQL Server, Redis, Kafka, Prometheus, and Grafana together.

### Local

Prerequisites: SQL Server on `localhost:1433`, Redis on `localhost:6379`, connection strings in `appsettings.Development.json`.

```powershell
# Backend
cd src/Microblog.Api
dotnet run --launch-profile https   # https://localhost:7282

# Frontend
cd web-client
npm install
npm run dev                         # http://localhost:3000
```

---

## Key Design Trade-offs

- **Eventual consistency** — likes/follows write to Redis first and sync to SQL Server in the background; reads may lag briefly
- **Configurable messaging** — Kafka for high-throughput self-hosted setups, Azure Service Bus for managed cloud deployments
- **Embedding toggle** — recommendations are entirely opt-in; the platform functions without an OpenAI key

---

## One-line summary

> A backend-heavy microblog platform exploring caching, eventual consistency, async messaging, and AI-powered recommendations.
