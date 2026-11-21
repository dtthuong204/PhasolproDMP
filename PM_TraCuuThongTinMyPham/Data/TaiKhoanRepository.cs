using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //đọc / ghi bảng TaiKhoan, DoanhNghiep, DoanhNghiep_LienHe (tạo bằng _rieng/sql/04_TaiKhoan.sql, SQL Server)
    public class TaiKhoanRepository
    {
        //thông báo lỗi của lần gọi gần nhất, null nếu không có lỗi
        public string Loi { get; private set; }

        //các cột đọc ra TaiKhoanRow
        private const string SqlChonTaiKhoan = @"
SELECT t.ID, t.TenDangNhap, t.Email, t.MatKhauHash, t.VaiTro, t.HoTen, t.SoDienThoai, t.DoanhNghiepID,
       d.TenTiengViet AS TenDoanhNghiep, t.DangHoatDong, t.PhaiDoiMatKhau, t.NgayTao, t.LanDangNhapCuoi
FROM dbo.TaiKhoan AS t
LEFT JOIN dbo.DoanhNghiep AS d ON d.ID = t.DoanhNghiepID";

        #region Đọc tài khoản
        // Lấy tài khoản theo tên đăng nhập hoặc email (không phân biệt hoa thường), null nếu không có
        public TaiKhoanRow GetTaiKhoanDangNhap(string tenHoacEmail)
        {
            Loi = null;
            if (String.IsNullOrEmpty(tenHoacEmail))
                return null;
            string sql = SqlChonTaiKhoan + " WHERE t.TenDangNhap = @ten OR t.Email = @ten";
            List<TaiKhoanRow> ds = DocDS(sql, new SqlParameter("@ten", SqlDbType.NVarChar, 150) { Value = tenHoacEmail.Trim() });
            return ds.Count > 0 ? ds[0] : null;
        }

        // Lấy tài khoản theo ID, null nếu không có
        public TaiKhoanRow GetTaiKhoan(int id)
        {
            Loi = null;
            List<TaiKhoanRow> ds = DocDS(SqlChonTaiKhoan + " WHERE t.ID = @id", new SqlParameter("@id", SqlDbType.Int) { Value = id });
            return ds.Count > 0 ? ds[0] : null;
        }

        // Lấy danh sách mọi tài khoản, mới tạo trước
        public List<TaiKhoanRow> GetDSTaiKhoan()
        {
            Loi = null;
            return DocDS(SqlChonTaiKhoan + " ORDER BY t.NgayTao DESC, t.ID DESC");
        }

        // Kiểm tra trùng tên đăng nhập / email / mã số doanh nghiệp; trả về danh sách câu báo lỗi (rỗng nếu không trùng)
        public List<string> KiemTraTrung(string tenDangNhap, string email, string maSoDN)
        {
            Loi = null;
            List<string> kq = new List<string>();
            string sql = @"
SELECT
    (SELECT COUNT(*) FROM dbo.TaiKhoan WHERE TenDangNhap = @ten) AS TrungTen,
    (SELECT COUNT(*) FROM dbo.TaiKhoan WHERE @email <> N'' AND Email = @email) AS TrungEmail,
    (SELECT COUNT(*) FROM dbo.DoanhNghiep WHERE @maSo <> N'' AND MaSoDN = @maSo) AS TrungMaSo";
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    //bỏ khoảng trắng hai đầu giống lúc lưu (ThemChu), nếu không " a@b.vn " sẽ không bị coi là trùng "a@b.vn"
                    cmd.Parameters.Add("@ten", SqlDbType.NVarChar, 50).Value = (tenDangNhap ?? "").Trim();
                    cmd.Parameters.Add("@email", SqlDbType.NVarChar, 150).Value = (email ?? "").Trim();
                    cmd.Parameters.Add("@maSo", SqlDbType.NVarChar, 50).Value = (maSoDN ?? "").Trim();
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (Convert.ToInt32(reader["TrungTen"]) > 0)
                                kq.Add("Tên đăng nhập này đã có người dùng, hãy chọn tên khác.");
                            if (Convert.ToInt32(reader["TrungEmail"]) > 0)
                                kq.Add("Email này đã được dùng cho một tài khoản khác.");
                            if (Convert.ToInt32(reader["TrungMaSo"]) > 0)
                                kq.Add("Mã số doanh nghiệp này đã được đăng ký.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
            }
            return kq;
        }
        #endregion

        #region Thêm tài khoản
        // Thêm tài khoản Cá nhân hoặc Sở Y tế (không gắn doanh nghiệp), trả về ID mới, 0 nếu lỗi
        public int ThemTaiKhoan(TaiKhoanRow tk)
        {
            Loi = null;
            if (tk == null)
                return 0;
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                {
                    KetNoiDB.MoKetNoi(conn);
                    return ChenTaiKhoan(conn, null, tk);
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return 0;
            }
        }

        // Thêm doanh nghiệp + người liên hệ + tài khoản đăng nhập trong một giao dịch, trả về ID tài khoản, 0 nếu lỗi
        public int ThemDoanhNghiep(DangKyDoanhNghiepVM dn, string matKhauHash)
        {
            return ThemDoanhNghiep(dn, matKhauHash, false);
        }

        // Như trên; phaiDoiMatKhau = true khi Admin tạo tài khoản bằng mật khẩu tạm
        public int ThemDoanhNghiep(DangKyDoanhNghiepVM dn, string matKhauHash, bool phaiDoiMatKhau)
        {
            Loi = null;
            if (dn == null)
                return 0;
            string sqlDN = @"
INSERT INTO dbo.DoanhNghiep (LoaiHinh, MaSoDN, TenTiengViet, TenTiengAnh, DiaChiTiengAnh, CoQuanChuQuan, NamThanhLap,
    SoDKKD, TenVietTat, TinhThanh, XaPhuong, DiaChiChiTiet, SoDienThoai, Fax, Email, Website,
    DaiDien_HoTen, DaiDien_Email, DaiDien_ChucVu, DaiDien_SoDienThoai, VPDD_Ten, VPDD_SoDienThoai, VPDD_DiaChi)
VALUES (@LoaiHinh, @MaSoDN, @TenTiengViet, @TenTiengAnh, @DiaChiTiengAnh, @CoQuanChuQuan, @NamThanhLap,
    @SoDKKD, @TenVietTat, @TinhThanh, @XaPhuong, @DiaChiChiTiet, @SoDienThoai, @Fax, @Email, @Website,
    @DaiDien_HoTen, @DaiDien_Email, @DaiDien_ChucVu, @DaiDien_SoDienThoai, @VPDD_Ten, @VPDD_SoDienThoai, @VPDD_DiaChi);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                {
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlTransaction gd = conn.BeginTransaction())
                    {
                        int idDN;
                        using (SqlCommand cmd = new SqlCommand(sqlDN, conn, gd))
                        {
                            ThemThamSoDoanhNghiep(cmd, dn);
                            idDN = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        ChenLienHe(conn, gd, idDN, dn.DSLienHe);

                        //email đăng nhập = email người đại diện
                        TaiKhoanRow tk = new TaiKhoanRow
                        {
                            TenDangNhap = dn.TenDangNhap,
                            Email = dn.DaiDien_Email,
                            MatKhauHash = matKhauHash,
                            VaiTro = VaiTro.DoanhNghiep,
                            HoTen = dn.DaiDien_HoTen,
                            SoDienThoai = dn.DaiDien_SoDienThoai,
                            DoanhNghiepID = idDN,
                            PhaiDoiMatKhau = phaiDoiMatKhau
                        };
                        int idTK = ChenTaiKhoan(conn, gd, tk);
                        gd.Commit();
                        return idTK;
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return 0;
            }
        }
        #endregion

        #region Cập nhật tài khoản
        // Ghi thời điểm đăng nhập gần nhất
        public void CapNhatDangNhapCuoi(int id)
        {
            ThucThi("UPDATE dbo.TaiKhoan SET LanDangNhapCuoi = GETDATE() WHERE ID = @id",
                new SqlParameter("@id", SqlDbType.Int) { Value = id });
        }

        // Đổi mật khẩu (đã băm); phaiDoi = true khi Admin đặt lại mật khẩu tạm
        public bool DoiMatKhau(int id, string matKhauHash, bool phaiDoi)
        {
            return ThucThi("UPDATE dbo.TaiKhoan SET MatKhauHash = @hash, PhaiDoiMatKhau = @phaiDoi WHERE ID = @id",
                new SqlParameter("@hash", SqlDbType.VarChar, 200) { Value = matKhauHash },
                new SqlParameter("@phaiDoi", SqlDbType.Bit) { Value = phaiDoi },
                new SqlParameter("@id", SqlDbType.Int) { Value = id });
        }

        // Khóa / mở khóa tài khoản
        public bool DatTrangThai(int id, bool dangHoatDong)
        {
            return ThucThi("UPDATE dbo.TaiKhoan SET DangHoatDong = @hd WHERE ID = @id",
                new SqlParameter("@hd", SqlDbType.Bit) { Value = dangHoatDong },
                new SqlParameter("@id", SqlDbType.Int) { Value = id });
        }

        // Xóa hẳn một tài khoản; tài khoản doanh nghiệp thì xóa luôn thông tin doanh nghiệp và người liên hệ
        // (một giao dịch). Không khôi phục được: chỉ muốn chặn đăng nhập thì dùng DatTrangThai (khóa)
        public bool XoaTaiKhoan(int id)
        {
            Loi = null;
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                {
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlTransaction gd = conn.BeginTransaction())
                    {
                        object idDN;
                        using (SqlCommand cmd = new SqlCommand("SELECT DoanhNghiepID FROM dbo.TaiKhoan WHERE ID = @id", conn, gd))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                            idDN = cmd.ExecuteScalar();
                        }
                        int soDong;
                        using (SqlCommand cmd = new SqlCommand("DELETE FROM dbo.TaiKhoan WHERE ID = @id", conn, gd))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                            soDong = cmd.ExecuteNonQuery();
                        }
                        if (soDong > 0 && idDN != null && idDN != DBNull.Value)
                        {
                            //người liên hệ tự xóa theo doanh nghiệp (khóa ngoại ON DELETE CASCADE)
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM dbo.DoanhNghiep WHERE ID = @idDN", conn, gd))
                            {
                                cmd.Parameters.Add("@idDN", SqlDbType.Int).Value = Convert.ToInt32(idDN);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        gd.Commit();
                        return soDong > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return false;
            }
        }

        // Sửa họ tên, email, số điện thoại, vai trò của tài khoản KHÔNG phải doanh nghiệp (doanh nghiệp sửa bằng CapNhatDoanhNghiep)
        public bool CapNhatTaiKhoan(int id, string hoTen, string email, string soDienThoai, string vaiTro)
        {
            Loi = null;
            string sql = @"
UPDATE dbo.TaiKhoan SET HoTen = @HoTen, Email = @Email, SoDienThoai = @SoDienThoai, VaiTro = @VaiTro
WHERE ID = @id AND VaiTro <> 'DoanhNghiep' AND @VaiTro <> 'DoanhNghiep'";
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    ThemChu(cmd, "@HoTen", hoTen);
                    ThemChu(cmd, "@Email", email);
                    ThemChu(cmd, "@SoDienThoai", soDienThoai);
                    cmd.Parameters.Add("@VaiTro", SqlDbType.VarChar, 20).Value = vaiTro ?? "";
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    KetNoiDB.MoKetNoi(conn);
                    //số dòng = 0 khi tài khoản là doanh nghiệp (điều kiện WHERE chặn), không coi là lỗi
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return false;
            }
        }
        #endregion

        #region Doanh nghiệp: đọc / sửa
        // Lấy thông tin doanh nghiệp kèm người liên hệ, null nếu không có hoặc lỗi (xem Loi)
        public DangKyDoanhNghiepVM GetDoanhNghiep(int id)
        {
            Loi = null;
            string sql = @"
SELECT LoaiHinh, MaSoDN, TenTiengViet, TenTiengAnh, DiaChiTiengAnh, CoQuanChuQuan, NamThanhLap, SoDKKD, TenVietTat,
       TinhThanh, XaPhuong, DiaChiChiTiet, SoDienThoai, Fax, Email, Website,
       DaiDien_HoTen, DaiDien_Email, DaiDien_ChucVu, DaiDien_SoDienThoai, VPDD_Ten, VPDD_SoDienThoai, VPDD_DiaChi
FROM dbo.DoanhNghiep
WHERE ID = @id;

SELECT HoTen, ChucVu, SoDienThoai, Email
FROM dbo.DoanhNghiep_LienHe
WHERE DoanhNghiepID = @id
ORDER BY ID;";
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;
                        DangKyDoanhNghiepVM dn = DangKyDoanhNghiepVM.fromReader(reader);
                        reader.NextResult();
                        while (reader.Read())
                        {
                            dn.DSLienHe.Add(new LienHeRow
                            {
                                HoTen = Convert.ToString(reader["HoTen"]),
                                ChucVu = Convert.ToString(reader["ChucVu"]),
                                SoDienThoai = Convert.ToString(reader["SoDienThoai"]),
                                Email = Convert.ToString(reader["Email"])
                            });
                        }
                        return dn;
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return null;
            }
        }

        // Kiểm tra trùng email / mã số doanh nghiệp khi SỬA (bỏ qua chính tài khoản / doanh nghiệp đang sửa)
        public List<string> KiemTraTrungKhiSua(int idTaiKhoan, string email, int idDoanhNghiep, string maSoDN)
        {
            Loi = null;
            List<string> kq = new List<string>();
            string sql = @"
SELECT
    (SELECT COUNT(*) FROM dbo.TaiKhoan WHERE @email <> N'' AND Email = @email AND ID <> @idTK) AS TrungEmail,
    (SELECT COUNT(*) FROM dbo.DoanhNghiep WHERE @maSo <> N'' AND MaSoDN = @maSo AND ID <> @idDN) AS TrungMaSo";
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@email", SqlDbType.NVarChar, 150).Value = (email ?? "").Trim();
                    cmd.Parameters.Add("@maSo", SqlDbType.NVarChar, 50).Value = (maSoDN ?? "").Trim();
                    cmd.Parameters.Add("@idTK", SqlDbType.Int).Value = idTaiKhoan;
                    cmd.Parameters.Add("@idDN", SqlDbType.Int).Value = idDoanhNghiep;
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (Convert.ToInt32(reader["TrungEmail"]) > 0)
                                kq.Add("Email này đã được dùng cho một tài khoản khác.");
                            if (Convert.ToInt32(reader["TrungMaSo"]) > 0)
                                kq.Add("Mã số doanh nghiệp này đã được đăng ký.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
            }
            return kq;
        }

        // Sửa thông tin doanh nghiệp + thay danh sách người liên hệ + cập nhật họ tên, email, SĐT của tài khoản theo người đại diện
        // (một giao dịch); suaMaSo = false thì giữ nguyên mã số doanh nghiệp
        public bool CapNhatDoanhNghiep(int idDoanhNghiep, DangKyDoanhNghiepVM dn, bool suaMaSo)
        {
            Loi = null;
            if (dn == null)
                return false;
            string sql = @"
UPDATE dbo.DoanhNghiep SET
    LoaiHinh = @LoaiHinh, MaSoDN = CASE WHEN @suaMaSo = 1 THEN @MaSoDN ELSE MaSoDN END, TenTiengViet = @TenTiengViet,
    TenTiengAnh = @TenTiengAnh, DiaChiTiengAnh = @DiaChiTiengAnh, CoQuanChuQuan = @CoQuanChuQuan, NamThanhLap = @NamThanhLap,
    SoDKKD = @SoDKKD, TenVietTat = @TenVietTat, TinhThanh = @TinhThanh, XaPhuong = @XaPhuong, DiaChiChiTiet = @DiaChiChiTiet,
    SoDienThoai = @SoDienThoai, Fax = @Fax, Email = @Email, Website = @Website,
    DaiDien_HoTen = @DaiDien_HoTen, DaiDien_Email = @DaiDien_Email, DaiDien_ChucVu = @DaiDien_ChucVu,
    DaiDien_SoDienThoai = @DaiDien_SoDienThoai, VPDD_Ten = @VPDD_Ten, VPDD_SoDienThoai = @VPDD_SoDienThoai, VPDD_DiaChi = @VPDD_DiaChi
WHERE ID = @idDN;

DELETE FROM dbo.DoanhNghiep_LienHe WHERE DoanhNghiepID = @idDN;

UPDATE dbo.TaiKhoan SET HoTen = @DaiDien_HoTen, Email = @DaiDien_Email, SoDienThoai = @DaiDien_SoDienThoai
WHERE DoanhNghiepID = @idDN;";
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                {
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlTransaction gd = conn.BeginTransaction())
                    {
                        using (SqlCommand cmd = new SqlCommand(sql, conn, gd))
                        {
                            ThemThamSoDoanhNghiep(cmd, dn);
                            cmd.Parameters.Add("@suaMaSo", SqlDbType.Int).Value = suaMaSo ? 1 : 0;
                            cmd.Parameters.Add("@idDN", SqlDbType.Int).Value = idDoanhNghiep;
                            cmd.ExecuteNonQuery();
                        }
                        ChenLienHe(conn, gd, idDoanhNghiep, dn.DSLienHe);
                        gd.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return false;
            }
        }
        #endregion

        #region Hàm phụ trợ
        private List<TaiKhoanRow> DocDS(string sql, params SqlParameter[] thamSo)
        {
            List<TaiKhoanRow> kq = new List<TaiKhoanRow>();
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(thamSo);
                    KetNoiDB.MoKetNoi(conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            kq.Add(TaiKhoanRow.fromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                kq.Clear();
            }
            return kq;
        }

        private bool ThucThi(string sql, params SqlParameter[] thamSo)
        {
            Loi = null;
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(thamSo);
                    KetNoiDB.MoKetNoi(conn);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                return false;
            }
        }

        //chèn 1 dòng TaiKhoan (dùng chung cho thêm lẻ và trong giao dịch đăng ký doanh nghiệp)
        private static int ChenTaiKhoan(SqlConnection conn, SqlTransaction gd, TaiKhoanRow tk)
        {
            string sql = @"
INSERT INTO dbo.TaiKhoan (TenDangNhap, Email, MatKhauHash, VaiTro, HoTen, SoDienThoai, DoanhNghiepID, PhaiDoiMatKhau)
VALUES (@TenDangNhap, @Email, @MatKhauHash, @VaiTro, @HoTen, @SoDienThoai, @DoanhNghiepID, @PhaiDoiMatKhau);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (SqlCommand cmd = new SqlCommand(sql, conn, gd))
            {
                ThemChu(cmd, "@TenDangNhap", tk.TenDangNhap);
                ThemChu(cmd, "@Email", tk.Email);
                cmd.Parameters.Add("@MatKhauHash", SqlDbType.VarChar, 200).Value = (object)tk.MatKhauHash ?? DBNull.Value;
                cmd.Parameters.Add("@VaiTro", SqlDbType.VarChar, 20).Value = tk.VaiTro;
                ThemChu(cmd, "@HoTen", tk.HoTen);
                ThemChu(cmd, "@SoDienThoai", tk.SoDienThoai);
                cmd.Parameters.Add("@DoanhNghiepID", SqlDbType.Int).Value = tk.DoanhNghiepID.HasValue ? (object)tk.DoanhNghiepID.Value : DBNull.Value;
                cmd.Parameters.Add("@PhaiDoiMatKhau", SqlDbType.Bit).Value = tk.PhaiDoiMatKhau;
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        //23 tham số cột của bảng DoanhNghiep (dùng chung cho thêm và sửa)
        private static void ThemThamSoDoanhNghiep(SqlCommand cmd, DangKyDoanhNghiepVM dn)
        {
            ThemChu(cmd, "@LoaiHinh", dn.LoaiHinh);
            ThemChu(cmd, "@MaSoDN", dn.MaSoDN);
            ThemChu(cmd, "@TenTiengViet", dn.TenTiengViet);
            ThemChu(cmd, "@TenTiengAnh", dn.TenTiengAnh);
            ThemChu(cmd, "@DiaChiTiengAnh", dn.DiaChiTiengAnh);
            ThemChu(cmd, "@CoQuanChuQuan", dn.CoQuanChuQuan);
            int nam;
            cmd.Parameters.Add("@NamThanhLap", SqlDbType.Int).Value =
                Int32.TryParse((dn.NamThanhLap ?? "").Trim(), out nam) ? (object)nam : DBNull.Value;
            ThemChu(cmd, "@SoDKKD", dn.SoDKKD);
            ThemChu(cmd, "@TenVietTat", dn.TenVietTat);
            ThemChu(cmd, "@TinhThanh", dn.TinhThanh);
            ThemChu(cmd, "@XaPhuong", dn.XaPhuong);
            ThemChu(cmd, "@DiaChiChiTiet", dn.DiaChiChiTiet);
            ThemChu(cmd, "@SoDienThoai", dn.SoDienThoai);
            ThemChu(cmd, "@Fax", dn.Fax);
            ThemChu(cmd, "@Email", dn.Email);
            ThemChu(cmd, "@Website", dn.Website);
            ThemChu(cmd, "@DaiDien_HoTen", dn.DaiDien_HoTen);
            ThemChu(cmd, "@DaiDien_Email", dn.DaiDien_Email);
            ThemChu(cmd, "@DaiDien_ChucVu", dn.DaiDien_ChucVu);
            ThemChu(cmd, "@DaiDien_SoDienThoai", dn.DaiDien_SoDienThoai);
            ThemChu(cmd, "@VPDD_Ten", dn.VPDD_Ten);
            ThemChu(cmd, "@VPDD_SoDienThoai", dn.VPDD_SoDienThoai);
            ThemChu(cmd, "@VPDD_DiaChi", dn.VPDD_DiaChi);
        }

        //chèn danh sách người liên hệ của một doanh nghiệp (trong giao dịch đang mở)
        private static void ChenLienHe(SqlConnection conn, SqlTransaction gd, int idDoanhNghiep, List<LienHeRow> ds)
        {
            if (ds == null)
                return;
            string sql = @"
INSERT INTO dbo.DoanhNghiep_LienHe (DoanhNghiepID, HoTen, ChucVu, SoDienThoai, Email)
VALUES (@dn, @HoTen, @ChucVu, @SoDienThoai, @Email);";
            foreach (LienHeRow lh in ds)
            {
                if (lh == null || String.IsNullOrWhiteSpace(lh.HoTen))
                    continue;
                using (SqlCommand cmd = new SqlCommand(sql, conn, gd))
                {
                    cmd.Parameters.Add("@dn", SqlDbType.Int).Value = idDoanhNghiep;
                    ThemChu(cmd, "@HoTen", lh.HoTen);
                    ThemChu(cmd, "@ChucVu", lh.ChucVu);
                    ThemChu(cmd, "@SoDienThoai", lh.SoDienThoai);
                    ThemChu(cmd, "@Email", lh.Email);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //tham số chữ: bỏ khoảng trắng hai đầu, chuỗi rỗng ghi NULL
        private static void ThemChu(SqlCommand cmd, string ten, string giaTri)
        {
            string gon = (giaTri ?? "").Trim();
            cmd.Parameters.Add(ten, SqlDbType.NVarChar, -1).Value = gon == "" ? (object)DBNull.Value : gon;
        }
        #endregion
    }
}
