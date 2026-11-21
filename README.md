# Phasolpro DMP

Web tra cứu thành phần trong công thức dược mỹ phẩm: đối chiếu tên thành phần với các danh mục EU (CosIng) và
ASEAN (ACD) để biết chất bị cấm, bị giới hạn hay được phép dùng.

## Chức năng

- **Danh mục**: xem, tìm trong 3 danh mục tổng hợp theo EU (Cấm sử dụng, Giới hạn, Cho phép), 5 Annex EU (II đến VI)
  và 5 Annex ASEAN (II, III, IV, VI, VII).
- **Tra cứu**: nhập, dán hoặc tải tệp (.docx, .xlsx, .xls, .csv) danh sách thành phần, tra theo hai chế độ
  Chính xác và Tương đối, xem chi tiết từng dòng Annex.
- **Tài khoản**: đăng ký (Cá nhân, Doanh nghiệp), đăng nhập, đổi mật khẩu, tự sửa thông tin.
- **Quản trị** (vai trò Quản trị): thêm, sửa, khóa, xóa tài khoản, đặt lại mật khẩu.

Mọi trang trừ trang chủ, Đăng nhập, Đăng ký đều phải đăng nhập.

## Công nghệ

- ASP.NET MVC 5, .NET Framework 4.5, viết bằng Visual Studio 2013.
- Truy cập dữ liệu bằng ADO.NET (không dùng Entity Framework).
- Đăng nhập bằng cookie OWIN; mật khẩu băm PBKDF2, không lưu mật khẩu gốc.
- Giao diện: Razor, jQuery, CSS tự viết.

## Nhánh

| Nhánh | Cơ sở dữ liệu | Chuỗi kết nối dùng |
|---|---|---|
| `main` | SQL Server | `TraCuuDb` |
| `mysql` | MySQL 8.4 (thư viện MySql.Data 6.9.12), chỉ còn để lưu, không phát triển tiếp | `TraCuuMySql` |

## Chạy trên máy

1. Mở `PhasolproDMP.sln` bằng Visual Studio 2013 (NuGet tự tải các gói khi build).
2. Trong thư mục `PM_TraCuuThongTinMyPham/Config`:
   - chép `ConnectionStrings.config.example` thành `ConnectionStrings.config`, điền máy chủ, tài khoản, mật khẩu
     database;
   - (không bắt buộc) chép `AppSettings.secret.config.example` thành `AppSettings.secret.config`, điền 2 khóa
     Google reCAPTCHA v2. Không có tệp này thì trang Đăng ký không hiện ô reCAPTCHA.
3. Build rồi bấm F5.

Hai tệp cấu hình ở bước 2 chứa mật khẩu và khóa bí mật nên không đưa lên git (xem `.gitignore`).
Script tạo database và dữ liệu danh mục không nằm trong repo này.

## Cấu trúc thư mục

| Thư mục (trong `PM_TraCuuThongTinMyPham`) | Nội dung |
|---|---|
| `Controllers` | Trang chủ, Danh mục, Tra cứu, Tài khoản, Quản trị; `PhanQuyenAttribute` kiểm tra đăng nhập và vai trò |
| `Data` | Đọc, ghi database (các `...Repository`), băm mật khẩu, kiểm tra dữ liệu nhập, so khớp tên, bộ nhớ đệm danh mục |
| `Models` | Các lớp dữ liệu cho view |
| `Views` | Giao diện Razor |
| `Scripts` | `danhmuc.js` (trang Danh mục), `tracuu.js` (trang Tra cứu), `taikhoan.js` (form tài khoản) |
| `Content` | `Site.css` |
