using System.Web.Mvc;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    public class HomeController : Controller
    {
        //trang chủ: chưa đăng nhập thì hiện trang giới thiệu + ô đăng nhập;
        //đã đăng nhập thì hiện tiêu đề + 2 lối vào Danh mục, Tra cứu
        [AllowAnonymous]
        public ActionResult Index()
        {
            ViewBag.Title = "Trang chủ";
            ViewBag.ActiveTab = "";
            ViewBag.ThongBao = TempData["ThongBao"] as string;
            if (!Request.IsAuthenticated)
                return View("GioiThieu", new DangNhapVM());
            return View();
        }

        //trang "Không tìm thấy" (404): Web.config (customErrors) chuyển tới đây khi địa chỉ không có trong web
        [AllowAnonymous]
        public ActionResult KhongTimThay()
        {
            ViewBag.Title = "Không tìm thấy trang";
            ViewBag.ActiveTab = "";
            Response.StatusCode = 404;
            //không để IIS thay bằng trang 404 mặc định của nó
            Response.TrySkipIisCustomErrors = true;
            return View();
        }
    }
}
