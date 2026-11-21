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

        // Ô chữ không dài quá số ký tự của cột trong database (tính sau khi bỏ khoảng trắng hai đầu, giống lúc lưu)
        public static void KiemTraDoDai(List<string> loi, string giaTri, int toiDa, string tenO)
        {
            if (giaTri != null && giaTri.Trim().Length > toiDa)
                loi.Add(string.Format("{0} dài tối đa {1} ký tự.", tenO, toiDa));
        }

        // Độ dài 3 ô của tài khoản không phải doanh nghiệp (cột HoTen, Email, SoDienThoai của bảng TaiKhoan)
        public static void KiemTraDoDaiTaiKhoan(List<string> loi, string hoTen, string email, string soDienThoai, string tenOHoTen)
        {
            KiemTraDoDai(loi, hoTen, 150, tenOHoTen);
            KiemTraDoDai(loi, email, 150, "Email");
            KiemTraDoDai(loi, soDienThoai, 30, "Số điện thoại");
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

            //độ dài theo cột của bảng DoanhNghiep (_rieng/sql-mysql/05_TaiKhoan.sql)
            KiemTraDoDai(loi, m.LoaiHinh, 100, "Loại hình doanh nghiệp");
            KiemTraDoDai(loi, m.MaSoDN, 50, "Mã số doanh nghiệp");
            KiemTraDoDai(loi, m.TenTiengViet, 300, "Tên doanh nghiệp (tiếng Việt)");
            KiemTraDoDai(loi, m.TenTiengAnh, 300, "Tên doanh nghiệp (tiếng Anh)");
            KiemTraDoDai(loi, m.DiaChiTiengAnh, 500, "Địa chỉ tiếng Anh");
            KiemTraDoDai(loi, m.CoQuanChuQuan, 300, "Cơ quan chủ quản");
            KiemTraDoDai(loi, m.SoDKKD, 50, "Số đăng ký kinh doanh");
            KiemTraDoDai(loi, m.TenVietTat, 100, "Tên viết tắt");
            KiemTraDoDai(loi, m.TinhThanh, 100, "Tỉnh/Thành phố");
            KiemTraDoDai(loi, m.XaPhuong, 100, "Xã/Phường");
            KiemTraDoDai(loi, m.DiaChiChiTiet, 500, "Địa chỉ chi tiết");
            KiemTraDoDai(loi, m.SoDienThoai, 30, "Số điện thoại doanh nghiệp");
            KiemTraDoDai(loi, m.Fax, 30, "Fax");
            KiemTraDoDai(loi, m.Email, 150, "Email doanh nghiệp");
            KiemTraDoDai(loi, m.Website, 200, "Website");
            KiemTraDoDai(loi, m.DaiDien_HoTen, 150, "Họ và tên người đại diện");
            KiemTraDoDai(loi, m.DaiDien_Email, 150, "Email người đại diện");
            KiemTraDoDai(loi, m.DaiDien_ChucVu, 100, "Chức vụ người đại diện");
            KiemTraDoDai(loi, m.DaiDien_SoDienThoai, 30, "Số điện thoại người đại diện");
            KiemTraDoDai(loi, m.VPDD_Ten, 300, "Tên văn phòng đại diện");
            KiemTraDoDai(loi, m.VPDD_SoDienThoai, 30, "Số điện thoại văn phòng đại diện");
            KiemTraDoDai(loi, m.VPDD_DiaChi, 500, "Địa chỉ văn phòng đại diện");

            KiemTraLienHe(loi, m.DSLienHe);
        }

        // Người liên hệ (gọi sau BoLienHeTrong): dòng đã điền thì phải có họ tên, email đúng dạng, không quá dài
        public static void KiemTraLienHe(List<string> loi, List<LienHeRow> ds)
        {
            if (ds == null)
                return;
            for (int i = 0; i < ds.Count; i++)
            {
                LienHeRow lh = ds[i];
                if (lh == null)
                    continue;
                string ten = string.Format("Người liên hệ thứ {0}: ", i + 1);
                if (String.IsNullOrWhiteSpace(lh.HoTen))
                    loi.Add(ten + "hãy nhập Họ và tên (hoặc xóa trắng cả dòng).");
                KiemTraEmail(loi, lh.Email, ten + "Email");
                KiemTraDoDai(loi, lh.HoTen, 150, ten + "Họ và tên");
                KiemTraDoDai(loi, lh.ChucVu, 100, ten + "Chức vụ");
                KiemTraDoDai(loi, lh.SoDienThoai, 30, ten + "Số điện thoại");
                KiemTraDoDai(loi, lh.Email, 150, ten + "Email");
            }
        }

        // Bỏ các dòng người liên hệ để trống hết các ô (dòng điền dở, thiếu họ tên thì giữ lại để KiemTraLienHe báo lỗi)
        public static void BoLienHeTrong(DangKyDoanhNghiepVM m)
        {
            if (m == null)
                return;
            if (m.DSLienHe == null)
                m.DSLienHe = new List<LienHeRow>();
            m.DSLienHe.RemoveAll(lh => lh == null || (String.IsNullOrWhiteSpace(lh.HoTen) && String.IsNullOrWhiteSpace(lh.ChucVu)
                && String.IsNullOrWhiteSpace(lh.SoDienThoai) && String.IsNullOrWhiteSpace(lh.Email)));
        }
    }
}
