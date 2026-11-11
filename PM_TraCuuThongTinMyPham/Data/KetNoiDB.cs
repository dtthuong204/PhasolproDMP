using System.Configuration;
using System.Data.SqlClient;

namespace PM_TraCuuThongTinMyPham.Data
{
    public class KetNoiDB
    {
        //tên connection string khai báo trong Web.config
        private const string TenKetNoi = "TraCuuDb";

        //chỉ dùng khi chạy ngoài web (script đánh giá đọc thẳng DLL, không có Web.config); web để null
        public static string ChuoiKetNoiNgoaiWeb { get; set; }

        // Tạo kết nối mới tới database tra cứu (chưa mở), nơi gọi tự mở và đóng bằng using
        public static SqlConnection TaoKetNoi()
        {
            if (!string.IsNullOrEmpty(ChuoiKetNoiNgoaiWeb))
                return new SqlConnection(ChuoiKetNoiNgoaiWeb);

            ConnectionStringSettings cauHinh = ConfigurationManager.ConnectionStrings[TenKetNoi];
            //báo lỗi rõ ràng nếu quên khai báo trong Web.config
            if (cauHinh == null)
                throw new ConfigurationErrorsException(string.Format("Không tìm thấy connection string '{0}' trong Web.config", TenKetNoi));

            return new SqlConnection(cauHinh.ConnectionString);
        }
    }
}
