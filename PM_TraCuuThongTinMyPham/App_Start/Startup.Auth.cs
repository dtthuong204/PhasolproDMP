using System;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham
{
    public partial class Startup
    {
        //đăng nhập bằng cookie (OWIN): tài khoản đọc từ bảng TaiKhoan bằng ADO.NET, không dùng ASP.NET Identity
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = NguoiDung.LoaiXacThuc,
                CookieName = "Phasolpro.DangNhap",
                //chưa đăng nhập mà mở trang cần đăng nhập thì chuyển tới đây (kèm ?ReturnUrl=trang cũ)
                LoginPath = new PathString("/TaiKhoan/DangNhap"),
                //"Lưu tài khoản": cookie giữ 14 ngày, mỗi lần dùng lại tính lại từ đầu
                ExpireTimeSpan = TimeSpan.FromDays(14),
                SlidingExpiration = true,
                CookieHttpOnly = true
            });
        }
    }
}
