using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //đọc toàn bộ danh mục cho trang Tra cứu (/TraCuu); việc so khớp tên làm trong bộ nhớ (Data/SoKhopTen.cs)
    public class DanhMucRepository
    {
        //thông báo lỗi của lần gọi gần nhất, null nếu không lỗi
        public string Loi { get; set; }

        #region Danh mục tra cứu
        //5 câu SELECT trong một lệnh, đọc lần lượt bằng reader.NextResult();
        //câu cuối đọc thẳng bảng EU_AnnexII vì vw_EU_TatCa không có các cột tham khảo (ghép vào bảng Cấm như app cũ)
        private const string SqlDanhMuc = @"
SELECT BangSYT, ID, LoaiDanhMuc, Ten, MoTa, ThongTin, GhiChu
FROM dbo.vw_SYT_TatCa
WHERE Ten IS NOT NULL AND LTRIM(RTRIM(Ten)) <> N'';

SELECT BangSYT, SytID, ThamChieuGoc, EUBang, EURowID
FROM dbo.SYT_EU_LienKet
ORDER BY BangSYT, SytID, ID;

SELECT Bang, RowID, Annex, RefNo, TenChat, TenINCI, CAS, EC, DaXoa
FROM dbo.vw_EU_TatCa;

SELECT Bang, RowID, Annex, RefNo, TenChat, CAS, GioiHanToiDa, GhiChu, TrangThaiHieuLuc
FROM dbo.vw_ACD_TatCa
WHERE TrangThaiHieuLuc <> N'Đã loại bỏ' AND TenChat IS NOT NULL AND LTRIM(RTRIM(TenChat)) <> N''
ORDER BY Annex, RowID;

SELECT ID, [Regulation] AS Regulation, [Other Directives/Regulations] AS ChiThiKhac, [SCCS opinions] AS SCCS,
       [Chemical/IUPAC Name] AS IUPAC, [Identified INGREDIENTS or substances e.g.] AS ThanhPhanNhanDien
FROM dbo.EU_AnnexII;";

        // Lấy toàn bộ danh mục SYT (kèm tham chiếu EU), Annex EU và Annex ASEAN đang hiệu lực
        public DanhMucTraCuu GetDanhMucTraCuu()
        {
            Loi = null;
            DanhMucTraCuu kq = new DanhMucTraCuu();
            try
            {
                Dictionary<string, ChatSytRow> sytTheoKhoa = new Dictionary<string, ChatSytRow>();
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(SqlDanhMuc, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ChatSytRow item = ChatSytRow.fromReader(reader);
                            item.TenGon = SoKhopTen.ChuanHoa(item.Ten);
                            kq.DSSyt.Add(item);
                            sytTheoKhoa[item.Khoa] = item;
                        }

                        //tham chiếu SYT -> EU, giữ đúng thứ tự trong cột MoTa
                        reader.NextResult();
                        while (reader.Read())
                        {
                            string khoaSyt = Convert.ToString(reader["BangSYT"]) + "#" + Convert.ToInt32(reader["SytID"]);
                            ChatSytRow chat;
                            if (!sytTheoKhoa.TryGetValue(khoaSyt, out chat))
                                continue;
                            ThamChieuEURow thamChieu = new ThamChieuEURow
                            {
                                ThamChieu = Convert.ToString(reader["ThamChieuGoc"]).Trim()
                            };
                            if (reader["EURowID"] != DBNull.Value)
                                thamChieu.KhoaEU = Convert.ToString(reader["EUBang"]) + "#" + Convert.ToInt32(reader["EURowID"]);
                            chat.DSThamChieu.Add(thamChieu);
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            DongEURow item = DongEURow.fromReader(reader);
                            kq.EU[item.Khoa] = item;
                        }

                        reader.NextResult();
                        while (reader.Read())
                        {
                            DongAseanRow item = DongAseanRow.fromReader(reader);
                            item.TenGon = SoKhopTen.ChuanHoa(item.TenChat);
                            kq.DSAsean.Add(item);
                        }

                        //cột tham khảo của Annex EU II
                        reader.NextResult();
                        while (reader.Read())
                        {
                            DongEURow dongEU;
                            if (!kq.EU.TryGetValue("EU_AnnexII#" + Convert.ToInt32(reader["ID"]), out dongEU))
                                continue;
                            dongEU.Regulation = Convert.ToString(reader["Regulation"]);
                            dongEU.ChiThiKhac = Convert.ToString(reader["ChiThiKhac"]);
                            dongEU.SCCS = Convert.ToString(reader["SCCS"]);
                            dongEU.IUPAC = Convert.ToString(reader["IUPAC"]);
                            dongEU.ThanhPhanNhanDien = Convert.ToString(reader["ThanhPhanNhanDien"]);
                        }
                    }
                }

                //tham chiếu trỏ tới dòng EU không còn (đã xóa/chuyển) thì coi như không khớp
                foreach (ChatSytRow chat in kq.DSSyt)
                {
                    foreach (ThamChieuEURow thamChieu in chat.DSThamChieu)
                    {
                        DongEURow dongEU;
                        if (thamChieu.KhoaEU != null && (!kq.EU.TryGetValue(thamChieu.KhoaEU, out dongEU) || dongEU.DaXoa))
                            thamChieu.KhoaEU = null;
                    }

                    List<ChatSytRow> ds;
                    if (!kq.SytTheoTen.TryGetValue(chat.TenGon, out ds))
                    {
                        ds = new List<ChatSytRow>();
                        kq.SytTheoTen.Add(chat.TenGon, ds);
                    }
                    ds.Add(chat);
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                kq = new DanhMucTraCuu();
            }
            return kq;
        }
        #endregion
    }
}
