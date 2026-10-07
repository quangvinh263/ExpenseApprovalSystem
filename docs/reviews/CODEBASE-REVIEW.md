# Codebase Review: Expense Approval System

**Ngày review:** 2026-10-06  
**Phạm vi:** Domain, Application, Infrastructure, API và workflow
`Submit -> Approve/Reject -> MarkPaid`

## Review Summary

**Verdict:** REQUEST CHANGES

Các vấn đề Critical C1-C3 và Required Changes R1-R9 đã được xử lý. R10 còn mở
ở phần infrastructure/API automated tests.

## Critical Issues

Không còn issue Critical mở:

- **C1:** Actor identity lấy từ JWT claims; endpoint đã có authorization policies.
- **C2:** Development seeding tách riêng, chỉ chạy trong Development, password
  được lấy từ cấu hình ngoài source và hash bằng `PasswordHasher<User>`.
- **C3:** Create kiểm tra requester/department, quyền sở hữu department và các
  invariant của expense trước khi insert.

## Required Changes

### R1. Draft gán approver role sớm — ĐÃ HOÀN TẤT

Draft không còn gán `CurrentApproverRole`; role chỉ được tính khi Submit.

### R2. State machine — ĐÃ HOÀN TẤT

[`ExpenseWorkflow`](../../src/Domain/Workflow/ExpenseWorkflow.cs) tập trung
status, transition và guard. Submit mô hình hóa checkpoint
`Draft -> Submitted -> PendingApproval`; các transition không hợp lệ bị chặn.

### R3. Hard-coded workflow values — ĐÃ HOÀN TẤT

Status, role, action và category constants đã được tập trung trong
[`ExpenseWorkflow`](../../src/Domain/Workflow/ExpenseWorkflow.cs), còn
database mapping vẫn dùng string theo schema.

### R4. Create-time validation — ĐÃ HOÀN TẤT

[`CreateExpenseCommandHandler`](../../src/Application/Expenses/CreateExpenseCommandHandler.cs)
kiểm tra requester/department active, department consistency, amount, category
và reason trước khi insert. Validation hiện nằm ở Application layer; chưa thêm
FluentValidation riêng vì không cần thiết cho phạm vi hiện tại.

### R5. RowVersion read/API contract — ĐÃ HOÀN TẤT

`GET /api/expenses/{id}` trả DTO có `RowVersion` dạng Base64 để client lấy
version mới nhất trước state-changing commands.

### R6. Error mapping — ĐÃ HOÀN TẤT

Global exception handler trong `Program.cs` chuẩn hóa `400`, `403`, `404`,
`409` và `500`. Một số try/catch cục bộ còn tồn tại nhưng không làm mất
mapping tập trung.

### R7. Append-only ApprovalHistory — ĐÃ HOÀN TẤT

`AppDbContext.SaveChangesAsync` từ chối entry `Modified` hoặc `Deleted` của
`ApprovalHistory`. Database trigger/permission là hardening tùy chọn.

### R8. Migration và async I/O — ĐÃ HOÀN TẤT

Startup dùng `MigrateAsync`; development seed được tách riêng, chỉ chạy khi
Development và có `DevelopmentSeed:Password`. Seeder dùng async query/write.

### R9. Logic workflow bị lặp — ĐÃ HOÀN TẤT

Transition status đã được tập trung trong Domain workflow. Validation approver
giữa Approve và Reject đã được tách thành
[`ApproverAuthorizationPolicy`](../../src/Application/Expenses/ApproverAuthorizationPolicy.cs)
dùng chung; handlers chỉ thực hiện orchestration và persistence.

### R10. Automated tests — ĐANG MỞ, ĐÃ CẢI THIỆN

Đã thêm project
[`ExpenseApproval.Application.Tests`](../../tests/ExpenseApproval.Application.Tests/)
và 5 unit tests cho transition cùng boundary amount. `dotnet test` đã pass.

Chưa có infrastructure/API tests cho:

- Sai role, khác department, tự approve và reject thiếu comment.
- Mark Paid bởi role không hợp lệ.
- RowVersion conflict, audit history và transaction rollback.
- HTTP status `400/403/404/409`.

## Verification Story

- **Build:** Đạt — solution build thành công với 0 warning và 0 error.
- **Tests:** Đạt một phần — 5 unit tests workflow pass; infrastructure/API
  coverage còn thiếu.
- **Security:** Đạt ở mức workflow hiện tại — JWT, policies, actor claims và
  development-only hashed seed đã được áp dụng.
- **Architecture:** Đạt — state machine ở Domain và approver policy dùng chung
  ở Application.
- **Performance:** Đạt ở mức hiện tại — workflow dùng async và `AsNoTracking`,
  chưa có list endpoint tạo N+1.

## Open Follow-up

1. Bổ sung infrastructure và API automated tests.
2. Cân nhắc chuyển migration execution sang deployment pipeline production.

## Optional Improvements

- Thêm endpoint xem approval history.
- Thêm idempotency/rate limiting cho command state-changing.
- Thêm structured logging và correlation ID.

## What's Done Well

- Controller không gọi trực tiếp `AppDbContext`.
- MediatR command/handler nằm ở Application.
- Submit/Approve/Reject/MarkPaid đều tạo `ApprovalHistory`.
- Handler dùng `CancellationToken`.
- Query dùng `AsNoTracking()` trước khi clone record.
- Có optimistic concurrency với `RowVersion`.
- Sai trạng thái ánh xạ thành `409`.
- Mark Paid dùng `StatusCode(403)`.
- Hướng phụ thuộc Domain -> Application -> Infrastructure được giữ đúng.
