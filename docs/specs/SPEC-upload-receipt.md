# Spec: Upload Receipt

## Objective

Allow an authenticated expense creator to upload a receipt for an
`ExpenseRequest` and associate the stored file URL with that request.

The feature must follow Clean Architecture:

- API handles multipart HTTP concerns only.
- Application validates the command and coordinates the use case.
- Infrastructure owns the local file-system implementation.
- Domain remains independent of ASP.NET Core, file-system APIs, and
  Infrastructure.

## Assumptions

1. The MVP stores receipt files on the local API host.
2. Receipt files are private and are served only through an authorized API
   endpoint; public static-file serving is not used for receipts.
3. A receipt may be uploaded only while the expense is `Draft` or `Rejected`.
4. Uploading a replacement receipt is allowed in those states; the previous
   file is not deleted by this MVP.
5. The authenticated user must satisfy the existing `ExpenseCreator` policy
   and the Application layer must authorize the actor for the target expense:
   the requester or an active elevated user in the same department
   (`TeamLead`, `Manager`, `Accountant`, or `Admin`).
6. The database already contains the nullable `ExpenseRequest.ReceiptUrl`
   column.

## Tech Stack

- C# / .NET 9
- ASP.NET Core Web API
- MediatR
- Entity Framework Core with PostgreSQL
- Clean Architecture
- Local file-system storage under `wwwroot/uploads/receipts`

## API Contract

### Upload receipt

```http
POST /api/expenses/{id}/receipt
Authorization: Bearer <access-token>
Content-Type: multipart/form-data
```

Multipart field:

```text
file: receipt.jpg | receipt.png | receipt.pdf
```

Successful response:

- HTTP `200 OK`
- `ApiResponse<string>`
- `Data` contains a relative URL such as
  `/uploads/receipts/0f8fad5b-d9cb-469f-a165-70867728950e.pdf`

### Download receipt

```http
GET /api/expenses/{id}/receipt
Authorization: Bearer <access-token>
```

The endpoint returns the receipt stream only after the same Application
authorization check. Receipt files are never exposed by a public static-file
route.

Expected errors:

| Condition | Status |
|---|---:|
| Missing, empty, unsupported, or oversized file | `400 Bad Request` |
| Expense request does not exist | `404 Not Found` |
| Expense status is not `Draft` or `Rejected` | `409 Conflict` |
| Missing/invalid authentication or policy failure | `401/403` |
| Stale `RowVersion` or concurrent update | `409 Conflict` |

## Validation Rules

- File is required and must not be empty.
- Maximum file size is `5 MB` (`5 * 1024 * 1024` bytes).
- Accepted content types are exactly:
  - `image/jpeg`
  - `image/png`
  - `application/pdf`
- Declared content type must match the file signature:
  - JPEG begins with `FF D8 FF`
  - PNG begins with `89 50 4E 47 0D 0A 1A 0A`
  - PDF begins with `%PDF-`
- The expense request must exist.
- The expense status must be `Draft` or `Rejected`.
- The stored file name must never use the client-provided file name.
- The persisted name must be generated using a cryptographically random GUID
  and the extension must be selected from the validated content type.

## Application Design

### Abstraction

`IFileStorageService` lives in `src/Application/Abstractions/`:

```csharp
Task<string> UploadAsync(
    Stream stream,
    string fileName,
    string contentType,
    CancellationToken cancellationToken);

Task DeleteAsync(string receiptUrl, CancellationToken cancellationToken);

Task<StoredFile> OpenReadAsync(
    string receiptUrl,
    CancellationToken cancellationToken);
```

### Command

`UploadReceiptCommand` lives in
`src/Application/Expenses/UploadReceiptCommand.cs`:

The command must contain only Application-friendly inputs:
`ExpenseRequestId`, `ActorId`, `Stream`, `FileName`, `ContentType`, and `Length`.
`IFormFile` is bound in the API layer and must not cross into Application.

The handler must:

1. Validate file presence, size, declared content type, and magic bytes.
2. Load the expense request with `AsNoTracking()`.
3. Validate actor authorization in Application.
4. Reject missing requests with `404` mapping.
5. Reject invalid statuses with `409` mapping.
6. Upload through `IFileStorageService`.
7. Update `ReceiptUrl`, `UpdatedAt`, and `RowVersion`.
8. Persist through `IApplicationDbContext.SaveChangesAsync`.
9. Delete the newly uploaded file if persistence fails.
10. Propagate the request `CancellationToken` to all asynchronous operations.

## Infrastructure Design

`LocalFileStorageService` lives in
`src/Infrastructure/Storage/LocalFileStorageService.cs`.

- Root directory: `{WebRootPath}/uploads/receipts`
- Fallback root when `WebRootPath` is unavailable:
  `{ContentRootPath}/wwwroot/uploads/receipts`
- Create the directory when needed.
- Use `FileMode.CreateNew`.
- Use asynchronous stream copying.
- Return only a relative URL beginning with `/uploads/receipts/`.
- Never expose an absolute server path or trust a path from the client.

Register the service with dependency injection:

```csharp
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
```

Do not call `UseStaticFiles()` for receipts. The authorized download endpoint
must use `OpenReadAsync`.

## Project Structure

- `src/Application/Abstractions/IFileStorageService.cs`
  — storage boundary
- `src/Application/Expenses/UploadReceiptCommand.cs`
  — command and handler
- `src/Infrastructure/Storage/LocalFileStorageService.cs`
  — local file-system implementation
- `src/Api/Controllers/ExpensesController.cs`
  — multipart endpoint
- `src/Api/Program.cs`
  — DI registration; no public receipt static-file middleware
- `tests/ExpenseApproval.Application.Tests/`
  — handler and storage-boundary tests
- `tests/ExpenseApproval.Api.Tests/`
  — endpoint contract/status tests

## Code Style

- Use records for commands and API response DTOs.
- Keep controller methods limited to binding, dispatching, and HTTP response
  mapping.
- Use `CancellationToken` for every asynchronous I/O operation.
- Use explicit exception types already mapped by the API:
  `ArgumentException`, `KeyNotFoundException`, `InvalidOperationException`,
  and authorization exceptions.
- Do not add EF Core or file-system dependencies to Domain.

## Testing Strategy

Unit/application tests must cover:

- Accepted JPEG, PNG, and PDF content types.
- Empty file rejection.
- File size exactly at 5 MB and above 5 MB.
- Unsupported content type rejection.
- Missing expense rejection.
- Draft and Rejected status acceptance.
- PendingApproval, Approved, and Paid status rejection.
- `ReceiptUrl` update and persistence invocation.
- Cancellation token propagation.

Infrastructure tests must cover:

- GUID-based generated file names.
- Correct extension selected from content type.
- Relative URL format.
- Files written beneath `wwwroot/uploads/receipts`.
- Client file names cannot escape the upload directory.

API tests must cover:

- `POST /api/expenses/{id}/receipt`.
- Successful `200 OK` response containing the relative URL.
- `400`, `404`, and `409` mappings.
- Authorization policy metadata remains applied.

Verification commands:

```text
dotnet restore .\ExpenseApproval.slnx
dotnet build .\ExpenseApproval.slnx --no-restore
dotnet test .\ExpenseApproval.slnx --no-restore
```

## Boundaries

- Always: validate size, declared content type, and magic bytes before writing;
  generate server-side file names; use async I/O; preserve Clean Architecture
  boundaries; use cancellation tokens; clean up files when persistence fails;
  test invalid status and file inputs.
- Ask first: changing the database schema, adding cloud/object storage,
  changing retention/deletion behavior, or changing the 5 MB/type limits.
- Never: trust the original file name as a path, allow arbitrary content types,
  store files outside the configured upload directory, commit uploaded files,
  expose absolute server paths, or add storage dependencies to Domain.

## Success Criteria

1. `POST /api/expenses/{id}/receipt` accepts a valid multipart receipt from an
   authorized user.
2. JPEG, PNG, and PDF files no larger than 5 MB are accepted.
3. Invalid files receive `400 Bad Request` without being written.
4. Only `Draft` and `Rejected` expense requests can receive a receipt.
5. Missing requests return `404`; invalid workflow states return `409`.
6. Files are stored below `wwwroot/uploads/receipts` using GUID-based names.
7. `ExpenseRequest.ReceiptUrl` is persisted and returned as a relative URL.
8. Receipt download is only available through the authorized API endpoint.
9. Application, Infrastructure, and API tests pass.

## Open Questions

- Production deployment may require replacing local storage with object storage
  and defining file retention/deletion rules. That is outside this MVP.
