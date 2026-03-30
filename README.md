# BarberBooking - Hướng dẫn cài đặt

## Yêu cầu
- .NET 8 SDK
- Visual Studio 2022 hoặc VS Code

## Các bước chạy

### 1. Restore packages
```
dotnet restore
```

### 2. Tạo migration & database
```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Cấu hình Email (appsettings.json)
Điền thông tin SMTP của bạn vào `appsettings.json`:
- Với Gmail: bật "App Password" trong Google Account
- Host: smtp.gmail.com, Port: 587

### 4. Chạy project
```
dotnet run
```
Hoặc nhấn F5 trong Visual Studio.

## Tài khoản Admin mặc định
- Email: `admin@barber.com`
- Password: `Admin@123`

> ⚠️ Đổi mật khẩu sau khi đăng nhập lần đầu!

## Cấu trúc project
```
BarberBooking/
├── Controllers/       # API Endpoints
├── Data/              # AppDbContext (EF Core)
├── Models/            # Entity models
├── Pages/
│   ├── Admin/         # Quản trị (Login, Bookings, Services)
│   ├── Booking/       # Đặt lịch, Huỷ lịch
│   └── Shared/        # Layout
├── Services/          # EmailService, ExportService
└── wwwroot/           # Static files (CSS, JS)
```

## API Endpoints
| Method | URL | Mô tả |
|--------|-----|-------|
| GET | /api/services | Danh sách dịch vụ |
| POST | /api/bookings | Đặt lịch |
| GET | /api/bookings/user?email= | Lịch của user |
| DELETE | /api/bookings/{id}?token= | Huỷ lịch |
| GET | /api/admin/bookings | Toàn bộ lịch (Admin) |
| GET | /api/admin/bookings/export?format=excel/csv/pdf | Xuất file |
