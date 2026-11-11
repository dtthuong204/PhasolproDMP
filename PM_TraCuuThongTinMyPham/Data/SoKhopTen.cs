using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PM_TraCuuThongTinMyPham.Models;

namespace PM_TraCuuThongTinMyPham.Data
{
    //so khớp tên nhập với danh mục đang giữ trong bộ nhớ (trang tra cứu mới)
    //Chính xác: SYT trùng toàn bộ tên; ASEAN chứa trọn cụm từ. Tương đối: tên chất CHỨA tên nhập (như app cũ)
    public static class SoKhopTen
    {
        // Chuẩn hóa tên để so sánh: bỏ khoảng trắng thừa (kể cả xuống dòng), chữ thường
        public static string ChuanHoa(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "";
            return Regex.Replace(value.Trim(), @"\s+", " ").ToLowerInvariant();
        }

        // Tra danh sách tên (đã bỏ trùng, giữ thứ tự) trên danh mục, gom các chất liên quan vào kết quả
        public static KetQuaTraCuuV2VM TraCuu(List<string> dsTen, DanhMucTraCuu danhMuc)
        {
            KetQuaTraCuuV2VM kq = new KetQuaTraCuuV2VM();
            if (dsTen == null || danhMuc == null)
                return kq;

            foreach (string ten in dsTen)
            {
                string khoa = ChuanHoa(ten);
                if (khoa == "")
                    continue;

                KetQuaTenRow dong = new KetQuaTenRow
                {
                    Ten = Regex.Replace(ten.Trim(), @"\s+", " "),
                    Khoa = khoa
                };

                //danh mục Sở Y tế
                List<ChatSytRow> dsTrung;
                if (danhMuc.SytTheoTen.TryGetValue(khoa, out dsTrung))
                {
                    foreach (ChatSytRow chat in dsTrung)
                        dong.SytChinhXac.Add(ThemSyt(kq, chat, danhMuc));
                }
                foreach (ChatSytRow chat in danhMuc.DSSyt.Where(x => x.TenGon.IndexOf(khoa, StringComparison.Ordinal) >= 0)
                                                        .OrderBy(x => x.Ten, StringComparer.OrdinalIgnoreCase))
                    dong.SytTuongDoi.Add(ThemSyt(kq, chat, danhMuc));

                //Annex ASEAN: lọc "chứa" trước (nhanh), rồi mới kiểm tra trọn cụm từ bằng regex
                Regex tronCumTu = TaoMauTronCumTu(khoa);
                foreach (DongAseanRow item in danhMuc.DSAsean)
                {
                    if (item.TenGon.IndexOf(khoa, StringComparison.Ordinal) < 0)
                        continue;
                    kq.Asean[item.Khoa] = item;
                    dong.AseanTuongDoi.Add(item.Khoa);
                    if (tronCumTu.IsMatch(item.TenGon))
                        dong.AseanChinhXac.Add(item.Khoa);
                }

                kq.DSTen.Add(dong);
            }
            return kq;
        }

        //thêm một chất SYT và các dòng Annex EU nó tham chiếu vào kết quả, trả về khóa của chất
        public static string ThemSyt(KetQuaTraCuuV2VM kq, ChatSytRow chat, DanhMucTraCuu danhMuc)
        {
            if (!kq.Syt.ContainsKey(chat.Khoa))
            {
                kq.Syt.Add(chat.Khoa, chat);
                foreach (ThamChieuEURow thamChieu in chat.DSThamChieu)
                {
                    DongEURow dongEU;
                    if (thamChieu.KhoaEU != null && danhMuc.EU.TryGetValue(thamChieu.KhoaEU, out dongEU))
                        kq.EU[dongEU.Khoa] = dongEU;
                }
            }
            return chat.Khoa;
        }

        //trọn cụm từ (đã chốt): ngay trước tên không phải chữ cái, chữ số, ")" hoặc "]";
        //ngay sau tên không phải chữ cái, chữ số. Vd "salicylic acid" không khớp "...azo]salicylic acid"
        public static Regex TaoMauTronCumTu(string khoa)
        {
            string mau = @"(?<![\p{L}\p{N}\)\]])" + Regex.Escape(khoa) + @"(?![\p{L}\p{N}])";
            return new Regex(mau, RegexOptions.CultureInvariant);
        }
    }
}
