# 📝 Microblog — Full Stack Social Platform

A full-stack microblogging platform built with **.NET (C#)**, **SQL Server**, **Redis**, and **React**.

This project focuses on **backend architecture, caching strategies, and scalable API design**, with an emphasis on understanding real-world trade-offs in performance and consistency.

---

**API Preview**

<img width="929" height="893" alt="image" src="https://github.com/user-attachments/assets/22d5a004-f5fd-4683-aad5-8ce152997c39" />


---

## ⚙️ Tech Stack

**Backend**

- .NET / ASP.NET Core
- SQL Server (primary database)
- Redis (caching + in-memory operations + pub/sub)
- JWT Authentication (cookie-based)

**Frontend**

- React
- TypeScript
- Material UI

---

## 🧠 Key Concepts Explored

- Authentication using **JWT + HTTP-only cookies**
- Rate limiting at API level
- **Redis-backed caching layer**
- Background processing for **eventual consistency**
- Separation of concerns via service layer architecture
- RESTful API design

---

## 🏗️ Architecture Overview

Client (React)  
        ↓  
ASP.NET API (Controllers)  
        ↓  
Service Layer (Business Logic)  
        ↓  
SQL Server (source of truth)  
        ↑  
Redis (cache + async sync layer)

---

## 🔐 Authentication Flow

- Login/Register via API
- JWT issued and stored in **HTTP-only cookies**
- Refresh token mechanism for session continuity

Example:

Response.Cookies.Append("accessToken", loginObj.AccessToken, new CookieOptions  
{  
    HttpOnly = true,  
    Secure = true,  
    SameSite = SameSiteMode.Strict  
});

---

## ⚡ Redis Integration

Redis is used for:

- Caching frequently accessed data
- Maintaining relationship sets (followers, likes, etc.)
- Event queues for background syncing

Example key design:

user:{id}:following  
user:{id}:followers  
userLikes:eventsQueue

Reference implementation:

---

## 🔄 Background Sync System

A hosted background service processes in-memory events and syncs them to the database.

### Why?

To balance:

Fast writes (Redis)  
+  
Durable storage (SQL Server)

This introduces:

- eventual consistency
- improved performance under load

---

## 🚦 Rate Limiting

Basic API-level rate limiting is implemented to prevent abuse:

if (await _rateLimiter.IsRequestAllowedAsync(...))

---

## 📦 Features

- User authentication (register/login/logout/refresh)
- Posts, comments, likes
- Follow/unfollow system
- Media upload handling
- Redis-backed caching
- Background sync for data consistency

---

## ⚠️ Trade-offs

This system intentionally explores real-world compromises:

- Redis introduces **eventual consistency**
- Background sync adds **complexity**
- Cookie-based auth requires careful security handling

---

## 🚀 Running the Project
Ensure:

- SQL Server is configured
- Redis is running
- `appsettings.json` has correct connection strings

---

## 📌 Future Improvements

- Distributed rate limiting
- Better cache invalidation strategies
- Horizontal scaling (multi-instance)
- Observability (metrics + tracing)

---

## One-line summary

> A backend-heavy microblog platform exploring caching, concurrency, and real-world system design trade-offs.
