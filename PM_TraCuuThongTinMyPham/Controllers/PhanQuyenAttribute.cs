using System.Web.Mvc;
using System.Web.Routing;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    //kiểm tra đăng nhập + vai trò trước mỗi action
    //  - gắn chung cho mọi trang (FilterConfig): phải đăng nhập, trừ action có [AllowAnonymous]
    //  - gắn trên controller/action với Roles: [PhanQuyen(Roles = VaiTro.Admin)] chỉ cho vai trò đó
    //  - đã đăng nhập nhưng sai vai trò: chuyển tới trang "Không có quyền" (không bắt đăng nhập lại)
    //  - đang dùng mật khẩu tạm: chỉ cho vào trang Đổi mật khẩu và Đăng xuất
    public class PhanQuyenAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
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
