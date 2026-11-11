using System;
using System.Data;
using System.Data.SqlClient;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    public class TraCuuRepository
    {
        //thông báo lỗi của lần gọi gần nhất, null nếu không có lỗi
        public string Loi { get; private set; }

        #region Chi tiết một dòng Annex
        private const string SqlChiTietAsean = @"
SELECT Annex, RefNo, TenChat, CAS, PhamViSuDung, GioiHanToiDa, DieuKienKhac, CanhBaoNhan,
       CanCu, NgayApDung, NgayHetHieuLuc, GhiChu, TrangThaiHieuLuc
FROM dbo.vw_ACD_TatCa
WHERE Bang = @bang AND RowID = @rowId";

        private const string SqlChiTietEU = @"
SELECT Annex, RefNo, TenChat, TenINCI, CAS, PhamViSuDung, GioiHanToiDa, DieuKienKhac, CanhBaoNhan,
       Regulation, NgayCapNhat
FROM dbo.vw_EU_TatCa
WHERE Bang = @bang AND RowID = @rowId";

        // Lấy nội dung đầy đủ của một dòng Annex (nguon = "ACD" hoặc "EU") cho hộp Xem chi tiết
        // Trả về null nếu không tìm thấy hoặc có lỗi (xem Loi)
        public ChiTietVM GetChiTiet(string nguon, string bang, int rowId)
        {
            Loi = null;
            bool laAsean = (nguon == "ACD");
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(laAsean ? SqlChiTietAsean : SqlChiTietEU, conn))
                {
                    cmd.Parameters.Add("@bang", SqlDbType.NVarChar, 128).Value = bang ?? "";
                    cmd.Parameters.Add("@rowId", SqlDbType.Int).Value = rowId;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;
                        return laAsean ? ChiTietVM.fromReaderAsean(reader) : ChiTietVM.fromReaderEU(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = ex.Message;
                return null;
            }
        }

        //các cột chỉ có ở bảng gốc Annex EU; {0} = tên bảng (đã kiểm tra trong DSBangEU), {1} = cột Colour index / màu của Annex IV
        private const string SqlThongTinThemEU = @"
SELECT [EC Number] AS EC, [Other Directives/Regulations] AS ChiThiKhac, [SCCS opinions] AS SCCS,
       [Chemical/IUPAC Name] AS IUPAC, [Identified INGREDIENTS or substances e.g.] AS ThanhPhanNhanDien, [CMR] AS CMR{1}
FROM dbo.{0}
WHERE ID = @rowId";

        private const string SqlCotMauAnnexIV = @",
       [Colour index Number / Name of Common Ingredients Glossary] AS ColourIndex, [Color] AS MauSac";

        //chỉ cho phép đọc 5 bảng Annex EU (tên bảng không truyền được bằng tham số)
        private static readonly string[] DSBangEU = { "EU_AnnexII", "EU_AnnexIII", "EU_AnnexIV", "EU_AnnexV", "EU_AnnexVI" };

        // Lấy thêm EC, Other Directives, SCCS, IUPAC, Identified, CMR (và Colour index, màu của Annex IV) cho hộp chi tiết EU
        // Ghi vào model; bảng không hợp lệ hoặc lỗi thì để trống (xem Loi)
        public void GetThongTinThemEU(ChiTietV2VM model, string bang, int rowId)
        {
            Loi = null;
            if (model == null || Array.IndexOf(DSBangEU, bang) < 0)
                return;
            bool laAnnexIV = (bang == "EU_AnnexIV");
            string sql = string.Format(SqlThongTinThemEU, bang, laAnnexIV ? SqlCotMauAnnexIV : "");
            try
            {
                using (SqlConnection conn = KetNoiDB.TaoKetNoi())
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@rowId", SqlDbType.Int).Value = rowId;
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return;
                        model.EC = Convert.ToString(reader["EC"]);
                        model.ChiThiKhac = Convert.ToString(reader["ChiThiKhac"]);
                        model.SCCS = Convert.ToString(reader["SCCS"]);
                        model.IUPAC = Convert.ToString(reader["IUPAC"]);
                        model.ThanhPhanNhanDien = Convert.ToString(reader["ThanhPhanNhanDien"]);
                        model.CMR = Convert.ToString(reader["CMR"]);
                        if (laAnnexIV)
                        {
                            model.ColourIndex = Convert.ToString(reader["ColourIndex"]);
                            model.MauSac = Convert.ToString(reader["MauSac"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loi = ex.Message;
            }
        }
        #endregion
    }
}
