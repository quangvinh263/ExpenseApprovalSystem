# Codebase Review: Expense Approval System

**Ngày review:** 2026-10-06  
**Phạm vi:** Domain, Application, Infrastructure, API và workflow
`Submit -> Approve/Reject -> MarkPaid`  
**Tài liệu đối chiếu:**

- [`docs/specs/SPEC-approval-workflow.md`](../specs/SPEC-approval-workflow.md)
- [`.agents/rules/architecture.md`](../../.agents/rules/architecture.md)
- [`.agents/context/database-schema.md`](../../.agents/context/database-schema.md)

## Review Summary

**Verdict:** REQUEST CHANGES

Codebase đã có skeleton Clean Architecture và workflow cơ bản, nhưng chưa sẵn
sàng production. Các vấn đề ưu tiên cao nhất còn lại là credentials plaintext
được seed trong startup, validation Create chưa đầy đủ và chưa có test tự động.

## Critical Issues

Không còn issue Critical mở trong phạm vi review này. C1 đã được xử lý bằng
JWT authentication, authorization policies và actor identity lấy từ JWT claims.

### C2. Credentials plaintext được seed trong startup

**File:** [`src/Api/Program.cs`](../../src/Api/Program.cs)

Startup đang seed:

```csharp
PasswordHash = "123"
```

Đây không phải password hash và có thể bị deploy ngoài ý muốn.

**Khuyến nghị:**

- Không seed credentials trong production startup.
- Dùng password hasher thực sự.
- Chỉ seed development bằng code/configuration riêng.
- Không commit secret hoặc mật khẩu cố định.

### C3. Create cho phép dữ liệu identity do client kiểm soát

**File:** [`src/Application/Expenses/CreateExpenseCommandHandler.cs`](../../src/Application/Expenses/CreateExpenseCommandHandler.cs)

Create chưa xác minh requester tồn tại, active, thuộc department được gửi lên
hoặc department active. Caller có thể tạo expense cho user/department khác.

**Khuyến nghị:**

- Dùng authenticated identity từ JWT.
- Kiểm tra requester và department consistency.
- Validate amount, category, reason ở boundary.
- Trả lỗi có chủ đích thay vì để foreign-key/database exception thành 500.

## Required Changes

### R1. Draft không nên có `CurrentApproverRole`

**File:** [`src/Application/Expenses/CreateExpenseCommandHandler.cs`](../../src/Application/Expenses/CreateExpenseCommandHandler.cs)

Draft hiện được tạo với `CurrentApproverRole = "TeamLead"`. Theo workflow spec,
role phải được tính tại thời điểm Submit.

**Khuyến nghị:** Gán `CurrentApproverRole = null` khi tạo Draft.

### R2. State machine chưa được mô hình hóa

**File:** [`src/Application/Expenses/SubmitExpenseCommandHandler.cs`](../../src/Application/Expenses/SubmitExpenseCommandHandler.cs)

Handler chuyển trực tiếp `Draft -> PendingApproval`, trong khi spec xác định
`Submitted` là checkpoint của state machine. Status cũng đang bị mutate trực
tiếp bằng string trong handlers.

**Khuyến nghị:**

- Tạo domain transition API hoặc state machine.
- Thực hiện và kiểm soát `Draft -> Submitted -> PendingApproval`.
- Chặn các transition không hợp lệ như `Approved -> Rejected`, `Paid -> Approved`
  và `Rejected -> Paid`.

### R3. Hard-coded status, role, action và category

**Files:** `src/Application/Expenses/*.cs`

Các giá trị như `Draft`, `PendingApproval`, `TeamLead`, `Manager`, `Accountant`,
`Admin`, `Submit`, `Approve`, `Reject`, `MarkPaid` và category đang rải rác
trong handlers.

**Khuyến nghị:** Tập trung thành domain constants, enum hoặc value objects;
mapping database vẫn có thể giữ string.

### R4. Create-time validation chưa đầy đủ

**File:** [`src/Application/Expenses/CreateExpenseCommandHandler.cs`](../../src/Application/Expenses/CreateExpenseCommandHandler.cs)

Chưa kiểm tra đầy đủ:

- Requester tồn tại và active.
- Department tồn tại và active.
- Requester thuộc department.
- Amount > 0.
- Amount phù hợp precision.
- Category hợp lệ.
- Reason không rỗng.

**Khuyến nghị:** Thêm FluentValidation validator và application-level
validation cho các invariant quan trọng.

### R5. RowVersion chưa có read/API contract hoàn chỉnh

**File:** [`src/Api/Controllers/ExpensesController.cs`](../../src/Api/Controllers/ExpensesController.cs)

Các response hiện chỉ trả ID. Không có GET endpoint hoặc response DTO chứa
`RowVersion`, nên client thực tế không có cách lấy version mới nhất để gửi cho
Approve/Reject/Pay.

**Khuyến nghị:**

- Thêm `GET /api/expenses/{id}` trả DTO có `RowVersion` dạng Base64.
- Hoặc trả representation đầy đủ sau mỗi state transition.
- Không dùng raw `byte[]` không có quy ước encoding ở public API.

### R6. Error mapping chưa nhất quán

**File:** [`src/Api/Controllers/ExpensesController.cs`](../../src/Api/Controllers/ExpensesController.cs)

Controller lặp nhiều khối `try/catch`; Create chưa có mapping lỗi expected,
nên validation hoặc persistence exception có thể thành 500.

**Khuyến nghị:** Dùng global exception handler để chuẩn hóa:

- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

### R7. Append-only audit chưa được enforce

**File:** [`src/Infrastructure/Data/AppDbContext.cs`](../../src/Infrastructure/Data/AppDbContext.cs)

`ApprovalHistory` có public `DbSet` và chưa có guard ngăn update/delete.

**Khuyến nghị:**

- Reject entry `Modified` hoặc `Deleted` của `ApprovalHistory` trong
  persistence layer.
- Cân nhắc database permissions/triggers nếu cần bảo vệ mạnh hơn.

### R8. Startup dùng `EnsureCreated()` và synchronous I/O

**File:** [`src/Api/Program.cs`](../../src/Api/Program.cs)

Repository đã có migration nhưng startup dùng `EnsureCreated()`, `Any()` và
`SaveChanges()`. Điều này không phù hợp cho schema evolution và vi phạm quy tắc
I/O async.

**Khuyến nghị:**

- Dùng migration trong deployment process hoặc `MigrateAsync`.
- Dùng `AnyAsync` và `SaveChangesAsync`.
- Tách development seed khỏi production startup.

### R9. Logic workflow bị lặp

**Files:**

- [`src/Application/Expenses/ApproveExpenseCommandHandler.cs`](../../src/Application/Expenses/ApproveExpenseCommandHandler.cs)
- [`src/Application/Expenses/RejectExpenseCommandHandler.cs`](../../src/Application/Expenses/RejectExpenseCommandHandler.cs)
- [`src/Application/Expenses/MarkExpensePaidCommandHandler.cs`](../../src/Application/Expenses/MarkExpensePaidCommandHandler.cs)

Approve/Reject lặp validation approver và mỗi handler tự mutate status.

**Khuyến nghị:** Tạo domain workflow/state-transition service và policy dùng
chung; handler chỉ orchestration.

### R10. Chưa có automated tests

Không tìm thấy test project hoặc test file.

**Khuyến nghị:** Thêm unit, infrastructure và API tests cho:

- Transition hợp lệ và không hợp lệ.
- Boundary `10.000.000` và `10.000.000,01`.
- Sai role, khác department, tự approve.
- Reject thiếu comment.
- Mark Paid bởi role không hợp lệ.
- RowVersion conflict.
- Audit history.
- Transaction rollback.
- HTTP status `400/403/404/409`.

## Optional Improvements

- Thêm endpoint xem chi tiết expense và approval history.
- Thêm idempotency/rate limiting cho command state-changing.
- Thêm structured logging và correlation ID cho workflow/concurrency conflict.
- Tạo deployment pipeline áp dụng migrations thay vì API tự thay đổi schema.

## Nits

- Nên gom các DTO request của controller vào thư mục Contracts riêng thay vì
  nested record nếu số lượng endpoint tiếp tục tăng.
- Nên thống nhất tên `MarkPaid`/`MarkExpensePaid` trong API/Application để giảm
  khác biệt ngữ nghĩa.
- Nên trả error body theo một schema thống nhất thay vì anonymous object chỉ ở
  một số endpoint.

## What's Done Well

- Controller không gọi trực tiếp `AppDbContext`.
- MediatR được đăng ký và command/handler nằm ở Application.
- Các action Submit/Approve/Reject/MarkPaid đều tạo `ApprovalHistory`.
- Các handler dùng `CancellationToken`.
- Entity được query bằng `AsNoTracking()` trước khi clone record.
- Đã có optimistic concurrency với `RowVersion`.
- Sai trạng thái được ánh xạ thành `409`.
- Mark Paid dùng `StatusCode(403)` theo yêu cầu.
- Hướng phụ thuộc Domain -> Application -> Infrastructure được giữ đúng.

## Verification Story

- **Build:** Đạt — solution build thành công với 0 warning và 0 error.
- **Tests:** Chưa đạt — chưa có test project/test file.
- **Security:** Chưa đạt — thiếu JWT/authorization, actor ID do client kiểm
  soát, credentials plaintext được seed.
- **Architecture:** Đạt một phần — boundary cơ bản đúng, nhưng state machine và
  policy chưa được tập trung ở Domain/Application.
- **Performance:** Chưa có N+1 rõ ràng vì chưa có list endpoint; startup vẫn
  dùng synchronous database I/O.

## Suggested Prioritized Roadmap

1. Loại bỏ plaintext credentials và tách development seed.
2. Sửa Create validation và để Draft có `CurrentApproverRole = null`.
3. Hoàn thiện state machine/domain transition rules.
4. Hoàn thiện RowVersion read contract.
5. Thêm global exception handling.
6. Enforce append-only ApprovalHistory.
7. Thêm automated tests cho Domain/Application/Infrastructure/API.
8. Chuyển startup sang migrations và async database I/O.
