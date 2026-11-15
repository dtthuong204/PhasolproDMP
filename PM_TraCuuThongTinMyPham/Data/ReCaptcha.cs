using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace PM_TraCuuThongTinMyPham.Data
{
    //ô "Tôi không phải là người máy" (Google reCAPTCHA v2) ở trang Đăng ký
    //khóa đặt trong Config/AppSettings.secret.config (không đưa lên git), tạo từ AppSettings.secret.config.example
    public static class ReCaptcha
    {
        private const string DiaChiKiemTra = "https://www.google.com/recaptcha/api/siteverify";

        public static string SiteKey
        {
            get { return (ConfigurationManager.AppSettings["ReCaptchaSiteKey"] ?? "").Trim(); }
        }

        private static string SecretKey
        {
            get { return (ConfigurationManager.AppSettings["ReCaptchaSecretKey"] ?? "").Trim(); }
        }

        //chưa khai báo đủ 2 khóa thì không hiện ô reCAPTCHA và bỏ qua bước kiểm tra (chỉ dùng khi làm thử)
        public static bool DangBat
        {
            get { return SiteKey != "" && SecretKey != "" && !SiteKey.StartsWith("DÁN"); }
        }

        // Gửi mã người dùng nhận được lên Google để kiểm tra; trả về null nếu hợp lệ, ngược lại là câu báo lỗi
        public static string KiemTra(string maPhanHoi, string ipNguoiDung)
        {
            if (!DangBat)
                return null;
            if (String.IsNullOrEmpty(maPhanHoi))
                return "Hãy bấm ô \"Tôi không phải là người máy\".";
            try
            {
                //.NET 4.5 mặc định dùng TLS 1.0, Google yêu cầu TLS 1.2
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
                using (WebClient wc = new WebClient())
                {
                    NameValueCollection thamSo = new NameValueCollection();
                    thamSo["secret"] = SecretKey;
                    thamSo["response"] = maPhanHoi;
                    thamSo["remoteip"] = ipNguoiDung ?? "";
                    string json = Encoding.UTF8.GetString(wc.UploadValues(DiaChiKiemTra, thamSo));
                    Dictionary<string, object> kq = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                    object thanhCong;
                    if (kq != null && kq.TryGetValue("success", out thanhCong) && thanhCong is bool && (bool)thanhCong)
                        return null;
                    return "Xác nhận \"Tôi không phải là người máy\" chưa đạt hoặc đã hết hạn, hãy bấm lại.";
                }
            }
            catch (Exception ex)
            {
                return "Không kiểm tra được reCAPTCHA (cần kết nối Internet): " + ex.Message;
            }
        }
    }
}
