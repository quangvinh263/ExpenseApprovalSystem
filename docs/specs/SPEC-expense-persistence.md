# Spec: Expense Persistence Model

## Objective

Implement the MVP persistence model from `.agents/context/database-schema.md`:
four Domain `record` entities (`Department`, `User`, `ExpenseRequest`,
`ApprovalHistory`) and an Infrastructure `AppDbContext` with PostgreSQL-ready
Fluent API mappings. Domain must remain independent of Infrastructure and
Entity Framework Core.

## Tech Stack

- C# / .NET 9
- Entity Framework Core with PostgreSQL
- Clean Architecture: Domain entities, Infrastructure persistence mapping

## Project Structure

- `src/Domain/Entities/` — persistence-agnostic entity records
- `src/Infrastructure/Data/` — `AppDbContext` and EF Core configuration

## Code Style

- Use immutable-friendly C# `record` entities with `Guid` identifiers.
- Keep EF Core-specific configuration exclusively in `AppDbContext`.
- Use explicit PostgreSQL column names/types, required/optional constraints,
  unique email index, relationships, and delete behaviors.
- Configure `ExpenseRequest.RowVersion` as a required `bytea` concurrency token.

## Testing Strategy

- Build the affected Domain and Infrastructure projects.
- Verify mappings compile and preserve the schema's keys, foreign keys,
  nullability, decimal precision, string lengths, timestamps, and concurrency
  configuration.

## Boundaries

- Always: keep Domain free of Infrastructure/EF Core references; map all schema
  columns explicitly; preserve append-only intent for approval history.
- Ask first: changing the supplied schema, adding migrations, or changing
  project target frameworks/dependencies.
- Never: put `DbContext` or EF Core attributes in Domain; introduce unrelated
  application behavior.

## Success Criteria

1. Four records exist under `src/Domain/Entities/` with all schema fields and
   navigation properties needed by the declared relationships.
2. `AppDbContext` exists under `src/Infrastructure/Data/` and exposes all four
   `DbSet`s.
3. `OnModelCreating` maps UUID keys, foreign keys, PostgreSQL types, lengths,
   required/nullable fields, unique `Users.Email`, relationship delete
   behaviors, and `RowVersion` as a concurrency token.
4. Domain has no Infrastructure or EF Core dependency.
5. The affected projects build successfully.

## Open Questions

- None for the requested MVP mapping. Existing package/version conventions will
  be preserved; dependency changes require approval.
