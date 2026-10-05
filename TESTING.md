# 🧪 OmniMart - Testing Strategy & Documentation

![xUnit](https://img.shields.io/badge/xUnit-2.9.3-512BD4?style=for-the-badge&logo=dotnet)
![Moq](https://img.shields.io/badge/Moq-4.20-blue?style=for-the-badge)
![FluentAssertions](https://img.shields.io/badge/FluentAssertions-8.10-00A551?style=for-the-badge)
![Coverage](https://img.shields.io/badge/Tests-200%2B_Passing-success?style=for-the-badge)

The OmniMart platform maintains enterprise-grade reliability and high code quality through a rigorous, well-architected test suite. The project contains over **218 comprehensive unit tests** that isolate and verify every layer of the Clean Architecture, ensuring business invariants are strictly enforced and regressions are prevented.

## 🛠️ Testing Tech Stack
*   **Test Framework:** [xUnit](https://xunit.net/) for test execution and lifecycle management.
*   **Mocking Framework:** [Moq](https://github.com/moq/moq4) for strict dependency isolation.
*   **Query Mocking:** [MockQueryable.Moq](https://github.com/romantitov/MockQueryable) for testing Entity Framework Core `IQueryable` operations without the overhead of an In-Memory Database.
*   **Assertions:** [FluentAssertions](https://fluentassertions.com/) for highly readable, descriptive, and chainable test validations.
*   **Validation Testing:** `FluentValidation.TestHelper` for streamlined verification of validation rules.

## 🏗️ Advanced Testing Patterns

### 1. The Builder Pattern (Test Data Generation)
To maintain the **DRY (Don't Repeat Yourself)** principle and keep test files clean, the suite utilizes the **Builder Pattern** for entity instantiation (`UserBuilder`, `ProductBuilder`, `OrderBuilder`, `CartBuilder`, etc.). 
*   Bypasses private setters using Reflection when necessary.
*   Encapsulates complex state setup (e.g., creating a `Product` and automatically transitioning it through `Draft -> Pending -> Active` states for specific test scenarios).

### 2. Complete Dependency Isolation
*   **Application Layer:** Handlers are tested by mocking the `IUnitOfWork`, `IAppDbContext`, and `ICurrentUserService`.
*   **External Services:** Services like Stripe, Cloudinary, and Email Dispatchers are fully mocked to ensure tests run fast and offline.

## 🎯 Test Coverage Breakdown

### 1. Domain Layer Tests (Business Invariants)
Tests directly assert the behaviors of Domain Entities, ensuring that the core business logic remains untainted.
*   **State Machines:** Verifying strict transitions (e.g., `Order` cannot transition from `Cancelled` to `Shipped`).
*   **Encapsulation:** Ensuring exceptions (`InvalidOperationException`, `ArgumentException`) are thrown when business rules are violated (e.g., negative stock, exceeding maximum allowed images).
*   **Domain Events:** Asserting that the correct events (`ProductOutOfStockEvent`, `OrderRefundedEvent`) are added to the entity's event collection upon specific state changes.

### 2. Application Layer Tests (CQRS Handlers)
Every Command and Query handler is thoroughly tested against multiple scenarios:
*   **Happy Paths:** Ensuring successful execution, accurate data mapping, and appropriate `SaveChangesAsync` calls.
*   **Security & Authorization:** Verifying that users cannot access or modify resources they do not own (e.g., a Vendor updating another Vendor's product).
*   **Concurrency Control:** Simulating `DbUpdateConcurrencyException` to ensure the system gracefully returns HTTP 409 Conflict errors when race conditions occur.
*   **Caching Verification:** Asserting that distributed cache keys (Redis) are accurately invalidated (`RemoveAsync`) during update/delete commands.

### 3. API Layer Tests (Controllers)
Controllers are tested to ensure they correctly interpret the `Result<T>` wrapper from MediatR and return the appropriate HTTP Status Codes:
*   `200 OK` for successful queries and commands.
*   `400 BadRequest` for logical failures.
*   `401 Unauthorized` for missing or invalid tokens.
*   `404 NotFound` when requested resources do not exist.
*   `409 Conflict` for state or concurrency conflicts.
*   `422 Unprocessable Entity` for validation pipeline failures.

### 4. Validation Layer Tests
Using `FluentValidation.TestHelper`, every command query is tested against boundary conditions:
*   Null, empty, and whitespace strings.
*   Maximum length constraints.
*   Negative values for prices and quantities.
*   Validating exact lengths (e.g., 12-character `AccountNumber`).

## 🚀 Running the Tests
To execute the test suite locally, use the .NET CLI:

```bash
# Run all tests
dotnet test OmniMart.Tests

# Run tests with detailed verbosity
dotnet test OmniMart.Tests -v normal

# Run tests and collect code coverage data
dotnet test OmniMart.Tests --collect:"XPlat Code Coverage"
```
