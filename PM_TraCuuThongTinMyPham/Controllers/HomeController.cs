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
    }
}
