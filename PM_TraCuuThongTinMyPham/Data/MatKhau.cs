using System;
using System.Security.Cryptography;
using System.Text;

namespace PM_TraCuuThongTinMyPham.Data
{
    //băm và kiểm tra mật khẩu bằng PBKDF2 (Rfc2898DeriveBytes có sẵn trong .NET 4.5), không lưu mật khẩu gốc
    //chuỗi lưu trong cột TaiKhoan.MatKhauHash có dạng "số vòng lặp.salt.hash" (salt, hash viết base64)
    public static class MatKhau
    {
        private const int SoVongLap = 10000;
        private const int DoDaiSalt = 16;
        private const int DoDaiHash = 32;
        //ký tự dùng cho mật khẩu tạm (bỏ các ký tự dễ nhầm: 0 O o 1 l I)
        private const string KyTuTam = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        // Băm mật khẩu với salt ngẫu nhiên
        public static string BamMatKhau(string matKhau)
        {
            byte[] salt = new byte[DoDaiSalt];
            using (RNGCryptoServiceProvider ngauNhien = new RNGCryptoServiceProvider())
            {
                ngauNhien.GetBytes(salt);
            }
            byte[] hash = TinhHash(matKhau, salt, SoVongLap);
            return string.Format("{0}.{1}.{2}", SoVongLap, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        // Kiểm tra mật khẩu người dùng nhập có khớp chuỗi đã băm không
        public static bool KiemTra(string matKhau, string chuoiBam)
        {
            if (matKhau == null || String.IsNullOrEmpty(chuoiBam))
                return false;
            string[] phan = chuoiBam.Split('.');
            if (phan.Length != 3)
                return false;
            try
            {
                int soVong = Convert.ToInt32(phan[0]);
                byte[] salt = Convert.FromBase64String(phan[1]);
                byte[] hashLuu = Convert.FromBase64String(phan[2]);
                byte[] hashNhap = TinhHash(matKhau, salt, soVong);
                return SoSanhDeuThoiGian(hashLuu, hashNhap);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        //chuỗi băm giả (salt, hash toàn số 0) để tên đăng nhập không tồn tại cũng tốn thời gian tính như tên có thật
        private static readonly string ChuoiBamGia = string.Format("{0}.{1}.{2}", SoVongLap,
            Convert.ToBase64String(new byte[DoDaiSalt]), Convert.ToBase64String(new byte[DoDaiHash]));

        // Dùng khi không tìm thấy tài khoản: vẫn tính băm một lần rồi trả về false,
        // để người ngoài không đoán được tên đăng nhập có tồn tại hay không qua thời gian trả lời
        public static bool KiemTraGia(string matKhau)
        {
            KiemTra(matKhau ?? "", ChuoiBamGia);
            return false;
        }

        // "Dấu" của mật khẩu đang lưu (16 ký tự rút từ chuỗi băm), ghi vào cookie đăng nhập.
        // Đổi hoặc đặt lại mật khẩu thì dấu đổi theo, các cookie cũ (thiết bị khác) hết hiệu lực
        public static string TaoDau(string chuoiBam)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] kq = sha.ComputeHash(Encoding.UTF8.GetBytes(chuoiBam ?? ""));
                return Convert.ToBase64String(kq, 0, 12);
            }
        }

        // Kiểm tra độ mạnh: tối thiểu 8 ký tự, có chữ hoa, chữ thường và số; trả về câu báo lỗi hoặc null nếu đạt
        public static string KiemTraDoManh(string matKhau)
        {
            if (String.IsNullOrEmpty(matKhau) || matKhau.Length < 8)
                return "Mật khẩu phải có tối thiểu 8 ký tự.";
            bool coHoa = false, coThuong = false, coSo = false;
            foreach (char c in matKhau)
            {
                if (Char.IsUpper(c)) coHoa = true;
                else if (Char.IsLower(c)) coThuong = true;
                else if (Char.IsDigit(c)) coSo = true;
            }
            if (!coHoa || !coThuong || !coSo)
                return "Mật khẩu phải có chữ hoa, chữ thường và số.";
            return null;
        }

        // Tạo mật khẩu tạm ngẫu nhiên 10 ký tự (luôn có chữ hoa, chữ thường, số) khi Admin cấp hoặc đặt lại mật khẩu
        public static string TaoMatKhauTam()
        {
            byte[] so = new byte[10];
            using (RNGCryptoServiceProvider ngauNhien = new RNGCryptoServiceProvider())
            {
                while (true)
                {
                    ngauNhien.GetBytes(so);
                    StringBuilder kq = new StringBuilder();
                    foreach (byte b in so)
                        kq.Append(KyTuTam[b % KyTuTam.Length]);
                    if (KiemTraDoManh(kq.ToString()) == null)
                        return kq.ToString();
                }
            }
        }

        private static byte[] TinhHash(string matKhau, byte[] salt, int soVong)
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(matKhau, salt, soVong))
            {
                return pbkdf2.GetBytes(DoDaiHash);
            }
        }

        //so sánh hết các byte (không dừng sớm) để không lộ thông tin qua thời gian xử lý
        private static bool SoSanhDeuThoiGian(byte[] a, byte[] b)
        {
            int khac = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                khac |= a[i] ^ b[i];
            return khac == 0;
        }
    }
}
