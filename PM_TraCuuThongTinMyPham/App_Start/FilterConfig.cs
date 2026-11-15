using System.Web;
using System.Web.Mvc;
using PM_TraCuuThongTinMyPham.Controllers;

namespace PM_TraCuuThongTinMyPham
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            //mọi trang đều phải đăng nhập, trừ action gắn [AllowAnonymous] (trang chủ giới thiệu, đăng nhập, đăng ký)
            filters.Add(new PhanQuyenAttribute());
        }
    }
}
