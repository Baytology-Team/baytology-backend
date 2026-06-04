<div align="center">

# Baytology

### AI-Powered Real Estate Platform — Backend

A production-grade **.NET 10** Web API backend for an intelligent real estate marketplace,
built with **Clean Architecture**, **Domain-Driven Design**, and **CQRS**.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?style=flat-square&logo=rabbitmq&logoColor=white)](https://www.rabbitmq.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

---

[Architecture](#-architecture) · [Tech Stack](#-tech-stack) · [Features](#-features) · [Getting Started](#-getting-started) · [Configuration](#%EF%B8%8F-configuration) · [Testing](#-testing) · [API Reference](#-api-reference)

</div>

---

## 📐 Architecture

The system follows **Clean Architecture** with strict dependency inversion — all dependencies point inward, and the domain layer has zero external references.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            Baytology.Api                               │
│           Controllers · Middleware · Swagger/OpenAPI · SignalR          │
├─────────────────────────────────────────────────────────────────────────┤
│                         Baytology.Contracts                            │
│                   Request/Response DTOs · API Contracts                │
├─────────────────────────────────────────────────────────────────────────┤
│                        Baytology.Application                           │
│       CQRS Commands & Queries · MediatR Behaviors · Interfaces        │
├─────────────────────────────────────────────────────────────────────────┤
│                       Baytology.Infrastructure                         │
│   EF Core · Identity · RabbitMQ · Paymob · SignalR · AI Fallback      │
├─────────────────────────────────────────────────────────────────────────┤
│                          Baytology.Domain                              │
│       Entities · Value Objects · Domain Events · Result Pattern        │
└─────────────────────────────────────────────────────────────────────────┘
```

> **Dependency Rule** — `Domain` has zero project references. `Application` depends only on `Domain`. `Infrastructure` implements `Application` interfaces. `Api` is the composition root that wires everything together.

---

## 🛠 Tech Stack

| Category | Technologies |
|:---|:---|
| **Runtime** | .NET 10, C# 13 |
| **API Layer** | ASP.NET Core Web API, API Versioning (`Asp.Versioning`), OpenAPI / Swagger |
| **Persistence** | Entity Framework Core 10, SQL Server, 18 Fluent Configurations |
| **Authentication** | ASP.NET Identity, JWT Bearer Tokens, Refresh Token Rotation |
| **OAuth** | Google Sign-In |
| **Messaging** | RabbitMQ 3.13 via `RabbitMQ.Client` |
| **Real-time** | SignalR (Notification Hub + Chat Hub) |
| **Payments** | Paymob Gateway Integration + Local Dev Simulation |
| **Caching** | `HybridCache` (L1 In-Memory + L2 Distributed) |
| **Resilience** | `Microsoft.Extensions.Http.Resilience` (Circuit Breaker, Retry, Timeout) |
| **Logging** | Serilog (Console + Rolling File + Seq sink) |
| **Validation** | FluentValidation |
| **Mediation** | MediatR (Commands, Queries, Notifications, Pipeline Behaviors) |
| **Testing** | xUnit, EF Core InMemory, `WebApplicationFactory` |

---

## ✨ Features

### Core Platform
- **Property Management** — Full CRUD with images, amenities, location data, and property status lifecycle (`Available` → `Sold` / `Rented`)
- **Booking System** — Viewing scheduling with date validation, agent confirmation, and payment attachment
- **Payment Processing** — Paymob integration with escrow flow, webhook verification, refund lifecycle, and local simulation mode for development
- **Real-time Chat** — SignalR-based buyer ↔ agent messaging with conversation groups and participant authorization
- **Notifications** — Persistent notifications pushed via SignalR with read tracking and reference linking
- **User Profiles & Agent Details** — Profile management, agent specializations, and verified agent reviews with rating aggregation

### AI Integration
- **Hybrid AI Pipeline** — RabbitMQ Outbox pattern dispatches search/recommendation requests to external Python microservices; automatic in-process fallback when workers are unavailable
- **Multi-Modal Search** — Text, voice (with transcription), and image-based property search with filter refinement
- **Recommendation Engine** — FAISS vector-based similarity recommendations resolved through the Outbox pipeline
- **AI Chatbot Proxy** — Direct proxy endpoints to an Arabic NLP chatbot and recommendation API
- **Recovery Processor** — Background service that auto-resolves stale pending AI requests

### Administration
- **Admin Dashboard** — Platform analytics, user management, property oversight
- **Refund Review** — Admin workflow for reviewing, approving, and rejecting refund requests
- **AI Request Management** — Manual resolution of stuck search and recommendation requests

---

## 🏗 Engineering Patterns

| # | Pattern | Implementation |
|:--|:---|:---|
| 1 | **Clean Architecture** | 5-project solution with strict dependency inversion |
| 2 | **Domain-Driven Design** | Rich domain entities, domain events, value objects |
| 3 | **CQRS** | Command/Query separation via MediatR |
| 4 | **Result Pattern** | `Result<T>` for explicit error handling — no exceptions for flow control |
| 5 | **Factory Method** | `Property.Create()`, `Payment.Create()`, `Booking.Create()`, etc. |
| 6 | **Outbox Pattern** | `DomainEventLog` table + `OutboxProcessor` background service |
| 7 | **Pipeline Behaviors** | Validation, Caching, Cache Invalidation, Logging, Performance, Exception Handling |
| 8 | **Saga (Choreography)** | Domain Events → Event Handlers → Commands |
| 9 | **Strategy Pattern** | `IAiDispatchPolicy`, `IAiSearchFallbackService` |
| 10 | **Interceptor Pattern** | `AuditableEntityInterceptor`, `DomainEventInterceptor`, `AuditLogInterceptor` |
| 11 | **Background Services** | `OutboxProcessor`, `AiFallbackRecoveryProcessor` |
| 12 | **Global Exception Handling** | Environment-aware error responses — production never leaks internals |

---

## 📁 Project Structure

```
src/
├── Baytology.Domain/                # Zero dependencies — pure business logic
│   ├── AuditLogs/                  # AuditLog entity
│   ├── Common/                     # Base Entity, AuditableEntity, Enums, Constants, Result<T>
│   ├── DomainEvents/               # DomainEventLog (Outbox table), Domain Events
│   ├── Entities/                   # Property, Booking, Payment, Conversation, Notification, etc.
│   ├── Exceptions/                 # Domain-specific error constants (PropertyErrors, BookingErrors)
│   └── ValueObjects/               # SearchRequest, TextSearch, RecommendationRequest, etc.
│
├── Baytology.Application/          # Use cases — no infrastructure knowledge
│   ├── Features/
│   │   ├── Properties/             # CRUD + Search + Save + Views + Reviews
│   │   ├── Bookings/               # Create, Confirm, Cancel, GetMyBookings
│   │   ├── Payments/               # CreatePaymentIntention, ProcessWebhook, Refunds
│   │   ├── Conversations/          # Create, SendMessage, MarkRead, GetConversations
│   │   ├── AISearch/               # Commands, Queries, EventHandlers
│   │   ├── Recommendations/        # Commands, Queries, EventHandlers
│   │   ├── Admin/                  # Dashboard, UserManagement, ReviewRefund
│   │   ├── Identity/               # Register, Login, RefreshToken, OAuth
│   │   └── InternalAi/             # PropertyMappings lookup for AI workers
│   └── Common/
│       ├── Behaviours/             # 6 MediatR pipeline behaviors
│       ├── Caching/                # ICacheable, ICacheInvalidation, CacheTags
│       └── Interfaces/             # 16 port interfaces (IAppDbContext, IPaymentGateway, etc.)
│
├── Baytology.Infrastructure/       # All external concerns
│   ├── AI/                         # Fallback services, dispatch policy, external API clients
│   ├── BackgroundJobs/             # OutboxProcessor, AiFallbackRecoveryProcessor
│   ├── Caching/                    # HybridQueryCache implementation
│   ├── Data/                       # AppDbContext, 18 EF Configurations, Migrations, Seeders
│   ├── Identity/                   # IdentityService, TokenProvider, ExternalLoginValidator
│   ├── Interceptors/               # AuditableEntity, DomainEvent, AuditLog interceptors
│   ├── Messaging/                  # RabbitMqPublisher
│   ├── Notifications/              # NotificationService (DB + SignalR push)
│   ├── Payments/                   # PaymobGateway with resilience policies
│   ├── RealTime/                   # NotificationHub, ChatHub
│   └── Settings/                   # Strongly-typed configuration POCOs
│
├── Baytology.Contracts/            # Shared DTOs (Request/Response)
│
└── Baytology.Api/                  # Composition root
    ├── Controllers/                # 15 versioned API controllers
    ├── Infrastructure/             # GlobalExceptionHandler, Middleware
    ├── OpenApi/                    # Swagger transformers (Bearer auth, version info)
    └── Program.cs                  # Application bootstrap

tests/
├── Baytology.Domain.Tests/         # 24 unit tests — entities, validation, domain events
├── Baytology.Application.Tests/    # 38 unit tests — handlers, behaviors, persistence
└── Baytology.Api.Tests/            # 16 integration tests — WebApplicationFactory
```

---

## 🚀 Getting Started

### Prerequisites

| Requirement | Version |
|:---|:---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ |
| [SQL Server](https://www.microsoft.com/en-us/sql-server) | 2019+ (LocalDB or full instance) |
| [Docker](https://www.docker.com/) | Latest (optional — for RabbitMQ) |

### 1 — Clone & Restore

```bash
git clone https://github.com/Youssef-AbdelRaafi/Baytology.git
cd Baytology
dotnet restore
```

### 2 — Configure Secrets

Sensitive values must be set via [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) — they are **never committed** to source control:

```bash
cd src/Baytology.Api

dotnet user-secrets set "JwtSettings:Secret" "your-256-bit-secret-key-minimum-32-characters"
dotnet user-secrets set "AdminSettings:DefaultPassword" "replace-with-local-admin-password"
```

### 3 — Create the Database

```bash
dotnet ef database update --project ../Baytology.Infrastructure
```

### 4 — Run

```bash
dotnet run --project src/Baytology.Api
```

The API will be available at `https://localhost:5001` with Swagger UI at `/swagger`.

### 5 — Start RabbitMQ *(optional)*

```bash
docker compose -f docker-compose.rabbitmq.yml up -d
```

Management UI: [http://localhost:15672](http://localhost:15672) — `guest` / `guest`

---

## ⚙️ Configuration

All settings are managed through `appsettings.json` with environment overrides and user secrets:

| Section | Key Settings | Purpose |
|:---|:---|:---|
| `ConnectionStrings` | `DefaultConnection` | SQL Server connection string |
| `JwtSettings` | `Secret`, `Issuer`, `Audience`, `AccessTokenExpiration`, `RefreshTokenExpiration` | JWT authentication |
| `RabbitMq` | `Enabled`, `HostName`, `Port`, queue names | Message broker for AI pipeline |
| `Paymob` | `EnableLocalSimulation`, `ApiKey`, `SecretKey`, `IntegrationId` | Payment gateway |
| `AiProcessing` | `EnableInProcessFallback`, `EnableDelayedFallbackRecovery` | AI fallback behavior |
| `ExternalAiServices` | `ChatbotBaseUrl`, `RecommendationBaseUrl`, `VoiceRecognitionBaseUrl`, `ImageSearchBaseUrl`, `TimeoutSeconds` | External AI API endpoints |
| `AiWorker` | `ServiceToken` | Shared secret for AI worker authentication |
| `Email` | `DeliveryMode`, `SmtpHost`, `FromAddress` | Email delivery (SMTP or file pickup) |
| `GoogleAuthSettings` | `ClientId`, `ClientSecret` | Google OAuth |

| `AdminSettings` | `DefaultEmail`, `DefaultPassword` | Initial admin seed credentials |
| `Serilog` | Sinks, levels, overrides | Structured logging |

### Development Quick Switches

| Scenario | Configuration |
|:---|:---|
| Run without RabbitMQ | `RabbitMq:Enabled = false` + `AiProcessing:EnableInProcessFallback = true` |
| Simulate payments locally | `Paymob:EnableLocalSimulation = true` |
| Disable external AI services | `ExternalAiServices:ChatbotEnabled = false` + `ExternalAiServices:VoiceRecognitionEnabled = false` |

---

## 🧪 Testing

```bash
# Run all 78 tests
dotnet test

# Run by layer
dotnet test tests/Baytology.Domain.Tests
dotnet test tests/Baytology.Application.Tests
dotnet test tests/Baytology.Api.Tests
```

| Layer | Tests | Scope |
|:---|:---:|:---|
| **Domain** | 24 | Entity creation, validation rules, state transitions, domain events |
| **Application** | 38 | Command/query handlers, pipeline behaviors, persistence logic |
| **API Integration** | 16 | Full HTTP endpoint flows via `WebApplicationFactory` |

The test suite covers:
- Entity validation and invariant enforcement
- Domain event raising and Outbox persistence
- MediatR pipeline behavior execution order
- Authentication and authorization flows
- Admin, Agent, and Buyer endpoint journeys
- Payment webhook processing
- AI search and recommendation resolution

---

## 📡 API Reference

### Endpoint Overview

| Area | Key Endpoints | Authorization |
|:---|:---|:---|
| **Identity** | Register, Login, Refresh Token, OAuth (Google), Change Password, Forgot/Reset Password, Email Confirmation | Public / Authenticated |
| **Properties** | CRUD, Search, Filter, Save/Unsave, Record View, Agent Reviews | Agent / Buyer |
| **Bookings** | Create, Confirm, Cancel, List (Buyer & Agent) | Buyer / Agent |
| **Conversations** | Create, Send Message, Mark Read, List | Authenticated |
| **Payments** | Create Intention, Webhook, Refund Request/Review | Buyer / Admin |
| **AI Search** | Create (Text/Voice/Image), Get Status, Admin Resolve | Authenticated / Admin |
| **Recommendations** | Create, Get Status, Admin Resolve | Authenticated / Admin |
| **AI Assistant** | Parse, Question, Search, Rank, Chat, Voice Chat, Image Search, Recommend (proxy) | Authenticated |
| **Admin** | Dashboard Analytics, User Management, Refund Review, Property Oversight | Admin |
| **Notifications** | List, Mark Read | Authenticated |
| **User Profiles** | Get, Update, Upload Avatar | Authenticated |

### Real-time Hubs

| Hub | Route | Purpose |
|:---|:---|:---|
| `NotificationHub` | `/hubs/notifications` | Push notifications to connected users |
| `ChatHub` | `/hubs/chat` | Real-time buyer ↔ agent messaging |

Connect with a JWT token:
```
wss://localhost:5001/hubs/notifications?access_token=<jwt>
```

Full interactive API documentation is available at `/swagger` in Development mode.

---

## 🔐 Security

| Measure | Details |
|:---|:---|
| **JWT Bearer Auth** | Access tokens with configurable expiration + refresh token rotation |
| **Role-Based Access** | Three roles: `Buyer`, `Agent`, `Admin` — enforced at controller level |
| **OAuth 2.0** | Google external login with token validation |
| **Rate Limiting** | Sliding window (100 req/min) on all endpoints |
| **CORS** | Configurable allowed origins — strict validation in production |
| **Webhook Security** | Constant-time comparison (`CryptographicOperations.FixedTimeEquals`) for Paymob and AI worker tokens |
| **Error Handling** | Environment-aware — production responses never expose stack traces or internal details |
| **User Secrets** | All sensitive keys stored via `dotnet user-secrets` — never committed to source |

---

## 👥 Roles & Permissions

| Role | Capabilities |
|:---|:---|
| **Buyer** | Browse and search properties, save/unsave favorites, book viewings, make payments, chat with agents, use AI search and recommendations |
| **Agent** | List and manage properties, handle booking requests, receive payments, chat with buyers, manage agent profile |
| **Admin** | Platform dashboard, user management, review refund requests, resolve AI requests, property oversight |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">

**Built with** ❤️ **using .NET 10, Clean Architecture, and Domain-Driven Design**

</div>
