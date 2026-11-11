using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using PM_TraCuuThongTinMyPham.Data;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Controllers
{
    //trang Tra cứu (menu TRA CỨU): server chỉ tra và trả JSON, Scripts/tracuu.js vẽ toàn bộ kết quả
    public class TraCuuController : Controller
    {
        //giới hạn để một lần tra không quá nặng
        private const int SoTenToiDa = 1000;
        private const int DoDaiTenToiDa = 500;

        private readonly TraCuuRepository repo = new TraCuuRepository();

        //khung trang: thanh trái nhập danh sách, khung phải kết quả
        public ActionResult Index()
        {
            ViewBag.ActiveTab = "tracuu";
            ViewBag.BoCucUngDung = true;
            return View();
        }

        //tra danh sách tên, trả về JSON gồm cả kết quả Chính xác và Tương đối (đổi chế độ không cần tra lại)
        //tracuu.js gửi POST; GET để thử thẳng trên trình duyệt, vd /TraCuu/TraDanhSach?dsTen=Salicylic%20acid&dsTen=Paraben
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public JsonResult TraDanhSach(List<string> dsTen)
        {
            KetQuaTraCuuV2VM model = new KetQuaTraCuuV2VM();
            List<string> dsGon = BoTenTrung(dsTen);
            if (dsGon.Count == 0)
            {
                model.Loi = "Hãy nhập ít nhất một thành phần.";
                return JsonLon(model);
            }
            if (dsGon.Count > SoTenToiDa)
            {
                model.Loi = string.Format("Mỗi lần tra tối đa {0} tên khác nhau, danh sách hiện có {1} tên.", SoTenToiDa, dsGon.Count);
                return JsonLon(model);
            }

            string loiDanhMuc;
            DanhMucTraCuu danhMuc = BoNhoTraCuu.GetDanhMuc(out loiDanhMuc);
            if (danhMuc == null)
            {
                model.Loi = "Lỗi khi tải dữ liệu: " + loiDanhMuc;
                return JsonLon(model);
            }

            model = SoKhopTen.TraCuu(dsGon, danhMuc);
            return JsonLon(model);
        }

        //nội dung hộp chi tiết dạng JSON; nguon = "ACD" hoặc "EU", phần danh mục EU của hộp đã có sẵn ở trình duyệt
        public JsonResult ChiTiet(string nguon, string bang, int? rowId)
        {
            ChiTietVM item = null;
            if (rowId.HasValue && (nguon == "ACD" || nguon == "EU"))
                item = repo.GetChiTiet(nguon, bang, rowId.Value);

            if (item == null)
            {
                item = new ChiTietVM { Nguon = nguon };
                item.Loi = String.IsNullOrEmpty(repo.Loi)
                    ? "Không tìm thấy dữ liệu của dòng này."
                    : "Lỗi khi tải dữ liệu: " + repo.Loi;
                return JsonLon(ChiTietV2VM.fromChiTiet(item));
            }

            ChiTietV2VM model = ChiTietV2VM.fromChiTiet(item);
            //EU: thêm các cột app cũ có mà view không có (EC, SCCS, IUPAC, CMR...)
            if (nguon == "EU")
            {
                repo.GetThongTinThemEU(model, bang, rowId.Value);
                if (!String.IsNullOrEmpty(repo.Loi))
                    model.Loi = "Lỗi khi tải dữ liệu: " + repo.Loi;
            }
            return JsonLon(model);
        }

        //trả JSON, cho phép GET và bỏ giới hạn 2 MB mặc định (kết quả "chứa từ khóa" có thể lớn)
        private JsonResult JsonLon(object data)
        {
            JsonResult kq = Json(data, JsonRequestBehavior.AllowGet);
            kq.MaxJsonLength = int.MaxValue;
            return kq;
        }

        //bỏ khoảng trắng thừa: cắt hai đầu, gộp nhiều khoảng trắng liền nhau thành một
        private static string LamGon(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "" : Regex.Replace(value.Trim(), @"\s+", " ");
        }

        //bỏ tên rỗng và tên trùng (không phân biệt hoa thường), giữ lần nhập đầu tiên, cắt tên quá dài
        private static List<string> BoTenTrung(List<string> dsTen)
        {
            List<string> kq = new List<string>();
            if (dsTen == null)
                return kq;

            HashSet<string> daCo = new HashSet<string>();
            foreach (string item in dsTen)
            {
                string ten = LamGon(item);
                if (ten == "")
                    continue;
                if (ten.Length > DoDaiTenToiDa)
                    ten = ten.Substring(0, DoDaiTenToiDa);
                if (daCo.Add(SoKhopTen.ChuanHoa(ten)))
                    kq.Add(ten);
            }
            return kq;
        }
    }
}
