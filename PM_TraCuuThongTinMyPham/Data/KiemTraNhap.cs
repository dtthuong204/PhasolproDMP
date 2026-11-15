using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //kiểm tra dữ liệu nhập ở các form tài khoản (Đăng ký, Admin thêm / sửa tài khoản); mỗi lỗi thêm một câu vào danh sách loi
    public static class KiemTraNhap
    {
        // Ô bắt buộc phải có chữ
        public static void KiemTraBatBuoc(List<string> loi, string giaTri, string tenO)
        {
            if (String.IsNullOrWhiteSpace(giaTri))
                loi.Add(string.Format("Hãy nhập {0}.", tenO));
        }

        // Email (nếu có) phải đúng dạng ten@mien.xx
        public static void KiemTraEmail(List<string> loi, string email, string tenO)
        {
            if (!String.IsNullOrWhiteSpace(email) && !Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                loi.Add(string.Format("{0} không đúng định dạng.", tenO));
        }

        // Tên đăng nhập 3-50 ký tự: chữ không dấu, số, dấu chấm, gạch dưới, gạch ngang
        public static void KiemTraTenDangNhap(List<string> loi, string ten)
        {
            if (String.IsNullOrWhiteSpace(ten))
                loi.Add("Hãy nhập Tên đăng nhập.");
            else if (!Regex.IsMatch(ten.Trim(), @"^[A-Za-z0-9._-]{3,50}$"))
                loi.Add("Tên đăng nhập dài 3-50 ký tự, chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới, gạch ngang.");
        }

        // Thông tin doanh nghiệp (không gồm phần tài khoản đăng nhập)
        public static void KiemTraDoanhNghiep(List<string> loi, DangKyDoanhNghiepVM m)
        {
            if (m == null)
            {
                loi.Add("Thiếu thông tin doanh nghiệp.");
                return;
            }
            KiemTraBatBuoc(loi, m.LoaiHinh, "Loại hình doanh nghiệp");
            KiemTraBatBuoc(loi, m.MaSoDN, "Mã số doanh nghiệp");
            KiemTraBatBuoc(loi, m.TenTiengViet, "Tên doanh nghiệp (tiếng Việt)");
            KiemTraBatBuoc(loi, m.TinhThanh, "Tỉnh/Thành phố");
            KiemTraBatBuoc(loi, m.XaPhuong, "Xã/Phường");
            KiemTraBatBuoc(loi, m.DiaChiChiTiet, "Địa chỉ chi tiết");
            KiemTraBatBuoc(loi, m.SoDienThoai, "Số điện thoại doanh nghiệp");
            KiemTraBatBuoc(loi, m.DaiDien_HoTen, "Họ và tên người đại diện");
            KiemTraBatBuoc(loi, m.DaiDien_Email, "Email người đại diện");
            KiemTraBatBuoc(loi, m.DaiDien_ChucVu, "Chức vụ người đại diện");
            KiemTraBatBuoc(loi, m.DaiDien_SoDienThoai, "Số điện thoại người đại diện");
            KiemTraEmail(loi, m.Email, "Email doanh nghiệp");
            KiemTraEmail(loi, m.DaiDien_Email, "Email người đại diện");
            int nam;
            if (!String.IsNullOrWhiteSpace(m.NamThanhLap) &&
                (!Int32.TryParse(m.NamThanhLap.Trim(), out nam) || nam < 1900 || nam > DateTime.Now.Year))
                loi.Add("Năm thành lập không hợp lệ.");
        }

        // Bỏ các dòng người liên hệ để trống họ tên
        public static void BoLienHeTrong(DangKyDoanhNghiepVM m)
        {
            if (m == null)
                return;
            if (m.DSLienHe == null)
                m.DSLienHe = new List<LienHeRow>();
            m.DSLienHe.RemoveAll(lh => lh == null || String.IsNullOrWhiteSpace(lh.HoTen));
        }
    }
}
