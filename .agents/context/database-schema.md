# Database Schema: Expense Approval System

The system uses PostgreSQL. The schema below is for the MVP phase.

## 1. Departments
| Column | Type | Note |
|--------|------|------|
| Id | uuid (PK) | |
| Name | varchar(100) | |
| MonthlyBudget | decimal(18,2) | Monthly allocated budget |
| IsActive | boolean | default true |
| CreatedAt | timestamptz | |

## 2. Users
| Column | Type | Note |
|--------|------|------|
| Id | uuid (PK) | |
| FullName | varchar(150) | |
| Email | varchar(150) | unique |
| PasswordHash | text | |
| Role | varchar(30) | Employee, TeamLead, Manager, Accountant, Admin |
| DepartmentId | uuid (FK → Departments) | nullable for Admin |
| IsActive | boolean | default true |
| CreatedAt | timestamptz | |

## 3. ExpenseRequests
| Column | Type | Note |
|--------|------|------|
| Id | uuid (PK) | |
| RequesterId | uuid (FK → Users) | |
| DepartmentId | uuid (FK → Departments) | Snapshot of user's dept at creation time |
| Amount | decimal(18,2) | |
| Category | varchar(50) | Travel, Meal, Hotel, Supplies, Other |
| Reason | text | |
| ReceiptUrl | text | nullable (link to receipt file) |
| Status | varchar(30) | Draft, Submitted, PendingApproval, Approved, Rejected, Paid |
| CurrentApproverRole | varchar(30) | nullable – role currently required to approve |
| CreatedAt | timestamptz | |
| UpdatedAt | timestamptz | |
| RowVersion | bytea | Concurrency token |

## 4. ApprovalHistories
| Column | Type | Note |
|--------|------|------|
| Id | uuid (PK) | |
| ExpenseRequestId | uuid (FK → ExpenseRequests) | |
| ActionByUserId | uuid (FK → Users) | |
| Action | varchar(30) | Submit, Approve, Reject, MarkPaid |
| FromStatus | varchar(30) | |
| ToStatus | varchar(30) | |
| Comment | text | nullable |
| CreatedAt | timestamptz | |

## Business Notes
- Basic Flow: Draft → Submitted → PendingApproval → Approved → Paid.
- Can be Rejected at the PendingApproval step.
- `ApprovalHistories` is append-only (no updates/deletes).
- Version 1 (MVP) does not need a separate Budget table; rely on `Departments.MonthlyBudget`.