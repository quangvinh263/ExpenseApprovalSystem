# AGENTS.md - Expense Approval System

## Project Overview
This is an internal **Expense Approval System** built with ASP.NET Core.
Employees submit expense claims (travel, meals, supplies...), managers approve them through a multi-level workflow, and accounting processes reimbursement.

## Tech Stack
- ASP.NET Core 9 (Web API)
- Entity Framework Core
- PostgreSQL (preferred) or SQL Server
- Clean Architecture (Domain → Application → Infrastructure → API)
- JWT Authentication + Role-based Authorization
- SignalR (optional - for real-time notifications)
- AI feature: Invoice/Receipt extraction using LLM

## Main Modules
1. Identity & Roles (Employee, TeamLead, Manager, Accountant, Admin)
2. Expense Request (create, upload receipt, submit)
3. Approval Workflow (multi-level, amount-based rules)
4. Budget Management (per department / project)
5. Reporting
6. AI Invoice Extraction (upload image/PDF → auto-fill amount, date, vendor, category)

## Development Rules (must follow)

### Before writing significant code
- Always write or update a clear Spec first (use skill: spec-driven-development)
- Break work into small, verifiable tasks (use skill: planning-and-task-breakdown)

### While coding
- Prefer incremental implementation (one vertical slice at a time)
- Follow Test-Driven Development for business logic
- Keep controllers thin, put business logic in Application layer
- Use strong typing and clear DTOs
- Never hardcode magic numbers/strings for roles, statuses, or approval limits

### Code Quality
- Every non-trivial change should be reviewable with the five-axis review (correctness, readability, architecture, security, performance)
- Write unit tests for domain logic and application services
- Validate all user inputs
- Do not commit secrets or connection strings

### Preferred Skills
When working on this project, prioritize these skills:
- spec-driven-development
- planning-and-task-breakdown
- incremental-implementation
- test-driven-development
- api-and-interface-design
- code-review-and-quality

## Architecture Notes
- Use Clean Architecture
- Domain layer has no dependency on Infrastructure
- Approval workflow should be modeled clearly (state machine style is preferred)
- Support soft delete and basic audit fields (CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)