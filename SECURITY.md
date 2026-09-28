# Chính sách bảo mật

## Phiên bản được hỗ trợ

Hiện tại, các bản sửa bảo mật được áp dụng cho mã nguồn mới nhất trên branch
`main`. Các branch cũ và bản build local không được đảm bảo sẽ nhận cập nhật bảo
mật.

| Phiên bản | Được hỗ trợ |
| --- | --- |
| `main` | Có |
| Branch và release cũ | Không |

## Báo cáo lỗ hổng

Không báo cáo lỗ hổng bảo mật thông qua GitHub issue, pull request, discussion
hoặc kênh chat công khai.

Hãy sử dụng tính năng báo cáo lỗ hổng riêng tư hoặc Security Advisory của GitHub
nếu repository đã bật tính năng này. Nếu chưa bật, hãy liên hệ riêng với
maintainer thông qua repository owner trước khi công khai thông tin.

Nội dung báo cáo nên gồm:

- Mô tả rõ lỗ hổng và mức độ ảnh hưởng
- Component, endpoint, package hoặc cấu hình bị ảnh hưởng
- Các bước tái hiện hoặc proof of concept tối thiểu
- Version, commit hoặc branch bị ảnh hưởng
- Điều kiện về authentication, quyền hạn hoặc môi trường
- Cách giảm thiểu tác động nếu đã biết

Hãy xóa credential, access token, dữ liệu cá nhân và thông tin nhạy cảm khác
khỏi báo cáo. Nếu secret bị lộ, chỉ nêu loại secret bị lộ và tuyệt đối không gửi
giá trị của secret đó.

Maintainer sẽ cố gắng xác nhận báo cáo hợp lệ, điều tra và thống nhất thời điểm
công bố với người báo cáo. Vui lòng cho maintainer thời gian hợp lý để xác minh
và sửa lỗi trước khi công khai.

## Phạm vi báo cáo

Các báo cáo sau đặc biệt hữu ích:

- Authentication, authorization, JWT validation, Identity hoặc dữ liệu tài khoản
- API endpoint có thể xem hoặc thay đổi website, notification của user khác
- SQL injection, xử lý input không an toàn, SSRF, XSS, CSRF hoặc CORS không an toàn
- Authorization của kết nối SignalR hoặc việc tách biệt dữ liệu notification
- Lộ secret trong cấu hình, log, Docker image hoặc file được sinh tự động
- Dependency có lỗ hổng hoặc bị xâm phạm
- Hành vi Playwright deep check cho phép truy cập trái phép tài nguyên local hoặc
  internal

Các vấn đề sau thường không được xem là lỗ hổng nếu không chứng minh được tác
động bảo mật:

- Lỗi yêu cầu máy development đã bị xâm nhập từ trước
- Đề xuất hardening hoặc feature còn thiếu nhưng không khai thác được
- Self-XSS hoặc social engineering không ảnh hưởng đến user khác
- Dependency cũ nhưng không có lỗ hổng liên quan đã biết
- Từ chối dịch vụ chỉ xảy ra trên instance development local

## Quy tắc bảo mật cho contributor

- Lưu connection string, JWT signing key, SMTP credential, token và API key trong
  cấu hình local hoặc secret store được hỗ trợ.
- Không commit secret vào `appsettings.json`, `appsettings.Development.json`,
  file `.env`, source code, log, migration hoặc screenshot.
- Dùng credential development mạnh, riêng biệt và rotate ngay khi có khả năng bị
  lộ.
- Luôn kiểm tra authorization ở server cho mọi API được bảo vệ; không phụ thuộc
  vào việc frontend chặn route.
- Kiểm tra và giới hạn URL do user cung cấp trước khi uptime check hoặc Playwright
  deep check. Đặc biệt lưu ý localhost, private IP range và cloud metadata
  endpoint.
- Không ghi password, JWT, cookie, SMTP credential hoặc dữ liệu user nhạy cảm vào
  log.
- Review thay đổi dependency và chạy build backend, lint/build frontend trước khi
  mở pull request.

## Cập nhật bảo mật

Bản sửa bảo mật có thể được phát hành với thông tin kỹ thuật giới hạn trong giai
đoạn đầu để giảm nguy cơ bị khai thác. Sau khi người dùng có thời gian hợp lý để
cập nhật, maintainer có thể công bố advisory đầy đủ hơn về mức độ ảnh hưởng,
version bị ảnh hưởng, version đã sửa và biện pháp giảm thiểu.
