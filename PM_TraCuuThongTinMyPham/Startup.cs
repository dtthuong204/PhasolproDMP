using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PM_TraCuuThongTinMyPham.Startup))]
namespace PM_TraCuuThongTinMyPham
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
