
# Order Processing System

## 1. Giới thiệu
Hệ thống xử lý đơn hàng và thông báo trạng thái phân tán.

## 2. Công nghệ
- C#
- ASP.NET Core
- SQL Server
- RabbitMQ
- SignalR
- Docker

## 3. Các thành phần
- OrderApi: Tiếp nhận và quản lý đơn hàng.
- OrderWorker: Xử lý đơn hàng bất đồng bộ.
- NotificationService: Gửi thông báo trạng thái.
- Shared: Chia sẻ các mô hình dữ liệu.

## 4. Trạng thái đơn hàng
Pending -> Processing -> Completed
                         -> Failed

## 5. Tác giả
Sinh viên thực hiện: [Trần Mạnh Hùng]
