using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using PM_TraCuuThongTinMyPham.Data;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    //đăng nhập, đăng xuất, đăng ký (Cá nhân / Doanh nghiệp), đổi mật khẩu
    public class TaiKhoanController : Controller
    {
        #region Đăng nhập / đăng xuất
        [AllowAnonymous]
        public ActionResult DangNhap(string returnUrl)
        {
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            ViewBag.Title = "Đăng nhập";
            DangNhapVM model = new DangNhapVM { ReturnUrl = returnUrl, ThongBao = TempData["ThongBao"] as string };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(DangNhapVM model)
        {
            if (model == null)
                model = new DangNhapVM();
            ViewBag.Title = "Đăng nhập";

            if (String.IsNullOrWhiteSpace(model.TenDangNhap) || String.IsNullOrEmpty(model.MatKhau))
            {
                model.Loi = "Hãy nhập tên đăng nhập (hoặc email) và mật khẩu.";
                return View(model);
            }

            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoanDangNhap(model.TenDangNhap);
            if (repo.Loi != null)
            {
                model.Loi = "Lỗi khi tải dữ liệu: " + repo.Loi;
                return View(model);
            }
            //không nói rõ sai tên hay sai mật khẩu để người ngoài không dò được tên tài khoản
            if (tk == null || !MatKhau.KiemTra(model.MatKhau, tk.MatKhauHash))
            {
                model.Loi = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View(model);
            }
            if (!tk.DangHoatDong)
            {
                model.Loi = "Tài khoản đã bị khóa. Hãy liên hệ quản trị hệ thống.";
                return View(model);
            }

            GhiCookieDangNhap(tk, model.LuuTaiKhoan);
            repo.CapNhatDangNhapCuoi(tk.ID);

            if (tk.PhaiDoiMatKhau)
                return RedirectToAction("DoiMatKhau");
            //chỉ quay lại địa chỉ trong trang web này (chống chuyển hướng sang trang lạ)
            if (Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangXuat()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(NguoiDung.LoaiXacThuc);
            return RedirectToAction("Index", "Home");
        }

        //đã đăng nhập nhưng vai trò không được vào trang đó
        public ActionResult KhongCoQuyen()
        {
            ViewBag.Title = "Không có quyền truy cập";
            return View();
        }
        #endregion

        #region Đăng ký
        [AllowAnonymous]
        public ActionResult DangKy(string loai)
        {
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            ViewBag.Title = "Đăng ký tài khoản";
            DangKyVM model = new DangKyVM { Loai = loai == "doanhnghiep" ? "doanhnghiep" : "canhan", ReCaptchaSiteKey = LayKhoaReCaptcha() };
            //form doanh nghiệp có sẵn 1 dòng người liên hệ
            model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(DangKyVM model)
        {
            if (model == null)
                model = new DangKyVM();
            if (model.CaNhan == null)
                model.CaNhan = new DangKyCaNhanVM();
            if (model.DoanhNghiep == null)
                model.DoanhNghiep = new DangKyDoanhNghiepVM();
            if (model.DoanhNghiep.DSLienHe == null)
                model.DoanhNghiep.DSLienHe = new List<LienHeRow>();
            model.Loai = model.Loai == "doanhnghiep" ? "doanhnghiep" : "canhan";
            model.ReCaptchaSiteKey = LayKhoaReCaptcha();
            ViewBag.Title = "Đăng ký tài khoản";

            bool laDoanhNghiep = model.Loai == "doanhnghiep";
            model.DSLoi = laDoanhNghiep ? KiemTraDoanhNghiep(model.DoanhNghiep) : KiemTraCaNhan(model.CaNhan);
            //bỏ các dòng người liên hệ để trống
            model.DoanhNghiep.DSLienHe.RemoveAll(lh => lh == null || String.IsNullOrWhiteSpace(lh.HoTen));

            if (model.DSLoi.Count == 0)
            {
                string loiBot = ReCaptcha.KiemTra(Request.Form["g-recaptcha-response"], Request.UserHostAddress);
                if (loiBot != null)
                    model.DSLoi.Add(loiBot);
            }

            TaiKhoanRepository repo = new TaiKhoanRepository();
            if (model.DSLoi.Count == 0)
            {
                List<string> trung = laDoanhNghiep
                    ? repo.KiemTraTrung(model.DoanhNghiep.TenDangNhap, model.DoanhNghiep.DaiDien_Email, model.DoanhNghiep.MaSoDN)
                    : repo.KiemTraTrung(model.CaNhan.TenDangNhap, model.CaNhan.Email, null);
                if (repo.Loi != null)
                    model.DSLoi.Add("Lỗi khi tải dữ liệu: " + repo.Loi);
                model.DSLoi.AddRange(trung);
            }

            if (model.DSLoi.Count > 0)
            {
                if (laDoanhNghiep && model.DoanhNghiep.DSLienHe.Count == 0)
                    model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
                return View(model);
            }

            int idMoi;
            if (laDoanhNghiep)
            {
                idMoi = repo.ThemDoanhNghiep(model.DoanhNghiep, MatKhau.BamMatKhau(model.DoanhNghiep.MatKhau));
            }
            else
            {
                idMoi = repo.ThemTaiKhoan(new TaiKhoanRow
                {
                    TenDangNhap = model.CaNhan.TenDangNhap.Trim(),
                    Email = model.CaNhan.Email,
                    MatKhauHash = MatKhau.BamMatKhau(model.CaNhan.MatKhau),
                    VaiTro = VaiTro.CaNhan,
                    HoTen = model.CaNhan.HoTen,
                    SoDienThoai = model.CaNhan.SoDienThoai
                });
            }
            if (idMoi == 0)
            {
                model.DSLoi.Add("Lỗi khi lưu dữ liệu: " + repo.Loi);
                if (laDoanhNghiep && model.DoanhNghiep.DSLienHe.Count == 0)
                    model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
                return View(model);
            }

            //đăng ký xong thì đăng nhập luôn
            TaiKhoanRow tk = repo.GetTaiKhoan(idMoi);
            if (tk != null)
            {
                GhiCookieDangNhap(tk, false);
                repo.CapNhatDangNhapCuoi(tk.ID);
            }
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Đổi mật khẩu
        public ActionResult DoiMatKhau()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View(new DoiMatKhauVM { BatBuoc = NguoiDung.PhaiDoiMatKhau(User) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(DoiMatKhauVM model)
        {
            if (model == null)
                model = new DoiMatKhauVM();
            ViewBag.Title = "Đổi mật khẩu";
            model.BatBuoc = NguoiDung.PhaiDoiMatKhau(User);

            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(NguoiDung.LayID(User));
            if (tk == null)
            {
                model.Loi = repo.Loi != null ? "Lỗi khi tải dữ liệu: " + repo.Loi : "Không tìm thấy tài khoản.";
                return View(model);
            }
            if (!MatKhau.KiemTra(model.MatKhauHienTai, tk.MatKhauHash))
                model.Loi = "Mật khẩu hiện tại không đúng.";
            else if (model.MatKhauMoi != model.NhapLaiMatKhau)
                model.Loi = "Xác nhận mật khẩu mới không khớp.";
            else if (model.MatKhauMoi == model.MatKhauHienTai)
                model.Loi = "Mật khẩu mới phải khác mật khẩu hiện tại.";
            else
                model.Loi = MatKhau.KiemTraDoManh(model.MatKhauMoi);
            if (model.Loi != null)
                return View(model);

            if (!repo.DoiMatKhau(tk.ID, MatKhau.BamMatKhau(model.MatKhauMoi), false))
            {
                model.Loi = "Lỗi khi lưu dữ liệu: " + repo.Loi;
                return View(model);
            }
            //ghi lại cookie để bỏ cờ "phải đổi mật khẩu"
            tk.PhaiDoiMatKhau = false;
            GhiCookieDangNhap(tk, false);
            TempData["ThongBao"] = "Đã đổi mật khẩu.";
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Hàm phụ trợ
        //ghi cookie đăng nhập; luu = true thì giữ 14 ngày kể cả khi đóng trình duyệt
        private void GhiCookieDangNhap(TaiKhoanRow tk, bool luu)
        {
            IAuthenticationManager xacThuc = HttpContext.GetOwinContext().Authentication;
            xacThuc.SignOut(NguoiDung.LoaiXacThuc);
            xacThuc.SignIn(new AuthenticationProperties { IsPersistent = luu }, NguoiDung.TaoDanhTinh(tk));
        }

        private static string LayKhoaReCaptcha()
        {
            return ReCaptcha.DangBat ? ReCaptcha.SiteKey : "";
        }

        private static List<string> KiemTraCaNhan(DangKyCaNhanVM m)
        {
            List<string> loi = new List<string>();
            KiemTraNhap.KiemTraBatBuoc(loi, m.HoTen, "Họ và tên");
            KiemTraNhap.KiemTraBatBuoc(loi, m.Email, "Email");
            KiemTraNhap.KiemTraEmail(loi, m.Email, "Email");
            KiemTraTaiKhoan(loi, m.TenDangNhap, m.MatKhau, m.NhapLaiMatKhau);
            return loi;
        }

        private static List<string> KiemTraDoanhNghiep(DangKyDoanhNghiepVM m)
        {
            List<string> loi = new List<string>();
            KiemTraTaiKhoan(loi, m.TenDangNhap, m.MatKhau, m.NhapLaiMatKhau);
            KiemTraNhap.KiemTraDoanhNghiep(loi, m);
            return loi;
        }

        //tên đăng nhập + mật khẩu (đủ mạnh, nhập lại khớp)
        private static void KiemTraTaiKhoan(List<string> loi, string ten, string matKhau, string nhapLai)
        {
            KiemTraNhap.KiemTraTenDangNhap(loi, ten);
            string loiMatKhau = MatKhau.KiemTraDoManh(matKhau);
            if (loiMatKhau != null)
                loi.Add(loiMatKhau);
            else if (matKhau != nhapLai)
                loi.Add("Nhập lại mật khẩu không khớp.");
        }
        #endregion
    }
}
