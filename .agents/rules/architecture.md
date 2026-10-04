# C# .NET Backend Architecture Rules

## 1. Core Stack
- Framework: ASP.NET Core 9 (Web API).
- ORM: Entity Framework Core 9 (PostgreSQL provider).
- Message Broker: RabbitMQ (sử dụng MassTransit).

## 2. Design Patterns
- Áp dụng Clean Architecture (Domain, Application, Infrastructure, API).
- KHÔNG gọi trực tiếp `DbContext` từ Controller. Mọi nghiệp vụ đi qua Service hoặc CQRS (MediatR).
- Quản lý vòng đời chi phí bằng State Machine (thư viện `Stateless`). KHÔNG dùng if/else/switch để chuyển đổi `Status`.

## 3. Coding Standards
- BẮT BUỘC sử dụng `record` cho DTOs, Commands, Queries.
- 100% các hàm I/O phải dùng `async/await` và truyền `CancellationToken`.
- Trả về thống nhất: Bọc mọi HTTP Response trong một wrapper (ví dụ: `ApiResponse<T>`).