using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace PM_TraCuuThongTinMyPham.Data
{
    public class KetNoiDB
    {
        //tên connection string khai báo trong Config\ConnectionStrings.config
        private const string TenKetNoi = "TraCuuDb";

        //chỉ dùng khi chạy ngoài web (script đánh giá đọc thẳng DLL, không có Web.config); web để null
        public static string ChuoiKetNoiNgoaiWeb { get; set; }

        // Tạo kết nối mới tới database tra cứu (chưa mở), nơi gọi tự mở và đóng bằng using
        public static SqlConnection TaoKetNoi()
        {
            if (!string.IsNullOrEmpty(ChuoiKetNoiNgoaiWeb))
                return new SqlConnection(ChuoiKetNoiNgoaiWeb);

            ConnectionStringSettings cauHinh = ConfigurationManager.ConnectionStrings[TenKetNoi];
            //báo lỗi rõ ràng nếu quên khai báo
            if (cauHinh == null)
                throw new ConfigurationErrorsException(string.Format("Không tìm thấy connection string '{0}' trong Config\\ConnectionStrings.config", TenKetNoi));

            return new SqlConnection(cauHinh.ConnectionString);
        }

        // Đổi lỗi database thành câu báo cho người dùng (các Repository ghi vào thuộc tính Loi).
        // Người xem từ máy khác không thấy chi tiết kỹ thuật (tên máy chủ, tên bảng, tài khoản database);
        // chạy trên máy mình (localhost) hoặc ngoài web vẫn thấy nguyên văn để sửa lỗi, giống customErrors RemoteOnly
        public static string ThongBaoLoi(Exception ex)
        {
            SqlException loiSql = ex as SqlException;
            if (loiSql != null)
            {
                //2627, 2601: trùng khóa UNIQUE (hai người cùng đăng ký một tên / email trong cùng lúc, lọt qua bước kiểm tra trùng)
                if (loiSql.Number == 2627 || loiSql.Number == 2601)
                    return "tên đăng nhập, email hoặc mã số doanh nghiệp đã có trong hệ thống.";
                //8152, 2628: chữ dài hơn cột
                if (loiSql.Number == 8152 || loiSql.Number == 2628)
                    return "có ô nhập dài quá mức cho phép.";
            }

            HttpContext web = HttpContext.Current;
            if (web == null || web.Request.IsLocal)
                return ex.Message;
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return "không truy cập được cơ sở dữ liệu, hãy thử lại sau ít phút.";
        }

        // Mở kết nối (giữ thành hàm riêng để các Repository gọi thống nhất một chỗ)
        public static void MoKetNoi(SqlConnection conn)
        {
            conn.Open();
        }
    }
}
