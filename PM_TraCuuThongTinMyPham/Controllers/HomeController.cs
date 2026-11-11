using System.Web.Mvc;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    public class HomeController : Controller
    {
        //trang chủ: tiêu đề + 2 lối vào Danh mục, Tra cứu
        public ActionResult Index()
        {
            ViewBag.Title = "Trang chủ";
            ViewBag.ActiveTab = "";
            return View();
        }
    }
}
