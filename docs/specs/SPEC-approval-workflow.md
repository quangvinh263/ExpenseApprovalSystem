# Spec: Approval Workflow for Expense Requests

## Objective

Define the MVP business rules for submitting, approving, rejecting, and
paying an `ExpenseRequest`. The workflow must be deterministic, role-based,
auditable, concurrency-safe, and implemented later in the Application layer
without allowing API controllers to access `AppDbContext` directly.

This specification is based on:

- `.agents/rules/architecture.md`
- `.agents/context/database-schema.md`
- `src/Domain/Entities/ExpenseRequest.cs`
- `src/Domain/Entities/ApprovalHistory.cs`

## Assumptions

1. All monetary amounts are in Vietnamese đồng (VND); the current MVP schema
   has no currency column.
2. The MVP uses one approval level selected by amount:
   - `Amount <= 10,000,000`: `TeamLead`
   - `Amount > 10,000,000`: `Manager`
3. A request is approved by the required role in the requester's department.
4. A requester cannot approve or reject their own request.
5. `Submitted` is a state-machine checkpoint. Submission performs the
   `Draft -> Submitted -> PendingApproval` routing atomically, so clients
   normally observe `PendingApproval` after a successful submit.
6. Rejected requests are terminal in MVP. Resubmission requires a future
   explicitly specified workflow and is not included here.
7. `Accountant` or `Admin` may mark an approved request as paid.

## State Machine

### States

| State | Meaning |
|---|---|
| `Draft` | Request is being prepared and cannot be approved or paid. |
| `Submitted` | Submission has passed validation and is awaiting automatic routing. |
| `PendingApproval` | The request has a computed `CurrentApproverRole`. |
| `Approved` | The required approver accepted the request. |
| `Rejected` | The request was rejected and is terminal for MVP. |
| `Paid` | Accounting completed reimbursement; terminal state. |

### Allowed transitions

```text
Draft
  └─ Submit + valid request ──> Submitted
                                └─ automatic routing ──> PendingApproval

PendingApproval
  ├─ Approve + authorized current approver ──> Approved
  └─ Reject + authorized current approver ──> Rejected

Approved
  └─ MarkPaid + Accountant/Admin ──> Paid
```

No other transitions are valid. In particular:

- `Draft -> Approved`, `Draft -> Rejected`, and `Draft -> Paid` are invalid.
- `Submitted` cannot be approved or rejected until routing has completed.
- `Approved` cannot be rejected.
- `Rejected` and `Paid` cannot be modified by workflow commands.
- An approval command must not silently recalculate or replace the
  `CurrentApproverRole` stored during submission.

### Submission behavior

The submit operation must execute the following as one transaction:

1. Validate the request is in `Draft`.
2. Transition to `Submitted`.
3. Determine `CurrentApproverRole` from `Amount`.
4. Transition to `PendingApproval`.
5. Update `UpdatedAt`.
6. Append one `ApprovalHistory` record:
   - `Action = "Submit"`
   - `FromStatus = "Draft"`
   - `ToStatus = "PendingApproval"`
   - `ActionByUserId =` submitting user

The intermediate `Submitted` state is part of the domain state machine, but the
audit record represents the complete user action and final routed state.

## CurrentApproverRole Rules

| Amount | `CurrentApproverRole` | Required actor |
|---:|---|---|
| `0 < Amount <= 10,000,000` | `TeamLead` | TeamLead in requester's department |
| `Amount > 10,000,000` | `Manager` | Manager in requester's department |

Additional rules:

- `Amount` must be strictly greater than zero.
- The role comparison is based on the stored role at approval time, not on
  client-provided role data.
- `Admin` may perform an operational override only if an explicit
  authorization policy permits it; this override is outside the normal
  `CurrentApproverRole` path and must still create an audit record.
- The MVP does not split approvals across multiple levels. A future
  multi-level policy must be added as a new specification before implementation.

## Approval and Rejection Behavior

### Approve

An approve command is valid only when:

1. The request exists and is in `PendingApproval`.
2. The request has a non-null `CurrentApproverRole`.
3. The acting user is active and has the required role.
4. The acting user belongs to the request's department.
5. The acting user is not the requester.
6. The request's `RowVersion` still matches the version read by the command.

On success:

- Set `Status = "Approved"`.
- Set `CurrentApproverRole = null`.
- Update `UpdatedAt`.
- Append an `ApprovalHistory` record with:
  `Action = "Approve"`, `FromStatus = "PendingApproval"`,
  `ToStatus = "Approved"`, acting user, timestamp, and optional comment.

### Reject

The same authorization and concurrency checks as approval apply. In addition,
the rejection comment is required and must be non-blank.

On success:

- Set `Status = "Rejected"`.
- Set `CurrentApproverRole = null`.
- Update `UpdatedAt`.
- Append an `ApprovalHistory` record with:
  `Action = "Reject"`, `FromStatus = "PendingApproval"`,
  `ToStatus = "Rejected"`, acting user, timestamp, and comment.

### Mark paid

`MarkPaid` is valid only when the request is `Approved` and the acting user is
an active `Accountant` or `Admin`. On success it transitions to `Paid`, clears
`CurrentApproverRole`, updates `UpdatedAt`, and appends:

- `Action = "MarkPaid"`
- `FromStatus = "Approved"`
- `ToStatus = "Paid"`

## Audit Requirements

`ApprovalHistories` is append-only. No workflow operation may update or delete
an existing history record.

Every successful state-changing user action must append a history entry in the
same database transaction as the `ExpenseRequest` update. Required actions:

- `Submit`
- `Approve`
- `Reject`
- `MarkPaid`

Each entry must include:

- `ExpenseRequestId`
- `ActionByUserId`
- `Action`
- `FromStatus`
- `ToStatus`
- `CreatedAt`
- Optional `Comment` where applicable

If either the request update or history insert fails, the entire operation must
roll back. Failed validation or stale-concurrency attempts must not create a
success-shaped audit entry.

## Validation Rules

### Submit validation

- Request exists.
- Current status is `Draft`.
- Requester exists and is active.
- Department exists and is active.
- `RequesterId` and `DepartmentId` are consistent with the requester's current
  department when the request is submitted.
- `Amount > 0` and fits the database precision `(18,2)`.
- `Category` is one of `Travel`, `Meal`, `Hotel`, `Supplies`, `Other`.
- `Reason` is non-empty after trimming.
- Required routing role can be determined from the amount.

### Approve/reject validation

- Current status is exactly `PendingApproval`.
- `CurrentApproverRole` is one of the supported approval roles.
- Acting user is active, authorized, in the matching department, and is not the
  requester.
- Approval comment is optional; rejection comment is required.
- The optimistic concurrency token (`RowVersion`) matches.
- A single request cannot be approved and rejected concurrently; a stale
  `RowVersion` must produce a conflict and no audit record.

### Paid validation

- Current status is exactly `Approved`.
- Acting user is active and has role `Accountant` or `Admin`.
- The optimistic concurrency token matches.

## API and Application Boundaries

- Commands and command handlers belong in `src/Application/`.
- State transition and authorization rules belong in Domain/Application
  services, not in controllers.
- Controllers accept typed request DTOs/commands and dispatch through MediatR;
  they never call `AppDbContext`.
- Success responses use the existing API response convention.
- Invalid state or authorization requests must return a deliberate conflict or
  forbidden result; they must not be treated as successful no-ops.

Example application-level transition shape:

```csharp
public sealed record ApproveExpenseCommand(
    Guid ExpenseRequestId,
    Guid ApproverId,
    string? Comment,
    byte[] RowVersion) : IRequest<Guid>;
```

## Tech Stack and Commands

- ASP.NET Core 9 Web API
- Entity Framework Core 9 with PostgreSQL
- MediatR for CQRS command dispatch
- JWT and role-based authorization

Verification commands:

```powershell
dotnet restore .\ExpenseApproval.slnx
dotnet build .\ExpenseApproval.slnx --no-restore
dotnet test .\ExpenseApproval.slnx --no-restore
```

## Project Structure

- `src/Domain/` — state concepts and domain invariants; no Infrastructure
  dependency.
- `src/Application/` — commands, handlers, validation, authorization policy
  orchestration, and transaction boundary.
- `src/Infrastructure/` — `AppDbContext`, persistence mappings, and migrations.
- `src/Api/` — HTTP controllers, authentication/authorization configuration,
  and response mapping.
- `docs/specs/` — approved feature specifications.

## Testing Strategy

### Domain/Application unit tests

Cover at minimum:

- Every allowed transition.
- Every invalid transition.
- Amount boundary values `10,000,000` and `10,000,000.01`.
- TeamLead and Manager authorization.
- Same-department and requester-self-approval rules.
- Required rejection comment.
- Terminal behavior for `Rejected` and `Paid`.
- `RowVersion` conflict behavior.
- Audit record contents for Submit, Approve, Reject, and MarkPaid.

### Infrastructure tests

- Verify each successful command updates the request and appends history in
  one transaction.
- Verify a failed history insert does not leave a partially transitioned
  request.
- Verify append-only history behavior.

### API tests

- Verify correct status codes and response shapes.
- Verify invalid state returns conflict, unauthorized role returns forbidden,
  and malformed input returns validation failure.
- Verify no controller test requires direct `AppDbContext` access.

## Boundaries

- **Always:** use the state machine rules; validate current status before every
  transition; enforce role and department authorization; record audit history
  transactionally; use `CancellationToken` for I/O; honor `RowVersion`.
- **Ask first:** changing amount thresholds, adding approval levels, allowing
  resubmission, changing terminal states, or adding new roles/statuses.
- **Never:** allow controllers to mutate entities directly; bypass
  `CurrentApproverRole`; overwrite/delete approval history; trust a
  client-provided role; approve stale entity versions silently.

## Success Criteria

1. The complete MVP state graph is explicitly implemented as the allowed
   transitions above, with invalid transitions rejected.
2. Amount-based routing assigns TeamLead for amounts through 10,000,000 VND
   and Manager above that threshold.
3. Submit, approve, reject, and mark-paid actions each create the required
   append-only `ApprovalHistory` entry transactionally.
4. Authorization, status, input, self-approval, department, and concurrency
   validations are defined and testable.
5. Controllers remain HTTP-only and dispatch CQRS commands through MediatR.
6. The solution can verify the workflow with unit, infrastructure, and API
   tests using the commands in this specification.

## Open Questions

1. Should rejected requests be editable and resubmittable in a later release? The answer: No 
2. Should `Admin` be allowed to approve requests as an operational override,
   or only manage configuration and user access? The answer: No
3. Should approval thresholds become department-configurable instead of fixed
   constants? The answer: Fixed constants
4. Should the schema add a currency column before supporting non-VND expenses? The answer: Ignore
