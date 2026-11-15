using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace PM_TraCuuThongTinMyPham.Models
{
    //thông tin người đang đăng nhập, lưu trong cookie dưới dạng các "claim" (cặp tên - giá trị)
    //view và controller đọc qua các hàm ở đây, không đọc database mỗi lần
    public static class NguoiDung
    {
        //tên kiểu xác thực của cookie đăng nhập (Startup.Auth.cs dùng cùng tên)
        public const string LoaiXacThuc = "PhasolproCookie";

        private const string ClaimHoTen = "phasolpro:HoTen";
        private const string ClaimTenHienThi = "phasolpro:TenHienThi";
        private const string ClaimDoanhNghiepID = "phasolpro:DoanhNghiepID";
        private const string ClaimPhaiDoiMatKhau = "phasolpro:PhaiDoiMatKhau";

        // Tạo danh tính (ClaimsIdentity) từ tài khoản vừa đăng nhập để ghi vào cookie
        public static ClaimsIdentity TaoDanhTinh(TaiKhoanRow tk)
        {
            List<Claim> ds = new List<Claim>();
            ds.Add(new Claim(ClaimTypes.NameIdentifier, tk.ID.ToString()));
            ds.Add(new Claim(ClaimTypes.Name, tk.TenDangNhap));
            ds.Add(new Claim(ClaimTypes.Role, tk.VaiTro));
            ds.Add(new Claim(ClaimHoTen, tk.HoTen ?? ""));
            ds.Add(new Claim(ClaimTenHienThi, tk.TenHienThi ?? ""));
            if (tk.DoanhNghiepID.HasValue)
                ds.Add(new Claim(ClaimDoanhNghiepID, tk.DoanhNghiepID.Value.ToString()));
            if (tk.PhaiDoiMatKhau)
                ds.Add(new Claim(ClaimPhaiDoiMatKhau, "1"));
            return new ClaimsIdentity(ds, LoaiXacThuc, ClaimTypes.Name, ClaimTypes.Role);
        }

        public static int LayID(IPrincipal user)
        {
            int id;
            return Int32.TryParse(LayClaim(user, ClaimTypes.NameIdentifier), out id) ? id : 0;
        }

        public static string LayVaiTro(IPrincipal user)
        {
            return LayClaim(user, ClaimTypes.Role);
        }

        public static string LayHoTen(IPrincipal user)
        {
            return LayClaim(user, ClaimHoTen);
        }

        //doanh nghiệp: tên công ty; còn lại: họ tên
        public static string LayTenHienThi(IPrincipal user)
        {
            string ten = LayClaim(user, ClaimTenHienThi);
            return ten != "" ? ten : LayClaim(user, ClaimTypes.Name);
        }

        public static int? LayDoanhNghiepID(IPrincipal user)
        {
            int id;
            return Int32.TryParse(LayClaim(user, ClaimDoanhNghiepID), out id) ? id : (int?)null;
        }

        public static bool PhaiDoiMatKhau(IPrincipal user)
        {
            return LayClaim(user, ClaimPhaiDoiMatKhau) == "1";
        }

        //chữ cái đầu cho ô tròn avatar
        public static string LayChuCaiDau(IPrincipal user)
        {
            string ten = LayTenHienThi(user).Trim();
            return ten == "" ? "?" : ten.Substring(0, 1).ToUpper();
        }

        private static string LayClaim(IPrincipal user, string loai)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return "";
            ClaimsIdentity danhTinh = user.Identity as ClaimsIdentity;
            if (danhTinh == null)
                return "";
            Claim c = danhTinh.FindFirst(loai);
            return c == null ? "" : c.Value;
        }
    }
}
