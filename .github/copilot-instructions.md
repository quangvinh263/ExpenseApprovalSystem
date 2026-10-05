# Expense Approval System - Copilot Instructions

## Project Context
You are helping build an internal Expense Approval System using ASP.NET Core.
Employees submit expense claims with receipts. The system supports multi-level approval workflow and later reimbursement by accounting.

## Tech Stack
- ASP.NET Core 9 Web API
- Entity Framework Core
- PostgreSQL
- Clean Architecture
- JWT + Role-based Authorization
- FluentValidation
- MediatR (optional but preferred for CQRS style)

## Coding Standards

### Architecture
- Follow Clean Architecture strictly
- Controllers only handle HTTP concerns
- Business logic lives in Application layer
- Domain entities should be rich when possible
- Use DTOs for all API requests/responses

### Naming & Style
- Use clear and intention-revealing names
- Prefer async/await for all I/O
- Use record for DTOs when appropriate
- Enums for Status and Roles

### Expense Domain Rules (important)
- Expense statuses: Draft, Submitted, PendingApproval, Approved, Rejected, Paid
- Roles: Employee, TeamLead, Manager, Accountant, Admin
- Approval can be multi-level depending on amount
- Always keep an approval history (who, when, decision, comment)

### Testing
- Prefer writing tests before implementation for business logic
- Focus unit tests on Domain and Application layers
- Do not skip verification

### AI Feature
- There will be an Invoice Extraction feature (upload receipt image/PDF → extract amount, date, vendor, category)
- Keep the AI service behind an interface so it can be mocked easily

## When Generating Code
- Always consider security (authorization, validation, no over-posting)
- Prefer small, focused changes
- Explain important design decisions briefly
- Follow the existing project structure when it exists