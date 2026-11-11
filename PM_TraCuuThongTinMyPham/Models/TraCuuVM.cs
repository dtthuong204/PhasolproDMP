using System;
using System.Data.SqlClient;

namespace PM_TraCuuThongTinMyPham.Models
{
    //nội dung hộp "Xem chi tiết" của một dòng Annex
    public class ChiTietVM
    {
        public string Nguon { get; set; }
        public string Annex { get; set; }
        public string TenNhap { get; set; }
        public string RefNo { get; set; }
        public string TenChat { get; set; }
        public string TenINCI { get; set; }
        public string CAS { get; set; }
        public string PhamViSuDung { get; set; }
        public string GioiHanToiDa { get; set; }
        public string DieuKienKhac { get; set; }
        public string CanhBaoNhan { get; set; }
        public string GhiChu { get; set; }

        //chỉ ASEAN
        public string CanCu { get; set; }
        public DateTime? NgayApDung { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public string TrangThaiHieuLuc { get; set; }

        //chỉ EU
        public string Regulation { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        //null nếu không có lỗi
        public string Loi { get; set; }

        public static ChiTietVM fromReaderAsean(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            return new ChiTietVM
            {
                Nguon = "ACD",
                Annex = Convert.ToString(reader["Annex"]),
                RefNo = Convert.ToString(reader["RefNo"]),
                TenChat = Convert.ToString(reader["TenChat"]),
                CAS = Convert.ToString(reader["CAS"]),
                PhamViSuDung = Convert.ToString(reader["PhamViSuDung"]),
                GioiHanToiDa = Convert.ToString(reader["GioiHanToiDa"]),
                DieuKienKhac = Convert.ToString(reader["DieuKienKhac"]),
                CanhBaoNhan = Convert.ToString(reader["CanhBaoNhan"]),
                GhiChu = Convert.ToString(reader["GhiChu"]),
                CanCu = Convert.ToString(reader["CanCu"]),
                NgayApDung = DocNgay(reader["NgayApDung"]),
                NgayHetHieuLuc = DocNgay(reader["NgayHetHieuLuc"]),
                TrangThaiHieuLuc = Convert.ToString(reader["TrangThaiHieuLuc"])
            };
        }

        public static ChiTietVM fromReaderEU(SqlDataReader reader)
        {
            if (reader == null)
                return null;
            return new ChiTietVM
            {
                Nguon = "EU",
                Annex = Convert.ToString(reader["Annex"]),
                RefNo = Convert.ToString(reader["RefNo"]),
                TenChat = Convert.ToString(reader["TenChat"]),
                TenINCI = Convert.ToString(reader["TenINCI"]),
                CAS = Convert.ToString(reader["CAS"]),
                PhamViSuDung = Convert.ToString(reader["PhamViSuDung"]),
                GioiHanToiDa = Convert.ToString(reader["GioiHanToiDa"]),
                DieuKienKhac = Convert.ToString(reader["DieuKienKhac"]),
                CanhBaoNhan = Convert.ToString(reader["CanhBaoNhan"]),
                Regulation = Convert.ToString(reader["Regulation"]),
                NgayCapNhat = DocNgay(reader["NgayCapNhat"])
            };
        }

        //cột ngày có thể NULL trong database (DBNull)
        private static DateTime? DocNgay(object giaTri)
        {
            if (giaTri == null || giaTri == DBNull.Value)
                return null;
            return Convert.ToDateTime(giaTri);
        }
    }
}
