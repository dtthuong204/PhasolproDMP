using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using PM_TraCuuThongTinMyPham.Data;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    //trang quản trị: chỉ tài khoản Admin
    [PhanQuyen(Roles = VaiTro.Admin)]
    public class QuanTriController : Controller
    {
        //vai trò Admin được đổi qua lại khi sửa tài khoản (doanh nghiệp giữ nguyên vai trò)
        private static readonly string[] DSVaiTroDoiDuoc = { VaiTro.CaNhan, VaiTro.SoYTe, VaiTro.Admin };

        #region Danh sách tài khoản
        //danh sách tài khoản, lọc theo vai trò (vaiTro rỗng = tất cả) và từ khóa
        public ActionResult TaiKhoan(string vaiTro, string tuKhoa)
        {
            DatTieuDe("Quản lý tài khoản");
            QuanLyTaiKhoanVM model = new QuanLyTaiKhoanVM
            {
                VaiTroLoc = vaiTro ?? "",
                TuKhoa = (tuKhoa ?? "").Trim(),
                IDDangDangNhap = NguoiDung.LayID(User),
                ThongBao = TempData["ThongBao"] as string,
                Loi = TempData["Loi"] as string
            };

            TaiKhoanRepository repo = new TaiKhoanRepository();
            List<TaiKhoanRow> tatCa = repo.GetDSTaiKhoan();
            if (repo.Loi != null)
            {
                model.Loi = "Lỗi khi tải dữ liệu: " + repo.Loi;
                return View(model);
            }

            model.SoTheoVaiTro[""] = tatCa.Count;
            foreach (string vt in new string[] { VaiTro.DoanhNghiep, VaiTro.SoYTe, VaiTro.CaNhan, VaiTro.Admin })
                model.SoTheoVaiTro[vt] = 0;
            string tuKhoaThuong = model.TuKhoa.ToLower();
            foreach (TaiKhoanRow tk in tatCa)
            {
                if (model.SoTheoVaiTro.ContainsKey(tk.VaiTro))
                    model.SoTheoVaiTro[tk.VaiTro]++;
                if (model.VaiTroLoc != "" && tk.VaiTro != model.VaiTroLoc)
                    continue;
                if (tuKhoaThuong != "" && !ChuaTuKhoa(tk, tuKhoaThuong))
                    continue;
                model.DSTaiKhoan.Add(tk);
            }
            return View(model);
        }

        // Khóa / mở khóa một tài khoản (không tự khóa chính mình)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatTrangThai(int id, bool dangHoatDong, string vaiTro, string tuKhoa)
        {
            if (id == NguoiDung.LayID(User))
            {
                TempData["Loi"] = "Không thể tự khóa tài khoản đang đăng nhập.";
            }
            else
            {
                TaiKhoanRepository repo = new TaiKhoanRepository();
                TaiKhoanRow tk = repo.GetTaiKhoan(id);
                if (tk != null && repo.DatTrangThai(id, dangHoatDong))
                    TempData["ThongBao"] = string.Format("Đã {0} tài khoản {1}.", dangHoatDong ? "mở khóa" : "khóa", tk.TenDangNhap);
                else
                    TempData["Loi"] = "Lỗi khi lưu dữ liệu: " + (repo.Loi ?? "không tìm thấy tài khoản.");
            }
            return RedirectToAction("TaiKhoan", new { vaiTro = vaiTro, tuKhoa = tuKhoa });
        }

        // Đặt lại mật khẩu: tạo mật khẩu tạm, hiện một lần cho Admin, người dùng phải đổi khi đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatLaiMatKhau(int id, string vaiTro, string tuKhoa)
        {
            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(id);
            string matKhauTam = MatKhau.TaoMatKhauTam();
            if (tk != null && repo.DoiMatKhau(id, MatKhau.BamMatKhau(matKhauTam), true))
                TempData["ThongBao"] = string.Format("Mật khẩu tạm của tài khoản {0}: {1} (chỉ hiện một lần, hãy gửi cho người dùng; " +
                    "người dùng phải đổi mật khẩu khi đăng nhập).", tk.TenDangNhap, matKhauTam);
            else
                TempData["Loi"] = "Lỗi khi lưu dữ liệu: " + (repo.Loi ?? "không tìm thấy tài khoản.");
            return RedirectToAction("TaiKhoan", new { vaiTro = vaiTro, tuKhoa = tuKhoa });
        }
        #endregion

        #region Thêm tài khoản
        //form thêm tài khoản; vaiTro chọn bằng tab trên form (mặc định Cá nhân)
        public ActionResult ThemTaiKhoan(string vaiTro)
        {
            DatTieuDe("Thêm tài khoản");
            ThemTaiKhoanVM model = new ThemTaiKhoanVM { VaiTro = ChuanHoaVaiTro(vaiTro) };
            //form doanh nghiệp có sẵn 1 dòng người liên hệ
            model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTaiKhoan(ThemTaiKhoanVM model)
        {
            if (model == null)
                model = new ThemTaiKhoanVM();
            if (model.DoanhNghiep == null)
                model.DoanhNghiep = new DangKyDoanhNghiepVM();
            model.DSLoi = new List<string>();
            model.VaiTro = ChuanHoaVaiTro(model.VaiTro);
            DatTieuDe("Thêm tài khoản");

            bool laDoanhNghiep = model.VaiTro == VaiTro.DoanhNghiep;
            KiemTraNhap.BoLienHeTrong(model.DoanhNghiep);
            KiemTraNhap.KiemTraTenDangNhap(model.DSLoi, model.TenDangNhap);
            if (laDoanhNghiep)
            {
                KiemTraNhap.KiemTraDoanhNghiep(model.DSLoi, model.DoanhNghiep);
            }
            else
            {
                KiemTraNhap.KiemTraBatBuoc(model.DSLoi, model.HoTen, model.VaiTro == VaiTro.SoYTe ? "Tên đơn vị" : "Họ và tên");
                KiemTraNhap.KiemTraEmail(model.DSLoi, model.Email, "Email");
            }

            TaiKhoanRepository repo = new TaiKhoanRepository();
            if (model.DSLoi.Count == 0)
            {
                string email = laDoanhNghiep ? model.DoanhNghiep.DaiDien_Email : model.Email;
                model.DSLoi.AddRange(repo.KiemTraTrung(model.TenDangNhap.Trim(), (email ?? "").Trim(),
                    laDoanhNghiep ? model.DoanhNghiep.MaSoDN : null));
                if (repo.Loi != null)
                    model.DSLoi.Add("Lỗi khi tải dữ liệu: " + repo.Loi);
            }

            int idMoi = 0;
            string matKhauTam = MatKhau.TaoMatKhauTam();
            if (model.DSLoi.Count == 0)
            {
                string hash = MatKhau.BamMatKhau(matKhauTam);
                if (laDoanhNghiep)
                {
                    model.DoanhNghiep.TenDangNhap = model.TenDangNhap.Trim();
                    idMoi = repo.ThemDoanhNghiep(model.DoanhNghiep, hash, true);
                }
                else
                {
                    idMoi = repo.ThemTaiKhoan(new TaiKhoanRow
                    {
                        TenDangNhap = model.TenDangNhap.Trim(),
                        Email = model.Email,
                        MatKhauHash = hash,
                        VaiTro = model.VaiTro,
                        HoTen = model.HoTen,
                        SoDienThoai = model.SoDienThoai,
                        PhaiDoiMatKhau = true
                    });
                }
                if (idMoi == 0)
                    model.DSLoi.Add("Lỗi khi lưu dữ liệu: " + repo.Loi);
            }

            if (idMoi == 0)
            {
                if (model.DoanhNghiep.DSLienHe.Count == 0)
                    model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
                return View(model);
            }
            TempData["ThongBao"] = string.Format("Đã thêm tài khoản {0} ({1}). Mật khẩu tạm: {2} (chỉ hiện một lần, hãy gửi cho người dùng; " +
                "người dùng phải đổi mật khẩu khi đăng nhập).", model.TenDangNhap.Trim(), VaiTro.TenHienThi(model.VaiTro), matKhauTam);
            return RedirectToAction("TaiKhoan", new { vaiTro = model.VaiTro });
        }
        #endregion

        #region Xem / sửa tài khoản
        public ActionResult SuaTaiKhoan(int id)
        {
            DatTieuDe("Sửa tài khoản");
            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(id);
            if (tk == null)
            {
                TempData["Loi"] = repo.Loi != null ? "Lỗi khi tải dữ liệu: " + repo.Loi : "Không tìm thấy tài khoản.";
                return RedirectToAction("TaiKhoan");
            }

            SuaTaiKhoanVM model = new SuaTaiKhoanVM
            {
                ID = tk.ID,
                VaiTro = tk.VaiTro,
                HoTen = tk.HoTen,
                Email = tk.Email,
                SoDienThoai = tk.SoDienThoai,
                TaiKhoan = tk,
                LaMinh = tk.ID == NguoiDung.LayID(User),
                ThongBao = TempData["ThongBao"] as string
            };
            if (tk.VaiTro == VaiTro.DoanhNghiep && tk.DoanhNghiepID.HasValue)
            {
                DangKyDoanhNghiepVM dn = repo.GetDoanhNghiep(tk.DoanhNghiepID.Value);
                if (dn != null)
                    model.DoanhNghiep = dn;
                else
                    model.DSLoi.Add("Lỗi khi tải dữ liệu: " + (repo.Loi ?? "không tìm thấy thông tin doanh nghiệp."));
            }
            if (model.DoanhNghiep.DSLienHe.Count == 0)
                model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaTaiKhoan(SuaTaiKhoanVM model)
        {
            if (model == null)
                return RedirectToAction("TaiKhoan");
            if (model.DoanhNghiep == null)
                model.DoanhNghiep = new DangKyDoanhNghiepVM();
            model.DSLoi = new List<string>();
            DatTieuDe("Sửa tài khoản");

            //đọc lại tài khoản từ database: vai trò cũ, doanh nghiệp gắn kèm không lấy từ form
            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(model.ID);
            if (tk == null)
            {
                TempData["Loi"] = repo.Loi != null ? "Lỗi khi tải dữ liệu: " + repo.Loi : "Không tìm thấy tài khoản.";
                return RedirectToAction("TaiKhoan");
            }
            model.TaiKhoan = tk;
            model.LaMinh = tk.ID == NguoiDung.LayID(User);
            bool laDoanhNghiep = tk.VaiTro == VaiTro.DoanhNghiep;
            KiemTraNhap.BoLienHeTrong(model.DoanhNghiep);

            if (laDoanhNghiep)
            {
                if (!tk.DoanhNghiepID.HasValue)
                    model.DSLoi.Add("Tài khoản doanh nghiệp này không có thông tin doanh nghiệp.");
                KiemTraNhap.KiemTraDoanhNghiep(model.DSLoi, model.DoanhNghiep);
            }
            else
            {
                //không tự đổi vai trò của chính mình (luôn còn ít nhất một Admin)
                if (model.LaMinh)
                    model.VaiTro = tk.VaiTro;
                if (Array.IndexOf(DSVaiTroDoiDuoc, model.VaiTro) < 0)
                    model.DSLoi.Add("Vai trò không hợp lệ.");
                KiemTraNhap.KiemTraBatBuoc(model.DSLoi, model.HoTen, model.VaiTro == VaiTro.SoYTe ? "Tên đơn vị" : "Họ và tên");
                KiemTraNhap.KiemTraEmail(model.DSLoi, model.Email, "Email");
            }

            if (model.DSLoi.Count == 0)
            {
                string email = laDoanhNghiep ? model.DoanhNghiep.DaiDien_Email : model.Email;
                model.DSLoi.AddRange(repo.KiemTraTrungKhiSua(tk.ID, email,
                    laDoanhNghiep ? tk.DoanhNghiepID.Value : 0, laDoanhNghiep ? model.DoanhNghiep.MaSoDN : null));
                if (repo.Loi != null)
                    model.DSLoi.Add("Lỗi khi tải dữ liệu: " + repo.Loi);
            }

            bool daLuu = false;
            if (model.DSLoi.Count == 0)
            {
                daLuu = laDoanhNghiep
                    ? repo.CapNhatDoanhNghiep(tk.DoanhNghiepID.Value, model.DoanhNghiep, true)
                    : repo.CapNhatTaiKhoan(tk.ID, model.HoTen, model.Email, model.SoDienThoai, model.VaiTro);
                if (!daLuu)
                    model.DSLoi.Add("Lỗi khi lưu dữ liệu: " + repo.Loi);
            }
            if (!daLuu)
            {
                if (model.DoanhNghiep.DSLienHe.Count == 0)
                    model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
                return View(model);
            }

            //sửa chính mình: ghi lại cookie để header hiện tên mới
            if (model.LaMinh)
            {
                TaiKhoanRow moi = repo.GetTaiKhoan(tk.ID);
                if (moi != null)
                    NguoiDung.CapNhatCookie(HttpContext.GetOwinContext().Authentication, moi);
            }
            TempData["ThongBao"] = string.Format("Đã lưu tài khoản {0}.", tk.TenDangNhap);
            return RedirectToAction("SuaTaiKhoan", new { id = tk.ID });
        }
        #endregion

        #region Hàm phụ trợ
        private void DatTieuDe(string tieuDe)
        {
            ViewBag.Title = tieuDe;
            ViewBag.ActiveTab = "quantri";
        }

        //vai trò hợp lệ, sai thì về Cá nhân
        private static string ChuanHoaVaiTro(string vaiTro)
        {
            if (vaiTro == VaiTro.DoanhNghiep || vaiTro == VaiTro.SoYTe || vaiTro == VaiTro.Admin)
                return vaiTro;
            return VaiTro.CaNhan;
        }

        //tìm theo tên đăng nhập, họ tên, tên doanh nghiệp, email
        private static bool ChuaTuKhoa(TaiKhoanRow tk, string tuKhoaThuong)
        {
            string[] dsO = { tk.TenDangNhap, tk.HoTen, tk.TenDoanhNghiep, tk.Email };
            foreach (string o in dsO)
            {
                if (!String.IsNullOrEmpty(o) && o.ToLower().Contains(tuKhoaThuong))
                    return true;
            }
            return false;
        }
        #endregion
    }
}
