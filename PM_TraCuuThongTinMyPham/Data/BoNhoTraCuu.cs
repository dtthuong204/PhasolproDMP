using System;
using System.Web;
using System.Web.Caching;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //giữ danh mục của trang tra cứu mới trong bộ nhớ web server, để mỗi lần tra không phải đọc DB
    public static class BoNhoTraCuu
    {
        private const string KhoaCache = "PM_TraCuu_DanhMucTraCuuV2";
        //sau bấy nhiêu phút thì đọc lại DB (dữ liệu gốc có thể vừa được cập nhật)
        private const int SoPhutGiu = 10;
        //khóa để 2 lượt tra cùng lúc không cùng đọc DB
        private static readonly object khoaNap = new object();

        // Lấy danh mục (đọc DB lần đầu hoặc khi đã hết hạn), trả về null nếu lỗi, thông báo lỗi ở loi
        public static DanhMucTraCuu GetDanhMuc(out string loi)
        {
            loi = null;
            DanhMucTraCuu kq = HttpRuntime.Cache[KhoaCache] as DanhMucTraCuu;
            if (kq != null)
                return kq;

            lock (khoaNap)
            {
                //kiểm tra lại: có thể lượt khác vừa nạp xong trong lúc chờ khóa
                kq = HttpRuntime.Cache[KhoaCache] as DanhMucTraCuu;
                if (kq != null)
                    return kq;

                DanhMucRepository repo = new DanhMucRepository();
                kq = repo.GetDanhMucTraCuu();
                if (!String.IsNullOrEmpty(repo.Loi))
                {
                    loi = repo.Loi;
                    return null;
                }

                HttpRuntime.Cache.Insert(KhoaCache, kq, null, DateTime.UtcNow.AddMinutes(SoPhutGiu), Cache.NoSlidingExpiration);
                return kq;
            }
        }
    }
}
