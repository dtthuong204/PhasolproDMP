using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //đọc toàn bộ dữ liệu cho trang Danh mục (/DanhMuc), chỉ đọc; đọc thẳng bảng gốc vì các view thiếu cột (SCCS, CMR, Field 1-4…)
    public class DanhMucXemRepository
    {
        //thông báo lỗi của lần gọi gần nhất, null nếu không lỗi
        public string Loi { get; set; }

        #region Câu SQL
        //phần cuối chung của các Annex ASEAN: căn cứ, ngày (dd/MM/yyyy) và trạng thái hiệu lực tính sẵn trong vw_ACD_TatCa
        private const string SqlCuoiAsean = @",
       a.CanCu, DATE_FORMAT(a.NgayApDung, '%d/%m/%Y') AS NgayApDung,
       DATE_FORMAT(a.NgayHetHieuLuc, '%d/%m/%Y') AS NgayHetHieuLuc, a.GhiChu, v.TrangThaiHieuLuc
FROM `{0}` AS a
JOIN vw_ACD_TatCa AS v ON v.Bang = '{0}' AND v.RowID = a.ID
ORDER BY a.ID;";

        //các cột tham khảo chung của 5 Annex EU
        private const string SqlCuoiEU = @",
       `Regulation` AS Regulation, `Other Directives/Regulations` AS ChiThiKhac, `SCCS opinions` AS SCCS,
       `Chemical/IUPAC Name` AS IUPAC, `Identified INGREDIENTS or substances e.g.` AS ThanhPhanNhanDien,
       `CMR` AS CMR, DATE_FORMAT(`Update date`, '%d/%m/%Y') AS NgayCapNhat
FROM `{0}`
ORDER BY ID;";

        //cột giới hạn của Annex EU III, IV, V, VI
        private const string SqlGioiHanEU = @",
       `Product Type, body parts` AS PhamVi, `Maximum concentration in ready for use preparation` AS GioiHan,
       `Other` AS Khac, `Wording of conditions of use and warnings` AS CanhBao";

        private const string SqlSyt = @"
SELECT ID, Ten, MoTa, ThongTin, GhiChu, {1} AS TrangThaiDuLieu
FROM `{0}`
WHERE Ten IS NOT NULL AND TRIM(Ten) <> ''
ORDER BY Ten, ID;";

        //3 kết quả đầu: tên Annex, chú thích ASEAN, liên kết SYT -> EU (bỏ dòng EU đã xóa/chuyển)
        private const string SqlPhuTro = @"
SELECT Source, REPLACE(Annex, 'Annex ', '') AS Annex, Name, TenTV
FROM AnnexList;

SELECT REPLACE(Annex, 'Annex ', '') AS Annex, Number, Notes, Classification
FROM ACD_Note
WHERE Annex IS NOT NULL AND Notes IS NOT NULL
ORDER BY ID;

SELECT l.BangSYT, l.SytID, l.ThamChieuGoc,
       CASE WHEN e.RowID IS NULL THEN NULL ELSE CONCAT(l.EUBang, '#', l.EURowID) END AS KhoaEU
FROM SYT_EU_LienKet AS l
LEFT JOIN vw_EU_TatCa AS e ON e.Bang = l.EUBang AND e.RowID = l.EURowID AND e.DaXoa = 0
ORDER BY l.BangSYT, l.SytID, l.ID;";
        #endregion

        #region Định nghĩa 13 danh mục
        //mỗi danh mục: mã, nhóm, bảng, Annex, tên ngắn, tiêu đề (SYT; Annex lấy tên tiếng Việt từ AnnexList), câu SQL
        private class DinhNghia
        {
            public string Ma { get; set; }
            public string Nhom { get; set; }
            public string Bang { get; set; }
            public string Annex { get; set; }
            public string TenNgan { get; set; }
            public string TieuDe { get; set; }
            public string Sql { get; set; }
        }

        private static readonly List<DinhNghia> DSDinhNghia = TaoDSDinhNghia();

        //câu SQL ghép từ hằng số trong code, không có dữ liệu người dùng
        private static List<DinhNghia> TaoDSDinhNghia()
        {
            List<DinhNghia> ds = new List<DinhNghia>();
            //tên hiển thị theo app cũ: "Cấm sử dụng (EU)", "Giới hạn (EU)", "Cho phép (EU)" (không ghi Sở Y tế)
            ds.Add(TaoSyt("syt-cam", "SYT_DanhMucCam", "Cấm sử dụng (EU)", "Danh mục cấm sử dụng (EU)", "IFNULL(`Data`, 'Gốc')"));
            ds.Add(TaoSyt("syt-gh", "SYT_DanhMucGioiHan", "Giới hạn (EU)", "Danh mục giới hạn (EU)", "IFNULL(`Data`, 'Gốc')"));
            ds.Add(TaoSyt("syt-cp", "SYT_DanhMucChoPhep", "Cho phép (EU)", "Danh mục cho phép (EU)", "'Gốc'"));

            ds.Add(TaoEU("eu-2", "II", "Chất cấm",
                "`Reference Number` AS RefNo, `Chemical name / INN` AS TenChat, `CAS Number` AS CAS, `EC Number` AS EC"));
            ds.Add(TaoEU("eu-3", "III", "Chất giới hạn",
                "`Reference Number` AS RefNo, `Chemical name / INN` AS TenChat, `Name of Common Ingredients Glossary` AS TenINCI, `CAS Number` AS CAS, `EC Number` AS EC" + SqlGioiHanEU));
            ds.Add(TaoEU("eu-4", "IV", "Phẩm màu",
                "`Reference Number` AS RefNo, `Chemical name` AS TenChat, `Colour index Number / Name of Common Ingredients Glossary` AS TenINCI, `CAS Number` AS CAS, `EC Number` AS EC, `Color` AS Mau" + SqlGioiHanEU));
            ds.Add(TaoEU("eu-5", "V", "Chất bảo quản",
                "`Reference Number` AS RefNo, `Chemical name / INN` AS TenChat, `Name of Common Ingredients Glossary` AS TenINCI, `CAS Number` AS CAS, `EC Number` AS EC" + SqlGioiHanEU));
            ds.Add(TaoEU("eu-6", "VI", "Chất lọc UV",
                "`Reference Number` AS RefNo, `Chemical name / INN / XAN` AS TenChat, `Name of Common Ingredients Glossary` AS TenINCI, `CAS Number` AS CAS, `EC Number` AS EC" + SqlGioiHanEU));

            ds.Add(TaoAsean("a-2", "II", "Chất cấm",
                "a.RefNo, a.Substances AS TenChat, a.CASNumber AS CAS"));
            ds.Add(TaoAsean("a-3", "III", "Chất giới hạn",
                "a.RefNo, a.Substances AS TenChat, a.FieldOfApplication AS PhamVi, a.Maximum AS GioiHan, a.Other AS Khac, a.ConditionsWarning AS CanhBao"));
            ds.Add(TaoAsean("a-4", "IV", "Phẩm màu",
                "a.ColourIndex AS RefNo, a.Colour AS Mau, a.Field1 AS F1, a.Field2 AS F2, a.Field3 AS F3, a.Field4 AS F4, a.Other AS Khac"));
            ds.Add(TaoAsean("a-6", "VI", "Chất bảo quản",
                "a.RefNo, a.Substances AS TenChat, a.Maximum AS GioiHan, a.Limitations AS Khac, a.ConditionsWarnings AS CanhBao"));
            ds.Add(TaoAsean("a-7", "VII", "Chất lọc UV",
                "a.RefNo, a.Substances AS TenChat, a.Maximum AS GioiHan, a.Limitations AS Khac, a.ConditionsWarnings AS CanhBao"));
            return ds;
        }

        private static DinhNghia TaoSyt(string ma, string bang, string tenNgan, string tieuDe, string trangThai)
        {
            return new DinhNghia
            {
                Ma = ma, Nhom = "SYT", Bang = bang, TenNgan = tenNgan, TieuDe = tieuDe,
                Sql = string.Format(SqlSyt, bang, trangThai)
            };
        }

        private static DinhNghia TaoEU(string ma, string annex, string tenNgan, string cot)
        {
            string bang = "EU_Annex" + annex;
            return new DinhNghia
            {
                Ma = ma, Nhom = "EU", Bang = bang, Annex = annex, TenNgan = tenNgan,
                Sql = "\r\nSELECT ID, " + cot + string.Format(SqlCuoiEU, bang)
            };
        }

        private static DinhNghia TaoAsean(string ma, string annex, string tenNgan, string cot)
        {
            string bang = "ACD_Annex" + annex;
            return new DinhNghia
            {
                Ma = ma, Nhom = "ASEAN", Bang = bang, Annex = annex, TenNgan = tenNgan,
                Sql = "\r\nSELECT a.ID, " + cot + string.Format(SqlCuoiAsean, bang)
            };
        }
        #endregion

        #region Đọc dữ liệu
        // Lấy cả 13 danh mục, liên kết SYT -> EU và chú thích ASEAN bằng một lệnh SQL
        public DanhMucXemVM GetDuLieuDanhMuc()
        {
            Loi = null;
            DanhMucXemVM kq = new DanhMucXemVM();
            try
            {
                StringBuilder sql = new StringBuilder(SqlPhuTro);
                foreach (DinhNghia dn in DSDinhNghia)
                    sql.Append(dn.Sql);

                //tên Annex: khóa "EU#III", "ACD#III" -> [tên tiếng Anh, tên tiếng Việt]
                Dictionary<string, string[]> tenAnnex = new Dictionary<string, string[]>();
                using (MySqlConnection conn = KetNoiDB.TaoKetNoi())
                using (MySqlCommand cmd = new MySqlCommand(sql.ToString(), conn))
                {
                    KetNoiDB.MoKetNoi(conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            tenAnnex[DocChuoi(reader, 0) + "#" + DocChuoi(reader, 1)] = new string[] { DocChuoi(reader, 2), DocChuoi(reader, 3) };

                        reader.NextResult();
                        while (reader.Read())
                        {
                            kq.DSChuThich.Add(new ChuThichRow
                            {
                                Annex = DocChuoi(reader, 0),
                                So = DocChuoi(reader, 1),
                                NoiDung = DocChuoi(reader, 2),
                                Loai = DocChuoi(reader, 3)
                            });
                        }

                        reader.NextResult();
                        while (reader.Read())
                            kq.DSLienKet.Add(new string[] { DocChuoi(reader, 0), DocChuoi(reader, 1), DocChuoi(reader, 2), DocChuoi(reader, 3) });

                        foreach (DinhNghia dn in DSDinhNghia)
                        {
                            reader.NextResult();
                            kq.DSDanhMuc.Add(DocDanhMuc(reader, dn, tenAnnex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = KetNoiDB.ThongBaoLoi(ex);
                kq = new DanhMucXemVM();
            }
            return kq;
        }

        //đọc một kết quả thành danh mục: tên cột theo tên đặt trong SQL, giá trị chuyển hết sang chuỗi
        private static DanhMucRow DocDanhMuc(MySqlDataReader reader, DinhNghia dn, Dictionary<string, string[]> tenAnnex)
        {
            DanhMucRow dm = new DanhMucRow
            {
                Ma = dn.Ma,
                Nhom = dn.Nhom,
                Bang = dn.Bang,
                Annex = dn.Annex,
                TenNgan = dn.TenNgan,
                TieuDe = dn.TieuDe
            };

            if (dn.Annex != null)
            {
                string[] ten;
                string nguon = (dn.Nhom == "EU") ? "EU" : "ACD";
                dm.TieuDe = "Annex " + dn.Annex;
                if (tenAnnex.TryGetValue(nguon + "#Annex " + dn.Annex, out ten) || tenAnnex.TryGetValue(nguon + "#" + dn.Annex, out ten))
                {
                    dm.TenTiengAnh = ten[0];
                    if (!String.IsNullOrEmpty(ten[1]))
                        dm.TieuDe = "Annex " + dn.Annex + " - " + ten[1];
                }
            }

            for (int i = 0; i < reader.FieldCount; i++)
                dm.DSCot.Add(reader.GetName(i));
            while (reader.Read())
            {
                string[] dong = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                    dong[i] = DocChuoi(reader, i);
                dm.DSDong.Add(dong);
            }
            return dm;
        }

        //giá trị ô dạng chuỗi đã bỏ khoảng trắng hai đầu; ô trống trả về null (JSON gọn hơn)
        private static string DocChuoi(MySqlDataReader reader, int i)
        {
            if (reader.IsDBNull(i))
                return null;
            string s = Convert.ToString(reader.GetValue(i)).Trim();
            return (s.Length == 0) ? null : s;
        }
        #endregion
    }
}
