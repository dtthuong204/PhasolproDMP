using System;
using System.Configuration;
using System.Web;
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

        // Đổi lỗi database thành câu báo cho người dùng (các Repository ghi vào thuộc tính Loi).
        // Người xem từ máy khác không thấy chi tiết kỹ thuật (tên máy chủ, tên bảng, tài khoản database);
        // chạy trên máy mình (localhost) hoặc ngoài web vẫn thấy nguyên văn để sửa lỗi, giống customErrors RemoteOnly
        public static string ThongBaoLoi(Exception ex)
        {
            MySqlException loiMySql = ex as MySqlException;
            if (loiMySql != null)
            {
                //1062: trùng khóa UNIQUE (hai người cùng đăng ký một tên / email trong cùng lúc, lọt qua bước kiểm tra trùng)
                if (loiMySql.Number == 1062)
                    return "tên đăng nhập, email hoặc mã số doanh nghiệp đã có trong hệ thống.";
                //1406: chữ dài hơn cột
                if (loiMySql.Number == 1406)
                    return "có ô nhập dài quá mức cho phép.";
            }

            HttpContext web = HttpContext.Current;
            if (web == null || web.Request.IsLocal)
                return ex.Message;
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return "không truy cập được cơ sở dữ liệu, hãy thử lại sau ít phút.";
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
