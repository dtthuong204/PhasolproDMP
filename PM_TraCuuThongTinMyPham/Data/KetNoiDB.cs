using System.Configuration;
using MySql.Data.MySqlClient;

namespace PM_TraCuuThongTinMyPham.Data
{
    public class KetNoiDB
    {
        //tên connection string khai báo trong Config\ConnectionStrings.config
        //(nhánh mysql dùng "TraCuuMySql"; "TraCuuDb" là bản SQL Server của nhánh main, giữ nguyên trong file)
        private const string TenKetNoi = "TraCuuMySql";

        //kiểu so sánh chữ của database: không phân biệt hoa thường, có phân biệt dấu (giống bản SQL Server)
        private const string SqlKieuSoSanh = "SET collation_connection = 'utf8mb4_0900_as_ci'";

        //chỉ dùng khi chạy ngoài web (script đánh giá đọc thẳng DLL, không có Web.config); web để null
        public static string ChuoiKetNoiNgoaiWeb { get; set; }

        // Tạo kết nối mới tới database tra cứu (chưa mở), nơi gọi mở bằng MoKetNoi và đóng bằng using
        public static MySqlConnection TaoKetNoi()
        {
            if (!string.IsNullOrEmpty(ChuoiKetNoiNgoaiWeb))
                return new MySqlConnection(ChuoiKetNoiNgoaiWeb);

            ConnectionStringSettings cauHinh = ConfigurationManager.ConnectionStrings[TenKetNoi];
            //báo lỗi rõ ràng nếu quên khai báo
            if (cauHinh == null)
                throw new ConfigurationErrorsException(string.Format("Không tìm thấy connection string '{0}' trong Config\\ConnectionStrings.config", TenKetNoi));

            return new MySqlConnection(cauHinh.ConnectionString);
        }

        // Mở kết nối rồi đặt kiểu so sánh chữ cho chữ viết trong câu SQL và tham số @...
        // (mặc định của kết nối là utf8mb4_0900_ai_ci, khác chữ trong view -> lỗi "Illegal mix of collations")
        public static void MoKetNoi(MySqlConnection conn)
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(SqlKieuSoSanh, conn))
                cmd.ExecuteNonQuery();
        }
    }
}
