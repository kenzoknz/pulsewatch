# PulseWatch

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10.0-blueviolet?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
[![React](https://img.shields.io/badge/React-19.2-61dafb?style=flat-square&logo=react)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-8.0-646CFF?style=flat-square&logo=vite&logoColor=white)](https://vite.dev/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-Express-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)

PulseWatch là công cụ giám sát uptime website, tự động kiểm tra tính khả dụng và thời gian phản hồi của các website theo chu kỳ. Hệ thống phát hiện sự cố ngừng hoạt động, lưu lịch sử kiểm tra và hiển thị tổng quan trực quan qua dashboard.

---

## Mục lục

- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt](#cài-đặt)
  - [1. Clone dự án](#1-clone-dự-án)
  - [2. Cài đặt Backend](#2-cài-đặt-backend)
  - [3. Cài đặt Frontend](#3-cài-đặt-frontend)
- [Chạy ứng dụng](#chạy-ứng-dụng)
- [Hướng dẫn sử dụng](#hướng-dẫn-sử-dụng)
- [Cấu hình nâng cao](#cấu-hình-nâng-cao)

---

## Yêu cầu hệ thống

Trước khi cài đặt, hãy đảm bảo máy tính đã có:

| Phần mềm | Phiên bản tối thiểu | Tải về |
|---|---|---|
| .NET SDK | 10.0 | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Node.js | 18.0 | [nodejs.org](https://nodejs.org/) |
| SQL Server | bất kỳ (kể cả Express) | [microsoft.com/sql-server](https://www.microsoft.com/sql-server/sql-server-downloads) |

---

## Cài đặt

### 1. Clone dự án

```bash
git clone https://github.com/your-username/pulsewatch.git
cd pulsewatch
```

### 2. Cài đặt Backend

**Bước 1 — Cấu hình chuỗi kết nối database**

Mở file `PulseWatch.Api/appsettings.json` và cập nhật `DefaultConnection` trỏ vào SQL Server instance của bạn:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TEN_SERVER;Database=PulseWatchDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Thay `TEN_SERVER` bằng tên SQL Server instance thực tế (ví dụ: `localhost`, `.\SQLEXPRESS`, `PC\SQLEXPRESS`).

**Bước 2 — Tạo database**

```bash
cd PulseWatch.Api
dotnet ef database update
```

Lệnh này sẽ tự động tạo database `PulseWatchDb` và toàn bộ bảng dữ liệu cần thiết.

### 3. Cài đặt Frontend

**Bước 1 — Tạo file cấu hình môi trường**

```bash
cd PulseWatch.Client
copy .env.example .env
```

**Bước 2 — Kiểm tra URL API**

Mở file `.env` vừa tạo, đảm bảo URL trỏ đúng vào port backend:

```
VITE_API_BASE_URL=http://localhost:5175/api
```

**Bước 3 — Cài đặt dependencies**

```bash
npm install
```

---

## Chạy ứng dụng

Cần mở **2 terminal** chạy đồng thời.

**Terminal 1 — Khởi động Backend:**

```bash
cd PulseWatch.Api
dotnet run
```

Backend sẽ chạy tại `http://localhost:5175`. Background service giám sát tự động khởi động cùng.

**Terminal 2 — Khởi động Frontend:**

```bash
cd PulseWatch.Client
npm run dev
```

Frontend sẽ chạy tại `http://localhost:5173`.

Mở trình duyệt và truy cập `http://localhost:5173` để sử dụng ứng dụng.

---

## Hướng dẫn sử dụng

### Thêm website cần giám sát

1. Vào mục **Websites > Manage Websites** trên thanh điều hướng bên trái.
2. Nhấn nút **Add Website**, nhập tên và URL của website.
3. Chọn chu kỳ kiểm tra mong muốn (mặc định: 5 phút).
4. Nhấn **Save** — hệ thống sẽ bắt đầu giám sát ngay lập tức.

### Thêm nhiều website cùng lúc

1. Vào mục **Websites > Bulk Add** trên thanh điều hướng.
2. Dán danh sách URL (mỗi URL một dòng) vào ô nhập liệu.
3. Nhấn **Add** — hệ thống tự động lấy tên từ hostname của từng URL.

### Xem tổng quan hệ thống

Vào mục **Dashboard** để xem:
- Tổng số website đang giám sát
- Số website đang online / offline
- Thời gian phản hồi trung bình
- Tổng số sự cố ngừng hoạt động đã ghi nhận

### Xem chi tiết một website

1. Vào mục **Websites > Manage Websites**.
2. Nhấn vào tên website bất kỳ.
3. Trang chi tiết hiển thị:
   - Phần trăm uptime tổng thể
   - Biểu đồ thời gian phản hồi
   - Lịch sử các lần kiểm tra (có phân trang)
   - Danh sách các sự kiện ngừng hoạt động đã ghi nhận

### Kiểm tra thủ công

Trên trang danh sách website, nhấn nút **Check Now** trên bất kỳ website nào để chạy kiểm tra ngay lập tức mà không cần chờ chu kỳ tự động.

### Chuyển đổi giao diện sáng/tối

Nhấn biểu tượng mặt trời/mặt trăng ở góc trên bên phải để chuyển đổi giữa dark mode và light mode.

---

## Cấu hình nâng cao

Chỉnh sửa phần `UptimeMonitoring` trong `PulseWatch.Api/appsettings.json` để điều chỉnh hành vi giám sát:

```json
"UptimeMonitoring": {
  "SchedulerDelaySeconds": 60,
  "HttpTimeoutSeconds": 10,
  "RetentionDays": 90,
  "MaxRetries": 3
}
```

| Khóa | Mặc định | Mô tả |
|---|---|---|
| `SchedulerDelaySeconds` | `60` | Chu kỳ giám sát tính bằng giây |
| `HttpTimeoutSeconds` | `10` | Thời gian chờ tối đa mỗi lần kiểm tra |
| `RetentionDays` | `90` | Số ngày lưu trữ lịch sử kiểm tra |
| `MaxRetries` | `3` | Số lần thử lại khi kiểm tra thất bại |
