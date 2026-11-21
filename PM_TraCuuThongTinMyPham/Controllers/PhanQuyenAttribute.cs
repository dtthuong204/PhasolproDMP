using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Owin.Security;
using PM_TraCuuThongTinMyPham.Data;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    //kiểm tra đăng nhập + vai trò trước mỗi action
    //  - gắn chung cho mọi trang (FilterConfig): phải đăng nhập, trừ action có [AllowAnonymous]
    //  - gắn trên controller/action với Roles: [PhanQuyen(Roles = VaiTro.Admin)] chỉ cho vai trò đó
    //  - đã đăng nhập nhưng sai vai trò: chuyển tới trang "Không có quyền" (không bắt đăng nhập lại)
    //  - đang dùng mật khẩu tạm: chỉ cho vào trang Đổi mật khẩu và Đăng xuất
    //  - mỗi yêu cầu đối chiếu cookie với database (DoiChieuTaiKhoan): khóa tài khoản, đổi vai trò, đổi mật khẩu có hiệu lực ngay
    public class PhanQuyenAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            KiemTraQuyen(filterContext);

            //trang đang mở tải dữ liệu bằng JavaScript (Danh mục, Tra cứu) mà bị chặn (hết phiên, bị khóa, phải đổi mật khẩu...):
            //trả mã 403 thay cho lệnh chuyển trang, vì JavaScript không tự chuyển trang được;
            //đoạn script trong _Layout.cshtml thấy mã 403 thì đưa người dùng về trang đăng nhập
            if (filterContext.Result != null && !filterContext.IsChildAction && filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                filterContext.Result = new HttpStatusCodeResult(403);
            }
        }

        private void KiemTraQuyen(AuthorizationContext filterContext)
        {
            if (!filterContext.IsChildAction && !DoiChieuTaiKhoan(filterContext))
                return;

            base.OnAuthorization(filterContext);
            if (filterContext.Result != null || filterContext.IsChildAction)
                return;

            if (NguoiDung.PhaiDoiMatKhau(filterContext.HttpContext.User))
            {
                string controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                string action = filterContext.ActionDescriptor.ActionName;
                bool duocVao = controller == "TaiKhoan" && (action == "DoiMatKhau" || action == "DangXuat");
                if (!duocVao)
                    filterContext.Result = ChuyenToi("DoiMatKhau");
            }
        }

        // Cookie đăng nhập chỉ là bản chép lúc đăng nhập (giữ tới 14 ngày), nên mỗi yêu cầu đọc lại tài khoản trong database:
        //  - tài khoản đã bị khóa / xóa, hoặc mật khẩu đã đổi ở nơi khác: đăng xuất, chuyển tới trang đăng nhập (trả về false)
        //  - vai trò, tên, cờ "phải đổi mật khẩu" đã khác cookie: ghi lại cookie và dùng thông tin mới ngay trong yêu cầu này
        private static bool DoiChieuTaiKhoan(AuthorizationContext filterContext)
        {
            HttpContextBase http = filterContext.HttpContext;
            if (http.User == null || !http.User.Identity.IsAuthenticated)
                return true;

            TaiKhoanRepository repo = new TaiKhoanRepository();
            TaiKhoanRow tk = repo.GetTaiKhoan(NguoiDung.LayID(http.User));
            //không đọc được database: giữ nguyên theo cookie, trang sẽ tự báo lỗi tải dữ liệu
            if (tk == null && repo.Loi != null)
                return true;

            IAuthenticationManager xacThuc = http.GetOwinContext().Authentication;
            if (!NguoiDung.ConHieuLuc(http.User, tk))
            {
                xacThuc.SignOut(NguoiDung.LoaiXacThuc);
                filterContext.Controller.TempData["ThongBao"] = "Phiên đăng nhập đã hết hiệu lực. Hãy đăng nhập lại.";
                filterContext.Result = ChuyenToi("DangNhap");
                return false;
            }

            if (NguoiDung.CanGhiLaiCookie(http.User, tk))
            {
                NguoiDung.CapNhatCookie(xacThuc, tk);
                ClaimsPrincipal moi = new ClaimsPrincipal(NguoiDung.TaoDanhTinh(tk));
                http.User = moi;
                Thread.CurrentPrincipal = moi;
            }
            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
                filterContext.Result = ChuyenToi("KhongCoQuyen");
            else
                //trả mã 401: cookie OWIN tự chuyển sang trang đăng nhập
                base.HandleUnauthorizedRequest(filterContext);
        }

        private static RedirectToRouteResult ChuyenToi(string action)
        {
            return new RedirectToRouteResult(new RouteValueDictionary(new { controller = "TaiKhoan", action = action }));
        }
    }
}
