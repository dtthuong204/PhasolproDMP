using System.Collections.Generic;

namespace PM_TraCuuThongTinMyPham.Models
{
    //dữ liệu trang Danh mục (/DanhMuc): cả 13 danh mục gửi về trình duyệt dạng JSON một lần, trình duyệt tự lọc, sắp xếp
    public class DanhMucXemVM
    {
        public string Loi { get; set; }
        //3 danh mục Sở Y tế, 5 Annex EU, 5 Annex ASEAN theo thứ tự trên thanh trái
        public List<DanhMucRow> DSDanhMuc { get; set; }
        //tham chiếu SYT -> EU, mỗi phần tử: [BangSYT, SytID, ThamChieu, KhoaEU "Bang#ID" hoặc null nếu không khớp]
        public List<string[]> DSLienKet { get; set; }
        //lời dẫn và chú thích của các Annex ASEAN (bảng ACD_Note)
        public List<ChuThichRow> DSChuThich { get; set; }

        public DanhMucXemVM()
        {
            DSDanhMuc = new List<DanhMucRow>();
            DSLienKet = new List<string[]>();
            DSChuThich = new List<ChuThichRow>();
        }
    }

    //một danh mục: tên cột lấy theo tên đặt trong câu SQL, mỗi dòng là mảng giá trị cùng thứ tự với DSCot
    public class DanhMucRow
    {
        //mã dùng ở trình duyệt: syt-cam, syt-gh, syt-cp, eu-2 … eu-6, a-2 … a-7
        public string Ma { get; set; }
        //SYT, EU hoặc ASEAN
        public string Nhom { get; set; }
        public string Bang { get; set; }
        public string Annex { get; set; }
        //tên ngắn trên thanh trái, tiêu đề lớn và tên tiếng Anh (lấy từ AnnexList) ở khung phải
        public string TenNgan { get; set; }
        public string TieuDe { get; set; }
        public string TenTiengAnh { get; set; }
        public List<string> DSCot { get; set; }
        public List<string[]> DSDong { get; set; }

        public DanhMucRow()
        {
            DSCot = new List<string>();
            DSDong = new List<string[]>();
        }
    }

    //một lời dẫn (Preamble) hoặc chú thích cuối (End) của Annex ASEAN
    public class ChuThichRow
    {
        //II, III, IV, VI, VII
        public string Annex { get; set; }
        public string So { get; set; }
        public string NoiDung { get; set; }
        public string Loai { get; set; }
    }
}
