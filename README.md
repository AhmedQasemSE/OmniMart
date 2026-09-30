# 🛒 OmniMart - Enterprise E-Commerce Backend

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Redis](https://img.shields.io/badge/redis-%23DD0031.svg?style=for-the-badge&logo=redis&logoColor=white)
![Stripe](https://img.shields.io/badge/Stripe-626CD9?style=for-the-badge&logo=Stripe&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)

OmniMart is a highly scalable, multi-tenant e-commerce backend built with the latest **.NET 10** and **C#**. Designed with enterprise-grade standards, it strictly follows **Clean Architecture** and implements **Domain-Driven Design (DDD)**, **CQRS**, and **Event-Driven Architecture**. The system seamlessly handles complex e-commerce workflows, including multi-role authorization, dynamic vendor management, robust order state machines, and secure payment processing.

## 🏗️ Architecture & Core Patterns
*   **Clean Architecture:** Strict separation of concerns across Domain, Application, Infrastructure, and API layers to ensure maintainability and testability.
*   **Domain-Driven Design (DDD):** Rich domain models with strict encapsulation, value objects (e.g., `Address`), domain events, and invariant enforcement.
*   **CQRS Pattern:** Segregation of Commands and Queries using **MediatR**. Queries are heavily optimized using `.AsNoTracking()` for high-speed read operations.
*   **Event-Driven Architecture:** In-process domain events (via custom `SaveChangesInterceptor` and MediatR `INotificationHandler`) decouple core logic from side effects like email notifications, SMS, and webhook triggers.
*   **Repository & Unit of Work:** Abstracts Entity Framework Core data access, ensuring transactional integrity across multiple operations.

## ✨ Advanced Engineering Features

### 🔐 Security & Authentication
*   **Multi-Role RBAC:** Distinct permission matrices for `SuperAdmin`, `Admin`, `Staff` (IT, HR, Finance, etc.), `Vendor`, and `Customer`.
*   **Secure Token Management:** JWT Bearer tokens for access, paired with **HTTP-Only, Secure, Same-Site cookies** for Refresh Tokens to mitigate XSS and CSRF attacks.
*   **Global Exception Handling:** Utilizes .NET `IExceptionHandler` to intercept errors globally, logging them with Serilog and returning standardized `ProblemDetails` with generated `TraceId`s to the client.
*   **API Rate Limiting:** Partitioned IP Rate Limiter to prevent brute-force attacks and abuse.

### 💳 Order Processing & Payments
*   **State Machine Orders:** Strict state transitions (Pending ➔ Processing ➔ Shipped ➔ Delivered) with robust cancellation, refund policies, and automatic stock restoration.
*   **Idempotency:** Custom API idempotency filters (`[Idempotent]`) backed by Redis prevent duplicate processing and double-charging during critical endpoints like Checkout.
*   **Stripe Webhooks:** Secure integration with Stripe Checkout. Webhooks validate digital signatures to asynchronously confirm payments, distribute vendor revenues, and deduct committed stock.
### 🚀 Performance, Caching & Resilience
*   **Distributed Caching (Redis):** Implemented heavily via a custom MediatR `CachingBehavior` pipeline to cache frequently accessed read-queries (e.g., categories, product searches) and manage OTP lifecycles.
*   **Optimistic Concurrency:** `RowVersion` implementation on critical entities (Orders, Products, Wallets, Categories) prevents race conditions and data corruption during high-traffic concurrent modifications.
*   **Background Jobs (Hangfire):** Asynchronous task processing for sending emails, SMS, and recurring maintenance tasks (e.g., `AbandonedCartCleanupJob` which clears expired payment sessions and releases reserved stock).
*   **Resilience (Polly):** Exponential backoff and retry policies configured for external HTTP clients to handle transient network failures gracefully.

### 🛍️ Catalog, Customers & Vendor Management
*   **Dynamic Product Catalog:** Supports products with hierarchical categories, category-specific required attributes, dynamic variants (SKUs), and stock quantity tracking.
*   **Cloud Media Storage:** Direct integration with **Cloudinary** for uploading, resizing, and managing product images.
*   **Vendor Ecosystem:** Comprehensive vendor onboarding, commercial register validation workflows, automated commission rate tracking, and dynamic wallet balances.
*   **Customer Profiles:** Management of multiple shipping addresses and a built-in Loyalty Points system (Earn/Redeem).

## 🛠️ Tech Stack & Libraries
*   **Framework:** .NET 10 / ASP.NET Core Web API
*   **Database:** SQL Server (EF Core 10)
*   **Caching:** StackExchange.Redis
*   **Message Dispatching & Pipelines:** MediatR
*   **Validation:** FluentValidation
*   **Payments:** Stripe.net
*   **Media Storage:** CloudinaryDotNet
*   **Background Processing:** Hangfire (SQL Server Storage)
*   **Resilience:** Polly
*   **Logging & Observability:** Serilog (Console, Rolling File, Seq)
*   **Testing:** xUnit, Moq, FluentAssertions *(See [TESTING.md](TESTING.md) for details)*

## 📂 Project Structure
```text
OmniMart/
 ├── OmniMart.Domain/           # Entities, Enums, Value Objects, and Domain Events
 ├── OmniMart.Application/      # CQRS Handlers, DTOs, Interfaces, Validation/Caching Behaviors
 ├── OmniMart.Infrastructure/   # EF Core DbContext, Repositories, Stripe, Redis, MailKit, Cloudinary
 ├── OmniMart/                  # Controllers, Middlewares, Webhooks, Idempotency Filters
 └── OmniMart.Tests/            # Unit Tests & Mock Data
```

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop (for Redis and Seq)
- SQL Server (LocalDB or Docker image)

### Setup Instructions

```text
1. Clone the repository:
git clone https://github.com/AhmedQasemSE/OmniMart.git
cd OmniMart

2. Start Infrastructure Services via Docker:
docker run -d -p 6379:6379 --name omnimart-redis redis
docker run -d -e ACCEPT_EULA=Y -p 5341:80 --name omnimart-seq datalust/seq

3. Configure Environment Variables (User Secrets):
Add your sensitive keys to .NET User Secrets or an appsettings.Development.json file.
{
  "StripeSettings": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "CloudinarySettings": {
    "CloudName": "...",
    "ApiKey": "...",
    "ApiSecret": "..."
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "OmniMart",
    "SenderEmail": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}

4. Apply Database Migrations:
dotnet ef database update --project OmniMart.Infrastructure --startup-project OmniMart

5. Run the Application:
dotnet run --project OmniMart

Navigate to https://localhost:7159/swagger to explore the API. The system automatically seeds a default SuperAdmin account upon the first run.
```
