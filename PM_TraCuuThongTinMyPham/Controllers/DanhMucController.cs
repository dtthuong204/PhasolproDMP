using System.Web.Mvc;
using PM_TraCuuThongTinMyPham.Data;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    //trang Danh mục (menu DANH MỤC): xem 3 danh mục Sở Y tế, 5 Annex EU, 5 Annex ASEAN; phải đăng nhập (mọi vai trò)
    public class DanhMucController : Controller
    {
        //khung trang; dữ liệu do Scripts/danhmuc.js tải riêng qua DuLieu
        public ActionResult Index()
        {
            ViewBag.ActiveTab = "danhmuc";
            ViewBag.BoCucUngDung = true;
            return View();
        }

        //toàn bộ dữ liệu 13 danh mục dạng JSON (khoảng vài MB), trình duyệt tải một lần khi mở trang
        public JsonResult DuLieu()
        {
            string loi;
            DanhMucXemVM model = BoNhoDanhMucXem.GetDuLieu(out loi);
            if (model == null)
                model = new DanhMucXemVM { Loi = "Lỗi khi tải dữ liệu: " + loi };
            return JsonLon(model);
        }

        //JSON lớn hơn giới hạn mặc định (khoảng 100 KB) của MVC
        private JsonResult JsonLon(object data)
        {
            JsonResult kq = Json(data, JsonRequestBehavior.AllowGet);
            kq.MaxJsonLength = int.MaxValue;
            return kq;
        }
    }
}
