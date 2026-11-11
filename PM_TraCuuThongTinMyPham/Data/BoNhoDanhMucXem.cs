using System;
using System.Web;
using System.Web.Caching;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //giữ dữ liệu trang Danh mục trong bộ nhớ web server, để mỗi lần mở trang không phải đọc DB
    public static class BoNhoDanhMucXem
    {
        private const string KhoaCache = "PM_TraCuu_DanhMucXem";
        //sau bấy nhiêu phút thì đọc lại DB (dữ liệu gốc có thể vừa được cập nhật)
        private const int SoPhutGiu = 10;
        //khóa để 2 lượt mở trang cùng lúc không cùng đọc DB
        private static readonly object khoaNap = new object();

        // Lấy dữ liệu 13 danh mục (đọc DB lần đầu hoặc khi đã hết hạn), trả về null nếu lỗi, thông báo lỗi ở loi
        public static DanhMucXemVM GetDuLieu(out string loi)
        {
            loi = null;
            DanhMucXemVM kq = HttpRuntime.Cache[KhoaCache] as DanhMucXemVM;
            if (kq != null)
                return kq;

            lock (khoaNap)
            {
                //kiểm tra lại: có thể lượt khác vừa nạp xong trong lúc chờ khóa
                kq = HttpRuntime.Cache[KhoaCache] as DanhMucXemVM;
                if (kq != null)
                    return kq;

                DanhMucXemRepository repo = new DanhMucXemRepository();
                kq = repo.GetDuLieuDanhMuc();
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
