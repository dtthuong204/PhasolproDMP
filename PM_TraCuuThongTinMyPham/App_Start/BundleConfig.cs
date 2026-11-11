using System.Web;
using System.Web.Optimization;

namespace PM_TraCuuThongTinMyPham
{
    public class BundleConfig
    {
        //gói script dùng chung cho mọi trang (jQuery)
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));
        }
    }
}
