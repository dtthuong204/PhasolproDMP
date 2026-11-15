using System;
using System.Web;
using System.Web.Caching;

namespace PM_TraCuuThongTinMyPham.Data
{
    //chống dò mật khẩu ở trang Đăng nhập: đếm số lần đăng nhập sai trong bộ nhớ web server (không cần thêm bảng)
    //  - cùng một tên đăng nhập từ cùng một địa chỉ IP: sai 5 lần thì phải chờ hết 15 phút kể từ lần sai đầu
    //  - cùng một địa chỉ IP (thử nhiều tên khác nhau): sai 30 lần thì phải chờ như trên
    //đăng nhập đúng thì xóa bộ đếm của tên đó; khởi động lại web thì các bộ đếm về 0
    public static class ChongDoMatKhau
    {
        private const int SoLanToiDa = 5;
        private const int SoLanToiDaTheoIP = 30;
        private const int SoPhutKhoa = 15;
        private const string TienTo = "PM_TraCuu_DangNhapSai_";
        private static readonly object khoa = new object();

        private class BoDem
        {
            public int SoLan;
            public DateTime HetHan;
        }

        // Số phút còn phải chờ trước khi được thử đăng nhập tiếp, 0 nếu chưa bị chặn
        public static int SoPhutPhaiCho(string tenDangNhap, string ip)
        {
            lock (khoa)
            {
                return Math.Max(PhutCho(KhoaTen(tenDangNhap, ip), SoLanToiDa), PhutCho(KhoaIP(ip), SoLanToiDaTheoIP));
            }
        }

        // Ghi thêm một lần đăng nhập sai (kể cả khi tên đăng nhập không tồn tại)
        public static void GhiLanSai(string tenDangNhap, string ip)
        {
            lock (khoa)
            {
                Tang(KhoaTen(tenDangNhap, ip));
                Tang(KhoaIP(ip));
            }
        }

        // Đăng nhập đúng: xóa bộ đếm của tên đăng nhập này (bộ đếm theo IP giữ nguyên)
        public static void XoaLanSai(string tenDangNhap, string ip)
        {
            lock (khoa)
            {
                HttpRuntime.Cache.Remove(KhoaTen(tenDangNhap, ip));
            }
        }

        private static string KhoaTen(string tenDangNhap, string ip)
        {
            return TienTo + "Ten_" + (tenDangNhap ?? "").Trim().ToLowerInvariant() + "|" + (ip ?? "");
        }

        private static string KhoaIP(string ip)
        {
            return TienTo + "IP_" + (ip ?? "");
        }

        private static int PhutCho(string khoaDem, int soLanToiDa)
        {
            BoDem dem = HttpRuntime.Cache[khoaDem] as BoDem;
            if (dem == null || dem.SoLan < soLanToiDa)
                return 0;
            return Math.Max(1, (int)Math.Ceiling((dem.HetHan - DateTime.UtcNow).TotalMinutes));
        }

        private static void Tang(string khoaDem)
        {
            BoDem dem = HttpRuntime.Cache[khoaDem] as BoDem;
            if (dem == null)
            {
                //bộ đếm tự mất sau SoPhutKhoa phút kể từ lần sai đầu tiên
                dem = new BoDem { HetHan = DateTime.UtcNow.AddMinutes(SoPhutKhoa) };
                HttpRuntime.Cache.Insert(khoaDem, dem, null, dem.HetHan, Cache.NoSlidingExpiration);
            }
            dem.SoLan++;
        }
    }
}
