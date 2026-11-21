using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PM_TraCuuThongTinMyPham.Models
{
    //4 vai trò lưu ở cột TaiKhoan.VaiTro
    public static class VaiTro
    {
        public const string Admin = "Admin";
        public const string SoYTe = "SoYTe";
        public const string DoanhNghiep = "DoanhNghiep";
        public const string CaNhan = "CaNhan";

        //tên hiển thị trên giao diện
        public static string TenHienThi(string vaiTro)
        {
            if (vaiTro == Admin) return "Quản trị";
            if (vaiTro == SoYTe) return "Sở Y tế";
            if (vaiTro == DoanhNghiep) return "Doanh nghiệp";
            if (vaiTro == CaNhan) return "Cá nhân";
            return vaiTro;
        }
    }

    //1 tài khoản (bảng TaiKhoan, kèm tên doanh nghiệp nếu có)
    public class TaiKhoanRow
    {
        public int ID { get; set; }
        public string TenDangNhap { get; set; }
        public string Email { get; set; }
        public string MatKhauHash { get; set; }
        public string VaiTro { get; set; }
        public string HoTen { get; set; }
        public string SoDienThoai { get; set; }
        public int? DoanhNghiepID { get; set; }
        public string TenDoanhNghiep { get; set; }
        public bool DangHoatDong { get; set; }
        public bool PhaiDoiMatKhau { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }

        //tên hiện ở góc phải header và bảng quản lý: doanh nghiệp hiện tên công ty, còn lại hiện họ tên
        public string TenHienThi
        {
            get { return String.IsNullOrEmpty(TenDoanhNghiep) ? HoTen : TenDoanhNghiep; }
        }

        public static TaiKhoanRow fromReader(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            return new TaiKhoanRow
            {
                ID = Convert.ToInt32(reader["ID"]),
                TenDangNhap = Convert.ToString(reader["TenDangNhap"]),
                Email = Convert.ToString(reader["Email"]),
                MatKhauHash = Convert.ToString(reader["MatKhauHash"]),
                VaiTro = Convert.ToString(reader["VaiTro"]),
                HoTen = Convert.ToString(reader["HoTen"]),
                SoDienThoai = Convert.ToString(reader["SoDienThoai"]),
                DoanhNghiepID = reader["DoanhNghiepID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DoanhNghiepID"]),
                TenDoanhNghiep = Convert.ToString(reader["TenDoanhNghiep"]),
                DangHoatDong = Convert.ToBoolean(reader["DangHoatDong"]),
                PhaiDoiMatKhau = Convert.ToBoolean(reader["PhaiDoiMatKhau"]),
                NgayTao = Convert.ToDateTime(reader["NgayTao"]),
                LanDangNhapCuoi = reader["LanDangNhapCuoi"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["LanDangNhapCuoi"])
            };
        }
    }

    //form đăng nhập (trang chủ khi chưa đăng nhập và trang /TaiKhoan/DangNhap)
    public class DangNhapVM
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public bool LuuTaiKhoan { get; set; }
        public string ReturnUrl { get; set; }
        public string Loi { get; set; }
        public string ThongBao { get; set; }
    }

    //trang Đăng ký: 2 tab Cá nhân / Doanh nghiệp
    public class DangKyVM
    {
        //canhan hoặc doanhnghiep
        public string Loai { get; set; }
        public DangKyCaNhanVM CaNhan { get; set; }
        public DangKyDoanhNghiepVM DoanhNghiep { get; set; }
        public List<string> DSLoi { get; set; }
        public string ReCaptchaSiteKey { get; set; }

        public DangKyVM()
        {
            Loai = "canhan";
            CaNhan = new DangKyCaNhanVM();
            DoanhNghiep = new DangKyDoanhNghiepVM();
            DSLoi = new List<string>();
        }
    }

    public class DangKyCaNhanVM
    {
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string NhapLaiMatKhau { get; set; }
    }

    //các mục theo thiết kế docs/giao-dien/Đăng nhập/Đăng ký.png, thêm phần tài khoản đăng nhập
    public class DangKyDoanhNghiepVM
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string NhapLaiMatKhau { get; set; }

        public string LoaiHinh { get; set; }
        public string MaSoDN { get; set; }
        public string TenTiengViet { get; set; }
        public string TenTiengAnh { get; set; }
        public string DiaChiTiengAnh { get; set; }
        public string CoQuanChuQuan { get; set; }
        public string NamThanhLap { get; set; }
        public string SoDKKD { get; set; }
        public string TenVietTat { get; set; }

        public string TinhThanh { get; set; }
        public string XaPhuong { get; set; }
        public string DiaChiChiTiet { get; set; }
        public string SoDienThoai { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }

        public string DaiDien_HoTen { get; set; }
        public string DaiDien_Email { get; set; }
        public string DaiDien_ChucVu { get; set; }
        public string DaiDien_SoDienThoai { get; set; }

        public string VPDD_Ten { get; set; }
        public string VPDD_SoDienThoai { get; set; }
        public string VPDD_DiaChi { get; set; }

        public List<LienHeRow> DSLienHe { get; set; }

        public DangKyDoanhNghiepVM()
        {
            DSLienHe = new List<LienHeRow>();
        }

        //đọc 1 dòng bảng DoanhNghiep (ô trống thành chuỗi rỗng); danh sách liên hệ đọc riêng
        public static DangKyDoanhNghiepVM fromReader(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            return new DangKyDoanhNghiepVM
            {
                LoaiHinh = Convert.ToString(reader["LoaiHinh"]),
                MaSoDN = Convert.ToString(reader["MaSoDN"]),
                TenTiengViet = Convert.ToString(reader["TenTiengViet"]),
                TenTiengAnh = Convert.ToString(reader["TenTiengAnh"]),
                DiaChiTiengAnh = Convert.ToString(reader["DiaChiTiengAnh"]),
                CoQuanChuQuan = Convert.ToString(reader["CoQuanChuQuan"]),
                NamThanhLap = Convert.ToString(reader["NamThanhLap"]),
                SoDKKD = Convert.ToString(reader["SoDKKD"]),
                TenVietTat = Convert.ToString(reader["TenVietTat"]),
                TinhThanh = Convert.ToString(reader["TinhThanh"]),
                XaPhuong = Convert.ToString(reader["XaPhuong"]),
                DiaChiChiTiet = Convert.ToString(reader["DiaChiChiTiet"]),
                SoDienThoai = Convert.ToString(reader["SoDienThoai"]),
                Fax = Convert.ToString(reader["Fax"]),
                Email = Convert.ToString(reader["Email"]),
                Website = Convert.ToString(reader["Website"]),
                DaiDien_HoTen = Convert.ToString(reader["DaiDien_HoTen"]),
                DaiDien_Email = Convert.ToString(reader["DaiDien_Email"]),
                DaiDien_ChucVu = Convert.ToString(reader["DaiDien_ChucVu"]),
                DaiDien_SoDienThoai = Convert.ToString(reader["DaiDien_SoDienThoai"]),
                VPDD_Ten = Convert.ToString(reader["VPDD_Ten"]),
                VPDD_SoDienThoai = Convert.ToString(reader["VPDD_SoDienThoai"]),
                VPDD_DiaChi = Convert.ToString(reader["VPDD_DiaChi"])
            };
        }
    }

    //1 người liên hệ của doanh nghiệp
    public class LienHeRow
    {
        public string HoTen { get; set; }
        public string ChucVu { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
    }

    //trang Đổi mật khẩu
    public class DoiMatKhauVM
    {
        public string MatKhauHienTai { get; set; }
        public string MatKhauMoi { get; set; }
        public string NhapLaiMatKhau { get; set; }
        //true khi đang dùng mật khẩu tạm (Admin cấp / đặt lại): bắt đổi trước khi dùng tiếp
        public bool BatBuoc { get; set; }
        public string Loi { get; set; }
    }

    //trang Quản lý tài khoản (Admin)
    public class QuanLyTaiKhoanVM
    {
        public List<TaiKhoanRow> DSTaiKhoan { get; set; }
        //vai trò đang lọc, rỗng = tất cả
        public string VaiTroLoc { get; set; }
        public string TuKhoa { get; set; }
        //số tài khoản theo vai trò (khóa "" = tất cả)
        public Dictionary<string, int> SoTheoVaiTro { get; set; }
        public int IDDangDangNhap { get; set; }
        public string ThongBao { get; set; }
        public string Loi { get; set; }

        public QuanLyTaiKhoanVM()
        {
            DSTaiKhoan = new List<TaiKhoanRow>();
            SoTheoVaiTro = new Dictionary<string, int>();
            VaiTroLoc = "";
            TuKhoa = "";
        }
    }

    //form Admin thêm tài khoản (mọi vai trò); mật khẩu tạm do hệ thống tạo, người dùng phải đổi khi đăng nhập lần đầu
    public class ThemTaiKhoanVM
    {
        public string VaiTro { get; set; }
        public string TenDangNhap { get; set; }
        //3 ô dưới dùng cho Cá nhân, Sở Y tế, Quản trị; doanh nghiệp lấy theo người đại diện
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public DangKyDoanhNghiepVM DoanhNghiep { get; set; }
        public List<string> DSLoi { get; set; }

        public ThemTaiKhoanVM()
        {
            VaiTro = Models.VaiTro.CaNhan;
            DoanhNghiep = new DangKyDoanhNghiepVM();
            DSLoi = new List<string>();
        }
    }

    //trang Admin xem / sửa một tài khoản
    public class SuaTaiKhoanVM
    {
        public int ID { get; set; }
        //vai trò mới chọn (Cá nhân / Sở Y tế / Quản trị); doanh nghiệp không đổi vai trò
        public string VaiTro { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public DangKyDoanhNghiepVM DoanhNghiep { get; set; }

        //phần chỉ xem, đọc lại từ database mỗi lần (không lấy từ form gửi lên)
        public TaiKhoanRow TaiKhoan { get; set; }
        //đang sửa chính tài khoản đang đăng nhập: không đổi vai trò
        public bool LaMinh { get; set; }
        public List<string> DSLoi { get; set; }
        public string ThongBao { get; set; }

        public SuaTaiKhoanVM()
        {
            DoanhNghiep = new DangKyDoanhNghiepVM();
            DSLoi = new List<string>();
        }
    }
}
