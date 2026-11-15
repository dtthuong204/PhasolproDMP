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
    //ValidateInput(false): cho nhập ký tự < > & (vd mật khẩu "Ab<c1234", tên "Công ty A & B <VN>"), mặc định ASP.NET báo lỗi 500.
    //An toàn vì view in ra bằng @ (Razor tự đổi < thành &lt;) và câu SQL dùng tham số @...
    [ValidateInput(false)]
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

            //sai quá nhiều lần liên tiếp thì phải chờ (chống dò mật khẩu), xem Data/ChongDoMatKhau.cs
            string ip = Request.UserHostAddress;
            int phutCho = ChongDoMatKhau.SoPhutPhaiCho(model.TenDangNhap, ip);
            if (phutCho > 0)
            {
                model.Loi = string.Format("Đăng nhập sai quá nhiều lần. Hãy thử lại sau {0} phút.", phutCho);
                return View(model);
            }

            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoanDangNhap(model.TenDangNhap);
            if (repo.Loi != null)
            {
                model.Loi = "Lỗi khi tải dữ liệu: " + repo.Loi;
                return View(model);
            }
            //không nói rõ sai tên hay sai mật khẩu để người ngoài không dò được tên tài khoản;
            //tên không tồn tại vẫn tính băm một lần (KiemTraGia) để thời gian trả lời giống tên có thật
            bool dungMatKhau = tk != null ? MatKhau.KiemTra(model.MatKhau, tk.MatKhauHash) : MatKhau.KiemTraGia(model.MatKhau);
            if (!dungMatKhau)
            {
                ChongDoMatKhau.GhiLanSai(model.TenDangNhap, ip);
                model.Loi = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View(model);
            }
            if (!tk.DangHoatDong)
            {
                model.Loi = "Tài khoản đã bị khóa. Hãy liên hệ quản trị hệ thống.";
                return View(model);
            }

            ChongDoMatKhau.XoaLanSai(model.TenDangNhap, ip);
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
            //bỏ các dòng người liên hệ để trống hết các ô
            KiemTraNhap.BoLienHeTrong(model.DoanhNghiep);
            model.DSLoi = laDoanhNghiep ? KiemTraDoanhNghiep(model.DoanhNghiep) : KiemTraCaNhan(model.CaNhan);

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

        #region Tài khoản của tôi
        //xem + tự sửa thông tin của tài khoản đang đăng nhập (mọi vai trò); tên đăng nhập, vai trò không sửa;
        //doanh nghiệp sửa được thông tin doanh nghiệp trừ mã số doanh nghiệp (chỉ Admin sửa)
        public ActionResult CuaToi()
        {
            ViewBag.Title = "Tài khoản của tôi";
            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(NguoiDung.LayID(User));
            if (tk == null)
            {
                TempData["ThongBao"] = repo.Loi != null ? "Lỗi khi tải dữ liệu: " + repo.Loi : "Không tìm thấy tài khoản.";
                return RedirectToAction("Index", "Home");
            }

            SuaTaiKhoanVM model = new SuaTaiKhoanVM
            {
                ID = tk.ID,
                VaiTro = tk.VaiTro,
                HoTen = tk.HoTen,
                Email = tk.Email,
                SoDienThoai = tk.SoDienThoai,
                TaiKhoan = tk,
                LaMinh = true,
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
        public ActionResult CuaToi(SuaTaiKhoanVM model)
        {
            if (model == null)
                model = new SuaTaiKhoanVM();
            if (model.DoanhNghiep == null)
                model.DoanhNghiep = new DangKyDoanhNghiepVM();
            model.DSLoi = new List<string>();
            ViewBag.Title = "Tài khoản của tôi";

            //luôn sửa tài khoản đang đăng nhập (không lấy ID, vai trò từ form gửi lên)
            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(NguoiDung.LayID(User));
            if (tk == null)
            {
                TempData["ThongBao"] = repo.Loi != null ? "Lỗi khi tải dữ liệu: " + repo.Loi : "Không tìm thấy tài khoản.";
                return RedirectToAction("Index", "Home");
            }
            model.ID = tk.ID;
            model.VaiTro = tk.VaiTro;
            model.TaiKhoan = tk;
            model.LaMinh = true;
            bool laDoanhNghiep = tk.VaiTro == VaiTro.DoanhNghiep && tk.DoanhNghiepID.HasValue;
            KiemTraNhap.BoLienHeTrong(model.DoanhNghiep);

            if (laDoanhNghiep)
            {
                //mã số doanh nghiệp chỉ xem: lấy lại giá trị đang lưu để kiểm tra và hiện lại trên form
                DangKyDoanhNghiepVM dnCu = repo.GetDoanhNghiep(tk.DoanhNghiepID.Value);
                if (dnCu == null)
                    model.DSLoi.Add("Lỗi khi tải dữ liệu: " + (repo.Loi ?? "không tìm thấy thông tin doanh nghiệp."));
                else
                    model.DoanhNghiep.MaSoDN = dnCu.MaSoDN;
                KiemTraNhap.KiemTraDoanhNghiep(model.DSLoi, model.DoanhNghiep);
            }
            else
            {
                KiemTraNhap.KiemTraBatBuoc(model.DSLoi, model.HoTen, tk.VaiTro == VaiTro.SoYTe ? "Tên đơn vị" : "Họ và tên");
                //cá nhân đăng ký bằng email nên email bắt buộc
                if (tk.VaiTro == VaiTro.CaNhan)
                    KiemTraNhap.KiemTraBatBuoc(model.DSLoi, model.Email, "Email");
                KiemTraNhap.KiemTraEmail(model.DSLoi, model.Email, "Email");
                KiemTraNhap.KiemTraDoDaiTaiKhoan(model.DSLoi, model.HoTen, model.Email, model.SoDienThoai,
                    tk.VaiTro == VaiTro.SoYTe ? "Tên đơn vị" : "Họ và tên");
            }

            if (model.DSLoi.Count == 0)
            {
                string email = laDoanhNghiep ? model.DoanhNghiep.DaiDien_Email : model.Email;
                model.DSLoi.AddRange(repo.KiemTraTrungKhiSua(tk.ID, email, laDoanhNghiep ? tk.DoanhNghiepID.Value : 0, null));
                if (repo.Loi != null)
                    model.DSLoi.Add("Lỗi khi tải dữ liệu: " + repo.Loi);
            }

            bool daLuu = false;
            if (model.DSLoi.Count == 0)
            {
                daLuu = laDoanhNghiep
                    ? repo.CapNhatDoanhNghiep(tk.DoanhNghiepID.Value, model.DoanhNghiep, false)
                    : repo.CapNhatTaiKhoan(tk.ID, model.HoTen, model.Email, model.SoDienThoai, tk.VaiTro);
                if (!daLuu)
                    model.DSLoi.Add("Lỗi khi lưu dữ liệu: " + repo.Loi);
            }
            if (!daLuu)
            {
                if (model.DoanhNghiep.DSLienHe.Count == 0)
                    model.DoanhNghiep.DSLienHe.Add(new LienHeRow());
                return View(model);
            }

            //ghi lại cookie để header hiện tên mới
            TaiKhoanRow moi = repo.GetTaiKhoan(tk.ID);
            if (moi != null)
                NguoiDung.CapNhatCookie(HttpContext.GetOwinContext().Authentication, moi);
            TempData["ThongBao"] = "Đã lưu thông tin tài khoản.";
            return RedirectToAction("CuaToi");
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

            string hashMoi = MatKhau.BamMatKhau(model.MatKhauMoi);
            if (!repo.DoiMatKhau(tk.ID, hashMoi, false))
            {
                model.Loi = "Lỗi khi lưu dữ liệu: " + repo.Loi;
                return View(model);
            }
            //ghi lại cookie để bỏ cờ "phải đổi mật khẩu" (giữ lựa chọn "Lưu tài khoản" của lần đăng nhập);
            //cookie mới mang dấu của mật khẩu mới, cookie ở các thiết bị khác hết hiệu lực
            tk.MatKhauHash = hashMoi;
            tk.PhaiDoiMatKhau = false;
            NguoiDung.CapNhatCookie(HttpContext.GetOwinContext().Authentication, tk);
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
            KiemTraNhap.KiemTraDoDaiTaiKhoan(loi, m.HoTen, m.Email, m.SoDienThoai, "Họ và tên");
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
