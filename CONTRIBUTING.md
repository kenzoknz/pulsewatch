# Contributing to PulseWatch

Thank you for contributing to PulseWatch. This guide covers local setup,
development standards, validation, commits, and pull requests.

## Project Overview

- `PulseWatch.Api`: ASP.NET Core 10 Web API with EF Core, SQL Server, Identity,
  JWT, SignalR, and Playwright.
- `PulseWatch.Client`: React 19 application built with Vite, Axios, SignalR,
  React Router, and plain CSS.

## Getting Started

### Prerequisites

- .NET SDK 10.0 or later
- Node.js 18.0 or later
- SQL Server or SQL Server Express
- Git

### Fork, clone, and branch

1. Fork the repository on GitHub.
2. Clone your fork:

   ```bash
   git clone https://github.com/<your-username>/pulsewatch.git
   cd pulsewatch
   ```

3. Create a feature branch from an up-to-date `main` branch:

   ```bash
   git checkout main
   git pull origin main
   git checkout -b feat/short-feature-name
   ```

### Configure and run the backend

Set a local SQL Server connection string and development JWT key in
`PulseWatch.Api/appsettings.Development.json`. Never commit real credentials,
SMTP passwords, tokens, or other secrets.

From `PulseWatch.Api`, apply migrations and start the API:

```bash
dotnet ef database update
dotnet run
```

### Configure and run the frontend

From `PulseWatch.Client`, install dependencies and start Vite:

```bash
npm install
npm run dev
```

Create a local `.env` file when needed:

```text
VITE_API_BASE_URL=http://localhost:5175/api
```

The default local URLs are `http://localhost:5175` for the API and
`http://localhost:5173` for the client. On Windows, `start.bat` can build and
start both applications.

## Development Standards

- Use English for API contracts, backend messages, frontend text, and new code
  documentation.
- Follow the existing project structure and technologies.
- C# uses PascalCase for types, methods, and public properties, and camelCase
  for local variables and parameters.
- React components use PascalCase; JavaScript variables and functions use
  camelCase.
- Use four spaces for indentation and never use tabs.
- Use clear names. Since this project uses C# and JavaScript/JSX, `snake_case` is
  not required for identifiers; follow the conventions of each language.
- Add concise XML documentation or comments for new public APIs and non-obvious
  business logic. Do not add comments that merely restate the code.
- Keep the existing plain CSS approach. Do not introduce Tailwind CSS without
  maintainer approval.
- Keep changes focused and avoid unrelated formatting, dependency updates, or
  generated build output.

## Database Migrations

When changing EF Core models or the database schema, run these commands from
`PulseWatch.Api`:

```bash
dotnet ef migrations add DescribeTheChange
dotnet ef database update
```

Include generated migration files in the pull request. Verify that the
application starts against a clean or updated development database.

## Validation Before Committing

Run the checks relevant to your changes:

```bash
cd PulseWatch.Api
dotnet build
```

```bash
cd PulseWatch.Client
npm run lint
npm run build
```

Also manually verify affected API endpoints, authentication, SignalR
notifications, background monitoring, or user flows. If a check cannot be run,
explain why in the pull request.

## Commit Messages

Use Conventional Commits with a short, imperative subject:

```text
feat(api): add website availability endpoint
fix(client): handle expired access tokens
docs: update local setup instructions
refactor(monitoring): simplify check scheduling
```

Common types include `feat`, `fix`, `docs`, `refactor`, `test`, `build`, and
`chore`.

## Pull Requests

1. Update your branch from `main` and run the applicable validation commands.
2. Push the branch to your fork:

   ```bash
   git push -u origin feat/short-feature-name
   ```

3. Open a pull request from your fork to the original repository's `main`
   branch.
4. Describe the problem, solution, validation performed, database/configuration
   changes, and known limitations.
5. Include screenshots or request/response examples for UI and API changes when
   useful.
6. Keep the branch focused on one change and respond to review feedback.

### Pull Request Checklist

- [ ] The change is scoped to the stated problem.
- [ ] Relevant backend and/or frontend checks pass.
- [ ] API behavior is documented where appropriate.
- [ ] Database migrations are included when the schema changes.
- [ ] No secrets, local connection strings, or generated build files are committed.
- [ ] UI and API-facing text is in English.
- [ ] Commits use Conventional Commits.

## Bug Reports and Feature Requests

Search existing issues and documentation first. A useful bug report includes
expected and actual behavior, reproduction steps, environment details, relevant
logs, and screenshots when applicable. Feature requests should explain the user
problem and desired outcome.

## Code of Conduct

Please be respectful, constructive, and collaborative. Discuss technical
decisions on their merits and do not use personal attacks or discrimination.

---

# Hướng dẫn đóng góp cho PulseWatch

Cảm ơn bạn đã quan tâm và muốn đóng góp cho PulseWatch. Tài liệu này hướng dẫn
cách thiết lập môi trường, phát triển tính năng, kiểm tra thay đổi và mở pull
request cho dự án.

## Tổng quan dự án

- `PulseWatch.Api`: ASP.NET Core 10 Web API với EF Core, SQL Server, Identity,
  JWT, SignalR và Playwright.
- `PulseWatch.Client`: ứng dụng React 19 dùng Vite, Axios, SignalR, React Router
  và CSS thuần.

## Bắt đầu phát triển

### Yêu cầu hệ thống

- .NET SDK 10.0 trở lên
- Node.js 18.0 trở lên
- SQL Server hoặc SQL Server Express
- Git

### Fork, clone và tạo branch

1. Fork repository trên GitHub.
2. Clone fork của bạn:

   ```bash
   git clone https://github.com/<your-username>/pulsewatch.git
   cd pulsewatch
   ```

3. Tạo branch tính năng từ `main` mới nhất:

   ```bash
   git checkout main
   git pull origin main
   git checkout -b feat/ten-tinh-nang
   ```

### Cấu hình và chạy backend

Đặt connection string SQL Server local và JWT key dành cho development trong
`PulseWatch.Api/appsettings.Development.json`. Tuyệt đối không commit mật khẩu,
SMTP credentials, token hoặc secret thật.

Tại thư mục `PulseWatch.Api`, cập nhật database và chạy API:

```bash
dotnet ef database update
dotnet run
```

### Cấu hình và chạy frontend

Tại thư mục `PulseWatch.Client`, cài dependencies và chạy Vite:

```bash
npm install
npm run dev
```

Tạo file `.env` local khi cần:

```text
VITE_API_BASE_URL=http://localhost:5175/api
```

Mặc định API chạy tại `http://localhost:5175`, frontend chạy tại
`http://localhost:5173`. Trên Windows, có thể dùng `start.bat` để build và chạy
cả hai ứng dụng.

## Quy chuẩn phát triển

- Dùng tiếng Anh cho API contract, thông báo backend, nội dung frontend và tài
  liệu code mới.
- Tuân thủ cấu trúc và công nghệ hiện có của dự án.
- C# dùng PascalCase cho type, method và public property; dùng camelCase cho
  biến local và parameter.
- React component dùng PascalCase; biến và function JavaScript dùng camelCase.
- Thụt lề bằng bốn khoảng trắng, tuyệt đối không dùng phím Tab.
- Vì dự án dùng C# và JavaScript/JSX, không bắt buộc dùng `snake_case` cho
  identifier; hãy theo quy ước của từng ngôn ngữ.
- Thêm XML documentation hoặc comment ngắn cho public API mới và logic nghiệp
  vụ khó hiểu. Không thêm comment chỉ lặp lại nội dung code.
- Tiếp tục dùng CSS thuần theo cấu trúc hiện tại. Không thêm Tailwind CSS nếu
  chưa được maintainer chấp thuận.
- Giữ thay đổi tập trung; không kèm formatting, nâng dependency hoặc build
  output không liên quan.

## Database migration

Khi thay đổi EF Core model hoặc schema, chạy tại `PulseWatch.Api`:

```bash
dotnet ef migrations add MoTaThayDoi
dotnet ef database update
```

Đưa các file migration được tạo vào pull request. Xác nhận ứng dụng khởi động
được với database development sạch hoặc đã cập nhật.

## Kiểm tra trước khi commit

Chạy các lệnh phù hợp với thay đổi:

```bash
cd PulseWatch.Api
dotnet build
```

```bash
cd PulseWatch.Client
npm run lint
npm run build
```

Ngoài ra, kiểm tra thủ công các API endpoint, đăng nhập, thông báo SignalR,
background monitoring hoặc user flow bị ảnh hưởng. Nếu không thể chạy kiểm tra,
hãy ghi rõ lý do trong pull request.

## Commit message

Sử dụng Conventional Commits với subject ngắn, ở dạng mệnh lệnh:

```text
feat(api): add website availability endpoint
fix(client): handle expired access tokens
docs: update local setup instructions
refactor(monitoring): simplify check scheduling
```

Các type thường dùng gồm `feat`, `fix`, `docs`, `refactor`, `test`, `build` và
`chore`.

## Pull request

1. Cập nhật branch từ `main` và chạy các lệnh kiểm tra phù hợp.
2. Push branch lên fork:

   ```bash
   git push -u origin feat/ten-tinh-nang
   ```

3. Mở pull request từ fork về branch `main` của repository gốc.
4. Mô tả vấn đề, giải pháp, các lệnh đã kiểm tra, thay đổi database/cấu hình và
   giới hạn còn biết.
5. Với thay đổi UI hoặc API, bổ sung screenshot hoặc ví dụ request/response khi
   hữu ích.
6. Giữ branch tập trung vào một thay đổi và phản hồi review.

### Checklist pull request

- [ ] Thay đổi chỉ tập trung vào vấn đề đã nêu.
- [ ] Các kiểm tra backend và/hoặc frontend phù hợp đã thành công.
- [ ] API mới hoặc API thay đổi đã được mô tả khi cần.
- [ ] Đã thêm migration nếu schema thay đổi.
- [ ] Không commit secret, connection string local hoặc build output.
- [ ] Nội dung UI và nội dung hướng tới API đều dùng tiếng Anh.
- [ ] Commit tuân thủ Conventional Commits.

## Báo lỗi và đề xuất tính năng

Hãy tìm issue và tài liệu hiện có trước khi tạo issue mới. Bug report nên gồm
hành vi mong đợi, hành vi thực tế, bước tái hiện, thông tin môi trường, log liên
quan và screenshot nếu có. Feature request nên giải thích vấn đề của người dùng
và kết quả mong muốn.

## Quy tắc ứng xử

Hãy trao đổi tôn trọng, mang tính xây dựng và hợp tác. Thảo luận kỹ thuật trên
cơ sở chuyên môn, không công kích cá nhân hoặc phân biệt đối xử.