# C# .NET Backend Architecture Rules

## 1. Core Stack (MVP)
- Framework: ASP.NET Core 9 (Web API)
- ORM: Entity Framework Core 9 + PostgreSQL
- Auth: JWT + Role-based Authorization
- Validation: FluentValidation
- (Optional later) MediatR for lightweight CQRS

## 2. Architecture
- Apply **Clean Architecture**:
  - Domain
  - Application
  - Infrastructure
  - Api
- Controllers handle HTTP concerns only. NEVER call `DbContext` directly.
- Business logic resides in the Application layer (Services or Handlers).
- Domain layer contains entities + core business logic (expense state transitions, approval rules).

## 3. Approval Workflow
- Manage Expense state using a State Machine (prefer the `Stateless` library or a simple Domain implementation).
- Do NOT use long if/else or switch chains to mutate `Status`.
- Every state transition must be recorded in `ApprovalHistories`.

## 4. Coding Standards
- Use `record` for DTOs, Commands, Queries.
- 100% of I/O operations must use `async/await` + `CancellationToken`.
- Standardize responses via a wrapper class `ApiResponse<T>`.
- Use clear Enums or Constants for Roles and Statuses (no scattered hardcoded strings).
- Implement basic soft concerns: CreatedAt, UpdatedAt; consider soft delete if necessary.

## 5. What NOT to do in the early stages (MVP Scope)
- Do NOT add Message Brokers (RabbitMQ/MassTransit) until there is a real necessity.
- Do NOT over-engineer with multi-tenancy, event sourcing, or complex CQRS.
- Prioritize getting the core flow working: Create Request → Approve → Mark as Paid.