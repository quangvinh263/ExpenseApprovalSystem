# Database Schema: Expense Approval System
Hệ thống sử dụng PostgreSQL. Các bảng cốt lõi:

## 1. Departments
- `Id` (Guid, PK)
- `Name` (string)
- `MonthlyBudget` (decimal)

## 2. Users
- `Id` (Guid, PK)
- `FullName`, `Email` (string)
- `Role` (Enum: Employee, Manager, Accountant)
- `DepartmentId` (Guid, FK)

## 3. ExpenseRequests
- `Id` (Guid, PK)
- `RequesterId` (Guid, FK -> Users)
- `Amount` (decimal)
- `Reason` (string)
- `ReceiptUrl` (string, nullable)
- `Status` (Enum: Draft, Pending_Audit, Pending_Manager, Approved, Rejected, Paid)
- `CreatedAt`, `UpdatedAt` (DateTimeOffset)
- `RowVersion` (byte[], Concurrency Token xử lý Race Condition)

## 4. AuditLogs (Append-only)
- `Id` (Guid, PK)
- `ExpenseRequestId` (Guid, FK)
- `ActionBy` (string)
- `Action` (string)
- `Timestamp` (DateTimeOffset)