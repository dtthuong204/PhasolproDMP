using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace PM_TraCuuThongTinMyPham
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //không gửi header X-AspNetMvc-Version (lộ phiên bản MVC cho người ngoài)
            MvcHandler.DisableMvcResponseHeader = true;
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //mã chống giả mạo form (AntiForgeryToken) gắn theo ID tài khoản trong cookie đăng nhập
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
        }
    }
}
