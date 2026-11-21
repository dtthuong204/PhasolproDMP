using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Script.Serialization;

namespace PM_TraCuuThongTinMyPham.Models
{
    //kết quả tra cứu (/TraCuu), trả về dạng JSON cho Scripts/tracuu.js vẽ
    //các chất chỉ gửi một lần trong 3 từ điển Syt / EU / Asean, mỗi tên nhập chỉ giữ danh sách khóa
    public class KetQuaTraCuuV2VM
    {
        //null nếu không có lỗi
        public string Loi { get; set; }
        public List<KetQuaTenRow> DSTen { get; set; }
        //khóa "BangSYT#ID"
        public Dictionary<string, ChatSytRow> Syt { get; set; }
        //khóa "Bang#RowID"
        public Dictionary<string, DongEURow> EU { get; set; }
        //khóa "Bang#RowID"
        public Dictionary<string, DongAseanRow> Asean { get; set; }

        public KetQuaTraCuuV2VM()
        {
            DSTen = new List<KetQuaTenRow>();
            Syt = new Dictionary<string, ChatSytRow>();
            EU = new Dictionary<string, DongEURow>();
            Asean = new Dictionary<string, DongAseanRow>();
        }
    }

    //kết quả của một tên nhập (đã bỏ trùng); danh sách Tương đối có cả các chất khớp chính xác
    public class KetQuaTenRow
    {
        public string Ten { get; set; }
        //tên đã chuẩn hóa (chữ thường, gọn khoảng trắng), JS dùng để ghép với dòng trong grid
        public string Khoa { get; set; }
        public List<string> SytChinhXac { get; set; }
        public List<string> SytTuongDoi { get; set; }
        public List<string> AseanChinhXac { get; set; }
        public List<string> AseanTuongDoi { get; set; }

        public KetQuaTenRow()
        {
            SytChinhXac = new List<string>();
            SytTuongDoi = new List<string>();
            AseanChinhXac = new List<string>();
            AseanTuongDoi = new List<string>();
        }
    }

    //1 chất trong danh mục Sở Y tế (vw_SYT_TatCa) kèm các tham chiếu sang Annex EU
    public class ChatSytRow
    {
        public string Khoa { get; set; }
        public string BangSYT { get; set; }
        public int ID { get; set; }
        //Cấm / Giới hạn / Cho phép
        public string LoaiDanhMuc { get; set; }
        public string Ten { get; set; }
        public string MoTa { get; set; }
        public string ThongTin { get; set; }
        public string GhiChu { get; set; }
        public List<ThamChieuEURow> DSThamChieu { get; set; }

        //tên đã chuẩn hóa để so khớp, không gửi về trình duyệt
        [ScriptIgnore]
        public string TenGon { get; set; }

        public ChatSytRow()
        {
            DSThamChieu = new List<ThamChieuEURow>();
        }

        public static ChatSytRow fromReader(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            ChatSytRow item = new ChatSytRow
            {
                BangSYT = Convert.ToString(reader["BangSYT"]),
                ID = Convert.ToInt32(reader["ID"]),
                LoaiDanhMuc = Convert.ToString(reader["LoaiDanhMuc"]),
                Ten = Convert.ToString(reader["Ten"]).Trim(),
                MoTa = Convert.ToString(reader["MoTa"]),
                ThongTin = Convert.ToString(reader["ThongTin"]),
                GhiChu = Convert.ToString(reader["GhiChu"])
            };
            item.Khoa = item.BangSYT + "#" + item.ID;
            return item;
        }
    }

    //1 tham chiếu trong cột MoTa của SYT, vd "III/98"; KhoaEU = null nếu không tìm thấy dòng Annex EU tương ứng
    public class ThamChieuEURow
    {
        public string ThamChieu { get; set; }
        public string KhoaEU { get; set; }
    }

    //1 dòng Annex EU (vw_EU_TatCa), chỉ các cột cần cho bảng; nội dung đầy đủ lấy qua ChiTietV2
    public class DongEURow
    {
        public string Khoa { get; set; }
        public string Bang { get; set; }
        public int RowID { get; set; }
        public string Annex { get; set; }
        public string RefNo { get; set; }
        public string TenChat { get; set; }
        public string TenINCI { get; set; }
        public string CAS { get; set; }
        public string EC { get; set; }
        //dòng EU đã bị xóa/chuyển (TenChat ghi "moved or deleted")
        public bool DaXoa { get; set; }

        //các cột tham khảo, chỉ có ở dòng Annex II (ghép vào bảng Cấm như app cũ), dòng khác để null
        public string Regulation { get; set; }
        public string ChiThiKhac { get; set; }
        public string SCCS { get; set; }
        public string IUPAC { get; set; }
        public string ThanhPhanNhanDien { get; set; }

        public static DongEURow fromReader(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            DongEURow item = new DongEURow
            {
                Bang = Convert.ToString(reader["Bang"]),
                RowID = Convert.ToInt32(reader["RowID"]),
                Annex = Convert.ToString(reader["Annex"]),
                RefNo = Convert.ToString(reader["RefNo"]),
                TenChat = Convert.ToString(reader["TenChat"]),
                TenINCI = Convert.ToString(reader["TenINCI"]),
                CAS = Convert.ToString(reader["CAS"]),
                EC = Convert.ToString(reader["EC"]),
                DaXoa = Convert.ToInt32(reader["DaXoa"]) == 1
            };
            item.Khoa = item.Bang + "#" + item.RowID;
            return item;
        }
    }

    //1 dòng Annex ASEAN đang hiệu lực (vw_ACD_TatCa, đã bỏ dòng "Đã loại bỏ")
    public class DongAseanRow
    {
        public string Khoa { get; set; }
        public string Bang { get; set; }
        public int RowID { get; set; }
        public string Annex { get; set; }
        //Annex IV: RefNo là Colour Index
        public string RefNo { get; set; }
        public string TenChat { get; set; }
        public string CAS { get; set; }
        public string GioiHanToiDa { get; set; }
        public string GhiChu { get; set; }
        public string TrangThaiHieuLuc { get; set; }

        [ScriptIgnore]
        public string TenGon { get; set; }

        public static DongAseanRow fromReader(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            DongAseanRow item = new DongAseanRow
            {
                Bang = Convert.ToString(reader["Bang"]),
                RowID = Convert.ToInt32(reader["RowID"]),
                Annex = Convert.ToString(reader["Annex"]),
                RefNo = Convert.ToString(reader["RefNo"]),
                TenChat = Convert.ToString(reader["TenChat"]),
                CAS = Convert.ToString(reader["CAS"]),
                GioiHanToiDa = Convert.ToString(reader["GioiHanToiDa"]),
                GhiChu = Convert.ToString(reader["GhiChu"]),
                TrangThaiHieuLuc = Convert.ToString(reader["TrangThaiHieuLuc"])
            };
            item.Khoa = item.Bang + "#" + item.RowID;
            return item;
        }
    }

    //toàn bộ danh mục cho trang mới, đọc DB một lần rồi giữ trong bộ nhớ (xem Data/BoNhoTraCuu.cs)
    public class DanhMucTraCuu
    {
        public List<ChatSytRow> DSSyt { get; set; }
        //tra nhanh SYT theo tên đã chuẩn hóa (một tên có thể nằm ở nhiều danh mục)
        public Dictionary<string, List<ChatSytRow>> SytTheoTen { get; set; }
        public Dictionary<string, DongEURow> EU { get; set; }
        public List<DongAseanRow> DSAsean { get; set; }

        public DanhMucTraCuu()
        {
            DSSyt = new List<ChatSytRow>();
            SytTheoTen = new Dictionary<string, List<ChatSytRow>>();
            EU = new Dictionary<string, DongEURow>();
            DSAsean = new List<DongAseanRow>();
        }
    }

    //nội dung hộp chi tiết dạng JSON (ngày đã đổi sang chuỗi dd/MM/yyyy)
    public class ChiTietV2VM
    {
        public string Loi { get; set; }
        public string Nguon { get; set; }
        public string Annex { get; set; }
        public string RefNo { get; set; }
        public string TenChat { get; set; }
        public string TenINCI { get; set; }
        public string CAS { get; set; }
        public string PhamViSuDung { get; set; }
        public string GioiHanToiDa { get; set; }
        public string DieuKienKhac { get; set; }
        public string CanhBaoNhan { get; set; }
        public string GhiChu { get; set; }
        public string CanCu { get; set; }
        public string NgayApDung { get; set; }
        public string NgayHetHieuLuc { get; set; }
        public string TrangThaiHieuLuc { get; set; }
        public string Regulation { get; set; }
        public string NgayCapNhat { get; set; }
        //các cột chỉ có ở bảng gốc Annex EU (view không có), điền bằng TraCuuRepository.GetThongTinThemEU
        public string EC { get; set; }
        //Annex IV: Colour index và màu
        public string ColourIndex { get; set; }
        public string MauSac { get; set; }
        public string ChiThiKhac { get; set; }
        public string SCCS { get; set; }
        public string IUPAC { get; set; }
        public string ThanhPhanNhanDien { get; set; }
        public string CMR { get; set; }

        public static ChiTietV2VM fromChiTiet(ChiTietVM item)
        {
            if (item == null)
                return null;
            return new ChiTietV2VM
            {
                Loi = item.Loi,
                Nguon = item.Nguon,
                Annex = item.Annex,
                RefNo = item.RefNo,
                TenChat = item.TenChat,
                TenINCI = item.TenINCI,
                CAS = item.CAS,
                PhamViSuDung = item.PhamViSuDung,
                GioiHanToiDa = item.GioiHanToiDa,
                DieuKienKhac = item.DieuKienKhac,
                CanhBaoNhan = item.CanhBaoNhan,
                GhiChu = item.GhiChu,
                CanCu = item.CanCu,
                NgayApDung = DinhDangNgay(item.NgayApDung),
                NgayHetHieuLuc = DinhDangNgay(item.NgayHetHieuLuc),
                TrangThaiHieuLuc = item.TrangThaiHieuLuc,
                Regulation = item.Regulation,
                NgayCapNhat = DinhDangNgay(item.NgayCapNhat)
            };
        }

        private static string DinhDangNgay(DateTime? ngay)
        {
            return ngay.HasValue ? ngay.Value.ToString("dd/MM/yyyy") : null;
        }
    }
}
