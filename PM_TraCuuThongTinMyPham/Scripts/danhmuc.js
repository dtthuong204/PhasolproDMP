// Trang Danh mục (/DanhMuc): tải dữ liệu 13 danh mục một lần (/DanhMuc/DuLieu), vẽ thanh trái và bảng của danh mục đang chọn.
// Mọi việc lọc, tìm, sắp xếp, phân trang làm trên trình duyệt, không gọi lại server.
(function ($) {
    "use strict";

    // ===================== Hằng số =====================
    // khóa localStorage: danh mục đang xem, phạm vi tìm, số dòng mỗi trang, cột ẩn/hiện của từng danh mục
    var KHOA_LUU = "phasolpro.danhmuc";

    var DS_NHOM = [
        { ma: "SYT", ten: "DANH MỤC EU" },
        { ma: "EU", ten: "ANNEX EU (COSING)" },
        { ma: "ASEAN", ten: "ANNEX ASEAN" }
    ];

    // màu chấm của từng danh mục: cấm đỏ, cho phép xanh lá, còn lại (giới hạn) cam
    var MAU_DANH_MUC = { "syt-cam": "cam", "eu-2": "cam", "a-2": "cam", "syt-cp": "chophep" };

    // tên ngắn ghép với nhóm, dùng ở nút gợi ý "Có kết quả ở danh mục khác"
    var TEN_NHOM_NGAN = { SYT: "Danh mục", EU: "EU", ASEAN: "ASEAN" };

    var TRANG_THAI_LOAI_BO = "Đã loại bỏ";

    // trạng thái hiệu lực ASEAN: thứ tự nút lọc, chữ ngắn trên nhãn, lớp màu
    var DS_TRANG_THAI = [
        { giaTri: "Đang áp dụng", nhan: "Đang áp dụng", lop: "on" },
        { giaTri: "Sắp áp dụng", nhan: "Sắp áp dụng", lop: "soon" },
        { giaTri: "Đang áp dụng - sắp thay đổi", nhan: "Sắp thay đổi", lop: "change" },
        { giaTri: "Đã đến ngày thay đổi - cần cập nhật dữ liệu", nhan: "Cần cập nhật", lop: "change" },
        { giaTri: TRANG_THAI_LOAI_BO, nhan: "Đã loại bỏ", lop: "off" }
    ];

    var DS_SO_DONG = [50, 100, 200];

    // tên loại danh mục SYT theo bảng (cột "Danh mục SYT" của Annex EU)
    var LOAI_SYT = {
        SYT_DanhMucCam: { ten: "Cấm sử dụng", lop: "cam", ma: "syt-cam" },
        SYT_DanhMucGioiHan: { ten: "Giới hạn", lop: "gioihan", ma: "syt-gh" },
        SYT_DanhMucChoPhep: { ten: "Cho phép", lop: "chophep", ma: "syt-cp" }
    };

    // màu ô vuông cạnh tên màu (Annex IV ASEAN)
    var MAU_SAC = {
        green: "#3c9a4a", yellow: "#f2c531", orange: "#ee8a2a", red: "#d6333f", blue: "#3166c9",
        violet: "#7b4bb7", brown: "#8a5a2b", black: "#222222", white: "#ffffff"
    };

    var ICON_SEARCH = '<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>';
    var ICON_COT = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16M15 4v16"/></svg>';
    var ICON_CHU_THICH = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 4h11a3 3 0 013 3v13H8a3 3 0 01-3-3V4z"/><path d="M9 9h6M9 13h6"/></svg>';
    var ICON_TICK ='<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3.2" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12l5 5 9-10"/></svg>';
    var ICON_TRAI = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M15 6l-6 6 6 6"/></svg>';
    var ICON_PHAI = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M9 6l6 6-6 6"/></svg>';
    var ICON_KHONG_CO = '<svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5M8.5 8.5l5 5M13.5 8.5l-5 5"/></svg>';

    // ===================== Trạng thái =====================
    var duLieu = null;          // JSON server trả về
    var theoMa = {};            // mã danh mục -> danh mục (thêm viTri, soDong, dsCot, chuoiTim)
    var lienKetTheoSyt = {};    // "BangSYT#ID" -> [{ ref, khoa }] theo đúng thứ tự trong MoTa
    var sytTheoEU = {};         // "EU_AnnexII#ID" -> [{ bang, id }] chất SYT tham chiếu tới dòng EU này
    var dongEU = {};            // "EU_AnnexII#ID" -> { dm, dong }
    var dongSyt = {};           // "SYT_DanhMucCam#ID" -> { dm, dong }

    var giaoDien = {
        ma: "syt-cam",
        tim: "",                // từ khóa đã chuẩn hóa (chữ thường)
        phamVi: "chinh",        // chinh: tên, CAS, ref; tatca: mọi cột
        soDong: 50,
        trang: 1,
        loc: {},                // mã danh mục -> bộ lọc (SYT: chuỗi; ASEAN: { trạng thái: true })
        sapXep: {},             // mã danh mục -> { cot, chieu }
        cot: {}                 // mã danh mục -> { mã cột: true/false } do người dùng chọn
    };

    var ketQua = { ds: [], demTim: 0 };   // chỉ số các dòng sau lọc + sắp xếp của danh mục đang xem
    var henGioTim = null;

    // ===================== Tiện ích =====================
    // chống chèn HTML khi đưa dữ liệu vào trang
    function maHoa(s) {
        if (s === null || s === undefined) return "";
        return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }

    // 2175 -> "2.175"
    function dinhDangSo(n) {
        return String(n).replace(/\B(?=(\d{3})+(?!\d))/g, ".");
    }

    // chữ thường, gộp khoảng trắng, bỏ khoảng trắng hai đầu
    function chuanHoa(s) {
        return String(s || "").toLowerCase().replace(/\s+/g, " ").replace(/^ | $/g, "");
    }

    // chữ có tô vàng phần khớp từ khóa đang tìm
    function vanBan(s) {
        if (s === null || s === undefined || s === "") return "";
        s = String(s);
        var tk = giaoDien.tim;
        if (!tk) return maHoa(s);
        var thuong = s.toLowerCase(), kq = "", i = 0, j;
        while ((j = thuong.indexOf(tk, i)) >= 0) {
            kq += maHoa(s.slice(i, j)) + '<mark class="dm-hl">' + maHoa(s.substr(j, tk.length)) + "</mark>";
            i = j + tk.length;
        }
        return kq + maHoa(s.slice(i));
    }

    // so sánh kiểu "tự nhiên": 2 < 10, 1 < 1a < 2; ô trống xếp cuối
    function soSanhTuNhien(a, b) {
        a = (a || "").toLowerCase();
        b = (b || "").toLowerCase();
        if (!a || !b) return a ? -1 : (b ? 1 : 0);
        var ra = a.match(/\d+|\D+/g), rb = b.match(/\d+|\D+/g);
        for (var k = 0; k < ra.length && k < rb.length; k++) {
            var x = ra[k], y = rb[k];
            if (/^\d/.test(x) && /^\d/.test(y)) {
                var hieu = parseInt(x, 10) - parseInt(y, 10);
                if (hieu !== 0) return hieu;
            } else if (x !== y) {
                return x < y ? -1 : 1;
            }
        }
        return ra.length - rb.length;
    }

    function soSanhChu(a, b) {
        a = a || "";
        b = b || "";
        if (!a || !b) return a ? -1 : (b ? 1 : 0);
        return a.localeCompare(b, "vi", { sensitivity: "base" });
    }

    function docLuu() {
        try {
            var s = window.localStorage.getItem(KHOA_LUU);
            if (!s) return;
            var o = JSON.parse(s);
            if (!o) return;
            if (o.ma) giaoDien.ma = o.ma;
            if (o.phamVi === "tatca") giaoDien.phamVi = "tatca";
            if ($.inArray(o.soDong, DS_SO_DONG) >= 0) giaoDien.soDong = o.soDong;
            if (o.cot) giaoDien.cot = o.cot;
        } catch (e) { /* trình duyệt chặn localStorage: dùng mặc định */ }
    }

    function ghiLuu() {
        try {
            window.localStorage.setItem(KHOA_LUU, JSON.stringify({
                ma: giaoDien.ma, phamVi: giaoDien.phamVi, soDong: giaoDien.soDong, cot: giaoDien.cot
            }));
        } catch (e) { /* bỏ qua */ }
    }

    // giá trị ô theo tên cột
    function layO(dm, dong, cot) {
        var i = dm.viTri[cot];
        return (i === undefined) ? null : dong[i];
    }

    function khoaDong(dm, dong) {
        return dm.Bang + "#" + layO(dm, dong, "ID");
    }

    function laLoaiBo(dm, dong) {
        return dm.Nhom === "ASEAN" && layO(dm, dong, "TrangThaiHieuLuc") === TRANG_THAI_LOAI_BO;
    }

    function timTrangThai(giaTri) {
        for (var i = 0; i < DS_TRANG_THAI.length; i++)
            if (DS_TRANG_THAI[i].giaTri === giaTri) return DS_TRANG_THAI[i];
        return { giaTri: giaTri, nhan: giaTri || "-", lop: "off" };
    }

    // ===================== Nội dung ô =====================
    function oTrong() {
        return '<span class="dm-trong">-</span>';
    }

    // ô chữ nhiều dòng; dài quá 3 dòng thì rút gọn, có nút "xem thêm"; ASEAN: số chú thích "(8)" thành nút
    function oChu(s, dm) {
        if (!s) return oTrong();
        // gộp các dòng trống liên tiếp cho gọn
        s = s.replace(/\n\s*\n+/g, "\n");
        var dai = s.length > 170 || s.split("\n").length > 3;
        return '<div class="dm-text' + (dai ? " dm-clamp" : "") + '">' + ganChuThich(vanBan(s), dm) + "</div>" +
            (dai ? '<button type="button" class="dm-more">xem thêm</button>' : "");
    }

    // ô ref của dòng EU/ASEAN: dòng đầu là nút (mở hộp chi tiết ở bước sau), phần còn lại (vd "(308)") ghi nhỏ bên dưới
    function oRefDong(s, tienTo) {
        if (!s) return oTrong();
        var ds = s.split("\n");
        var html = '<button type="button" class="ref-btn dm-ref-dong">' + (tienTo || "") + vanBan(ds[0]) + "</button>";
        if (ds.length > 1) html += '<span class="cell-sub">' + vanBan(ds.slice(1).join(" ")) + "</span>";
        return html;
    }

    // các ô tham chiếu Annex EU của một chất SYT; không có liên kết thì ghi nguyên cột MoTa
    function oRefSyt(dm, dong) {
        var ds = lienKetTheoSyt[khoaDong(dm, dong)];
        if (!ds || !ds.length) {
            var moTa = layO(dm, dong, "MoTa");
            return moTa ? '<span class="dm-text">' + vanBan(moTa) + "</span>" : oTrong();
        }
        return $.map(ds, function (lk) {
            if (lk.khoa)
                return '<button type="button" class="ref-btn" data-khoa-eu="' + maHoa(lk.khoa) + '">' + vanBan(lk.ref) + "</button>";
            return '<span class="ref-btn missing" title="Tham chiếu này không có trong Annex EU">' + vanBan(lk.ref) + "</span>";
        }).join(" ");
    }

    // giá trị một cột Annex EU II của các ref (chỉ danh mục Cấm), xếp chồng theo thứ tự ref
    function oEUCuaSyt(dm, dong, cot) {
        var ds = lienKetTheoSyt[khoaDong(dm, dong)] || [];
        var dsGiaTri = [];
        $.each(ds, function (i, lk) {
            var eu = lk.khoa ? dongEU[lk.khoa] : null;
            if (eu) dsGiaTri.push(layO(eu.dm, eu.dong, cot));
        });
        if (!dsGiaTri.length) return oTrong();
        return $.map(dsGiaTri, function (v) {
            return '<div class="dm-stack">' + (v ? '<span class="dm-text">' + vanBan(v) + "</span>" : oTrong()) + "</div>";
        }).join("");
    }

    // các chất SYT tham chiếu tới dòng EU: nhãn theo loại danh mục, kèm số chất nếu nhiều hơn 1
    function oSytCuaEU(dm, dong) {
        var ds = sytTheoEU[khoaDong(dm, dong)];
        if (!ds || !ds.length) return oTrong();
        var dem = {}, thuTu = [];
        $.each(ds, function (i, x) {
            if (!dem[x.bang]) { dem[x.bang] = 0; thuTu.push(x.bang); }
            dem[x.bang]++;
        });
        return $.map(thuTu, function (bang) {
            var loai = LOAI_SYT[bang] || { ten: bang, lop: "gioihan" };
            return '<span class="dm-tag ' + loai.lop + '">' + loai.ten + (dem[bang] > 1 ? " · " + dem[bang] : "") + "</span>";
        }).join(" ");
    }

    function oHieuLuc(dm, dong) {
        var tt = timTrangThai(layO(dm, dong, "TrangThaiHieuLuc"));
        var phu = [];
        var canCu = layO(dm, dong, "CanCu");
        var ngay = layO(dm, dong, "NgayApDung");
        var het = layO(dm, dong, "NgayHetHieuLuc");
        if (canCu) phu.push(vanBan(canCu));
        if (ngay) phu.push("từ " + maHoa(ngay));
        if (het) phu.push("hết hiệu lực " + maHoa(het));
        return '<span class="dm-tag ' + tt.lop + '">' + maHoa(tt.nhan) + "</span>" +
            (phu.length ? '<span class="cell-sub">' + phu.join(" · ") + "</span>" : "");
    }

    function oTenSyt(dm, dong) {
        // cột Data (Gốc / Dự kiến bổ sung) không hiện, như app cũ (đối chiếu 2026-11-11)
        return '<span class="cell-name">' + vanBan(layO(dm, dong, "Ten")) + "</span>";
    }

    function oMau(dm, dong) {
        var mau = layO(dm, dong, "Mau");
        if (!mau) return oTrong();
        var ma = MAU_SAC[String(mau).toLowerCase()] || "#b5b9c4";
        return '<span class="dm-swatch" style="background:' + ma + '"></span>' + vanBan(mau);
    }

    // ===================== Cột của từng danh mục =====================
    // mỗi cột: ma, tieuDe, rong (px), khoa (luôn hiện), an (mặc định ẩn), gopVao (hiện thành dòng phụ của cột khác),
    // o(dm, dong, stt) -> html, gt(dm, dong) -> giá trị sắp xếp, sx: "chu" | "ref" (không có: không sắp xếp được)
    function cotChu(ma, tieuDe, rong, an) {
        return {
            ma: ma, tieuDe: tieuDe, rong: rong, an: !!an, sx: "chu",
            o: function (dm, dong) { return oChu(layO(dm, dong, ma), dm); },
            gt: function (dm, dong) { return layO(dm, dong, ma); }
        };
    }

    function cotStt() {
        return { ma: "_stt", tieuDe: "STT", rong: 56, khoa: true, laStt: true, o: function (dm, dong, stt) { return stt; } };
    }

    function cotHieuLuc() {
        return {
            ma: "_hieuluc", tieuDe: "HIỆU LỰC", rong: 140, khoa: true, sx: "chu",
            o: oHieuLuc, gt: function (dm, dong) { return layO(dm, dong, "TrangThaiHieuLuc"); }
        };
    }

    function cotRef(tieuDe, tienTo) {
        return {
            ma: "RefNo", tieuDe: tieuDe || "REF", rong: tienTo ? 130 : 72, khoa: true, sx: "ref",
            o: function (dm, dong) { return oRefDong(layO(dm, dong, "RefNo"), tienTo); },
            gt: function (dm, dong) { return layO(dm, dong, "RefNo"); }
        };
    }

    function cotTenChat(tieuDe, rong) {
        var c = cotChu("TenChat", tieuDe, rong);
        c.khoa = true;
        c.o = function (dm, dong) { return '<span class="cell-name dm-text">' + ganChuThich(vanBan(layO(dm, dong, "TenChat")), dm) + "</span>"; };
        return c;
    }

    function cotPhu(ma, tieuDe, gopVao, tienTo) {
        return {
            ma: ma, tieuDe: tieuDe, gopVao: gopVao,
            o: function (dm, dong) {
                var v = layO(dm, dong, ma);
                return v ? '<span class="cell-sub">' + (tienTo || "") + vanBan(v) + "</span>" : "";
            }
        };
    }

    function dsCotSyt(dm) {
        var ds = [cotStt()];
        var ten = cotChu("Ten", "TÊN CHẤT", dm.Ma === "syt-cam" ? 200 : 230);
        ten.khoa = true;
        ten.o = oTenSyt;
        ds.push(ten);
        var ref = {
            ma: "_ref", tieuDe: dm.Ma === "syt-gh" ? "THAM CHIẾU ANNEX EU" : "REF", rong: dm.Ma === "syt-gh" ? 170 : 96,
            an: dm.Ma === "syt-cp", o: oRefSyt
        };
        ds.push(ref);
        if (dm.Ma === "syt-cam") {
            ds.push({ ma: "_euTen", tieuDe: "CHEMICAL NAME / INN", rong: 210, o: function (d, dong) { return oEUCuaSyt(d, dong, "TenChat"); } });
            ds.push({ ma: "_euCas", tieuDe: "CAS", rong: 100, o: function (d, dong) { return oEUCuaSyt(d, dong, "CAS"); } });
            ds.push({ ma: "_euEc", tieuDe: "EC", rong: 96, o: function (d, dong) { return oEUCuaSyt(d, dong, "EC"); } });
        }
        ds.push(cotChu("ThongTin", "THÔNG TIN", null, dm.Ma === "syt-cp"));
        ds.push(cotChu("GhiChu", "GHI CHÚ", dm.Ma === "syt-cp" ? null : 90));
        ds.push(cotChu("MoTa", "MÔ TẢ (GỐC)", 120, true));
        return ds;
    }

    function dsCotEU(dm) {
        var ds = [cotRef()];
        if (dm.Annex === "II") {
            ds.push(cotTenChat("CHEMICAL NAME / INN", null));
            ds.push(cotChu("CAS", "CAS", 130));
            ds.push(cotChu("EC", "EC", 120));
        } else {
            ds.push(cotTenChat(dm.Annex === "IV" ? "CHEMICAL NAME" : "CHEMICAL NAME / INN", 230));
            ds.push(cotPhu("TenINCI", dm.Annex === "IV" ? "Colour index / Glossary name" : "Name of Common Ingredients Glossary", "TenChat", "INCI: "));
            ds.push(cotChu("CAS", "CAS / EC", 120));
            ds.push(cotPhu("EC", "EC Number", "CAS", "EC "));
            if (dm.Annex === "IV") {
                var mau = cotChu("Mau", "COLOR", 100);
                mau.o = oMau;
                ds.push(mau);
            }
            ds.push(cotChu("PhamVi", "PRODUCT TYPE, BODY PARTS", 210));
            ds.push(cotChu("GioiHan", "MAX. CONCENTRATION", 170));
            ds.push(cotChu("Khac", "OTHER", null));
            ds.push(cotChu("CanhBao", "WORDING OF CONDITIONS / WARNINGS", null, true));
        }
        ds.push({ ma: "_syt", tieuDe: "DANH MỤC EU", rong: 116, o: oSytCuaEU });
        ds.push(cotChu("Regulation", "REGULATION", 120, true));
        ds.push(cotChu("NgayCapNhat", "UPDATE DATE", 110, true));
        ds.push(cotChu("ChiThiKhac", "OTHER DIRECTIVES/REGULATIONS", 160, true));
        ds.push(cotChu("SCCS", "SCCS OPINIONS", null, true));
        ds.push(cotChu("IUPAC", "CHEMICAL/IUPAC NAME", null, true));
        ds.push(cotChu("ThanhPhanNhanDien", "IDENTIFIED INGREDIENTS OR SUBSTANCES", null, true));
        ds.push(cotChu("CMR", "CMR", 120, true));
        return ds;
    }

    function dsCotAsean(dm) {
        var ds = [];
        if (dm.Annex === "IV") {
            ds.push(cotRef("COLOUR INDEX", "CI "));
            var mau = cotChu("Mau", "COLOUR", 130);
            mau.o = oMau;
            ds.push(mau);
            $.each(["F1", "F2", "F3", "F4"], function (i, f) {
                ds.push({
                    ma: f, tieuDe: String(i + 1), rong: 56, canGiua: true, khoa: true,
                    goiY: ["Dùng được trong mọi sản phẩm mỹ phẩm",
                        "Mọi sản phẩm, trừ sản phẩm dùng quanh mắt (trang điểm mắt, tẩy trang mắt)",
                        "Chỉ sản phẩm không tiếp xúc với niêm mạc",
                        "Chỉ sản phẩm tiếp xúc với da trong thời gian ngắn"][i],
                    o: function (d, dong) { return layO(d, dong, f) ? '<span class="dm-tick">' + ICON_TICK + "</span>" : ""; }
                });
            });
            ds.push(cotChu("Khac", "OTHER LIMITATIONS AND REQUIREMENTS", null));
        } else {
            ds.push(cotRef());
            ds.push(cotTenChat("SUBSTANCES", dm.Annex === "II" ? null : 200));
            if (dm.Annex === "II") {
                ds.push(cotChu("CAS", "CAS NUMBER", 170));
            } else {
                if (dm.Annex === "III") ds.push(cotChu("PhamVi", "FIELD OF APPLICATION", 190));
                ds.push(cotChu("GioiHan", "MAX. CONCENTRATION", 150));
                ds.push(cotChu("Khac", dm.Annex === "III" ? "OTHER LIMITATIONS" : "LIMITATIONS", null));
                ds.push(cotChu("CanhBao", "LABEL WARNING", 150));
            }
        }
        ds.push(cotHieuLuc());
        ds.push(cotChu("GhiChu", "GHI CHÚ", 160, true));
        return ds;
    }

    // cột đang hiện: người dùng đã chọn thì theo lựa chọn, không thì theo mặc định
    function cotHien(dm, cot) {
        if (cot.khoa) return true;
        var chon = giaoDien.cot[dm.Ma];
        if (chon && chon[cot.ma] !== undefined) return chon[cot.ma];
        return !cot.an;
    }

    function soCotAn(dm) {
        var n = 0;
        $.each(dm.dsCot, function (i, cot) { if (!cot.khoa && !cotHien(dm, cot)) n++; });
        return n;
    }

    // ===================== Tìm kiếm, lọc, sắp xếp =====================
    // chuỗi để tìm của mỗi dòng (tính một lần): "chinh" = tên, CAS, ref; "tatca" = mọi ô
    function chuoiTim(dm, i) {
        var bo = dm.chuoiTim[giaoDien.phamVi];
        if (!bo) bo = dm.chuoiTim[giaoDien.phamVi] = [];
        if (bo[i] !== undefined) return bo[i];

        var dong = dm.DSDong[i], phan = [];
        if (giaoDien.phamVi === "tatca") {
            $.each(dong, function (j, v) { if (v && dm.DSCot[j] !== "ID") phan.push(v); });
        } else {
            $.each(["Ten", "MoTa", "RefNo", "TenChat", "TenINCI", "CAS", "EC", "Mau"], function (j, cot) {
                var v = layO(dm, dong, cot);
                if (v) phan.push(v);
            });
        }
        // danh mục Cấm: tìm cả tên, CAS, EC của Annex EU II đã ghép vào bảng
        if (dm.Ma === "syt-cam") {
            $.each(lienKetTheoSyt[khoaDong(dm, dong)] || [], function (j, lk) {
                var eu = lk.khoa ? dongEU[lk.khoa] : null;
                if (eu) phan.push(layO(eu.dm, eu.dong, "TenChat"), layO(eu.dm, eu.dong, "CAS"), layO(eu.dm, eu.dong, "EC"));
            });
        }
        bo[i] = phan.join("\n").toLowerCase();
        return bo[i];
    }

    function khopTim(dm, i) {
        return !giaoDien.tim || chuoiTim(dm, i).indexOf(giaoDien.tim) >= 0;
    }

    // bộ lọc mặc định: ASEAN ẩn dòng Đã loại bỏ; danh mục khác không có bộ lọc
    function layLoc(dm) {
        if (giaoDien.loc[dm.Ma] !== undefined) return giaoDien.loc[dm.Ma];
        var loc = null;
        if (dm.Nhom === "ASEAN") {
            loc = {};
            $.each(DS_TRANG_THAI, function (i, tt) { loc[tt.giaTri] = (tt.giaTri !== TRANG_THAI_LOAI_BO); });
        }
        giaoDien.loc[dm.Ma] = loc;
        return loc;
    }

    function quaLoc(dm, dong, loc) {
        if (dm.Nhom === "ASEAN") return !!loc[layO(dm, dong, "TrangThaiHieuLuc")];
        return true;
    }

    function laySapXep(dm) {
        if (!giaoDien.sapXep[dm.Ma]) giaoDien.sapXep[dm.Ma] = { cot: dm.Nhom === "SYT" ? "Ten" : "RefNo", chieu: 1 };
        return giaoDien.sapXep[dm.Ma];
    }

    function timCot(dm, ma) {
        for (var i = 0; i < dm.dsCot.length; i++)
            if (dm.dsCot[i].ma === ma) return dm.dsCot[i];
        return null;
    }

    // lọc theo từ khóa + nút lọc, đếm số dòng cho từng nút lọc, rồi sắp xếp
    function tinhKetQua(dm) {
        var loc = layLoc(dm), dsTim = [], dsLoc = [];
        dm.demLoc = {};
        for (var i = 0; i < dm.DSDong.length; i++) {
            if (!khopTim(dm, i)) continue;
            dsTim.push(i);
            var dong = dm.DSDong[i];
            if (dm.Nhom === "ASEAN") {
                var nhom = layO(dm, dong, "TrangThaiHieuLuc");
                dm.demLoc[nhom] = (dm.demLoc[nhom] || 0) + 1;
            }
            if (quaLoc(dm, dong, loc)) dsLoc.push(i);
        }

        var sx = laySapXep(dm), cot = timCot(dm, sx.cot);
        if (cot && cot.gt) {
            var soSanh = (cot.sx === "ref") ? soSanhTuNhien : soSanhChu;
            dsLoc.sort(function (a, b) {
                var kq = soSanh(cot.gt(dm, dm.DSDong[a]), cot.gt(dm, dm.DSDong[b]));
                return (kq * sx.chieu) || (a - b);
            });
        }
        ketQua.ds = dsLoc;
        ketQua.demTim = dsTim.length;
    }

    // số dòng khớp từ khóa của một danh mục (thanh trái), không tính dòng ASEAN đã loại bỏ
    function demKhop(dm) {
        var n = 0;
        for (var i = 0; i < dm.DSDong.length; i++)
            if (!laLoaiBo(dm, dm.DSDong[i]) && khopTim(dm, i)) n++;
        return n;
    }

    // ===================== Vẽ giao diện =====================
    function veThanhTrai() {
        var html = "";
        $.each(DS_NHOM, function (i, nhom) {
            html += '<div class="dm-group"><div class="dm-group-title">' + nhom.ten + '<span class="dm-hit-label an">KẾT QUẢ</span></div>';
            $.each(duLieu.DSDanhMuc, function (j, dm) {
                if (dm.Nhom !== nhom.ma) return;
                var ten = maHoa(dm.TenNgan);
                if (dm.Annex) ten = "Annex " + dm.Annex + " <small>· " + ten + "</small>";
                // chữ màu theo loại danh mục (cấm đỏ, giới hạn cam, cho phép xanh), không dùng chấm tròn
                html += '<button type="button" class="dm-item ' + (MAU_DANH_MUC[dm.Ma] || "gioihan") + (dm.Ma === giaoDien.ma ? " active" : "") +
                    '" data-ma="' + dm.Ma + '">' +
                    '<span class="dm-name">' + ten + "</span>" +
                    '<span class="dm-count">' + dinhDangSo(dm.soDong) + "</span></button>";
            });
            html += "</div>";
        });
        $("#ds-danh-muc").html(html);
    }

    // đang tìm: thay số dòng bằng số dòng khớp của từng danh mục
    function veSoKhop() {
        var dangTim = !!giaoDien.tim;
        $("#ds-danh-muc .dm-hit-label").toggleClass("an", !dangTim);
        $("#ds-danh-muc .dm-item").each(function () {
            var dm = theoMa[$(this).data("ma")];
            var $dem = $(this).find(".dm-count, .dm-hit");
            if (!dangTim) {
                $(this).removeClass("zero");
                $dem.attr("class", "dm-count").text(dinhDangSo(dm.soDong));
                return;
            }
            var n = demKhop(dm);
            dm.soKhop = n;
            $(this).toggleClass("zero", n === 0);
            $dem.attr("class", "dm-hit" + (n === 0 ? " zero" : "")).text(dinhDangSo(n));
        });
    }

    function veDauTrang() {
        var dm = theoMa[giaoDien.ma];
        var tenNhom = "";
        $.each(DS_NHOM, function (i, nhom) { if (nhom.ma === dm.Nhom) tenNhom = nhom.ten; });
        var vd = (dm.Nhom === "SYT") ? "Tìm tên chất, ref…" : (dm.Ma === "a-4" ? "Tìm Colour Index, màu…" : "Tìm tên chất, CAS, ref…");
        var html = '<div class="dm-title-row"><div>' +
            '<div class="dm-kicker">' + tenNhom + "</div>" +
            '<h1 class="dm-title">' + maHoa(dm.TieuDe) + "</h1>" +
            (dm.TenTiengAnh ? '<div class="dm-sub">' + maHoa(dm.TenTiengAnh) + "</div>" : "") +
            '</div><div class="dm-head-actions">' + nutChuThich(dm) + '<button type="button" class="dm-btn" id="nut-cot"></button></div></div>' +
            '<div class="dm-toolbar">' +
            '<div class="dm-search">' + ICON_SEARCH +
            '<input type="text" id="o-tim" autocomplete="off" placeholder="' + vd + '" />' +
            '<button type="button" class="dm-search-clear an" id="nut-xoa-tim" title="Xóa từ khóa (Esc)">×</button>' +
            '<span class="dm-scope" id="pham-vi-tim">' +
            '<button type="button" data-phamvi="chinh">Tên · CAS · Ref</button>' +
            '<button type="button" data-phamvi="tatca" title="Tìm cả trong nội dung giới hạn, cảnh báo, ghi chú…">Mọi cột</button></span></div>' +
            '<div class="dm-chips" id="nut-loc"></div>' +
            '<div class="dm-pager" id="phan-trang-tren"></div></div>';
        $("#dau-danh-muc").html(html);
        $("#o-tim").val(giaoDien.timGoc || "");
        $("#nut-xoa-tim").toggleClass("an", !giaoDien.tim);
        vePhamVi();
        veNutCot();
    }

    // nút "Chú thích Annex" (chỉ Annex ASEAN có lời dẫn hoặc chú thích)
    function nutChuThich(dm) {
        var ct = layChuThich(dm);
        if (!ct || !(ct.dau.length + ct.cuoi.length)) return "";
        return '<button type="button" class="dm-btn" id="nut-chu-thich">' + ICON_CHU_THICH + ' Chú thích Annex <span class="n">· ' +
            (ct.dau.length + ct.cuoi.length) + "</span></button>";
    }

    function vePhamVi() {
        $("#pham-vi-tim button").each(function () {
            $(this).toggleClass("on", $(this).data("phamvi") === giaoDien.phamVi);
        });
    }

    function veNutCot() {
        var dm = theoMa[giaoDien.ma], n = soCotAn(dm);
        $("#nut-cot").html(ICON_COT + " Cột" + (n ? ' <span class="n">· ' + n + " ẩn</span>" : ""))
            .toggleClass("active", $("#menu-cot").length > 0);
    }

    function veNutLoc() {
        var dm = theoMa[giaoDien.ma], loc = layLoc(dm), html = "";
        if (dm.Nhom === "ASEAN") {
            $.each(DS_TRANG_THAI, function (i, tt) {
                var co = $.grep(dm.DSDong, function (dong) { return layO(dm, dong, "TrangThaiHieuLuc") === tt.giaTri; }).length;
                if (co) html += nutLoc(tt.giaTri, tt.nhan, dm.demLoc[tt.giaTri] || 0, !!loc[tt.giaTri], true);
            });
        }
        $("#nut-loc").html(html);
    }

    function nutLoc(giaTri, nhan, so, bat, coO) {
        return '<button type="button" class="dm-chip' + (bat ? " on" : "") + '" data-loc="' + maHoa(giaTri) + '">' +
            (coO ? '<span class="dm-chip-check">' + (bat ? ICON_TICK : "") + "</span>" : "") +
            maHoa(nhan) + ' <span class="n">· ' + dinhDangSo(so) + "</span></button>";
    }

    function soTrang() {
        return Math.max(1, Math.ceil(ketQua.ds.length / giaoDien.soDong));
    }

    function vePhanTrang() {
        var tong = ketQua.ds.length, trang = giaoDien.trang, soT = soTrang();
        var dau = tong ? (trang - 1) * giaoDien.soDong + 1 : 0;
        var cuoi = Math.min(tong, trang * giaoDien.soDong);
        var thongTin = '<span class="dm-pager-info"><b>' + dinhDangSo(dau) + "-" + dinhDangSo(cuoi) + "</b> / " + dinhDangSo(tong) + "</span>";

        $("#phan-trang-tren").html(tong ? thongTin +
            nutTrang(trang - 1, ICON_TRAI, trang <= 1, "Trang trước") +
            '<span class="dm-pg-text">Trang ' + trang + "/" + soT + "</span>" +
            nutTrang(trang + 1, ICON_PHAI, trang >= soT, "Trang sau") : "");

        if (!tong) {
            $("#phan-trang-duoi").html("");
            return;
        }
        var html = thongTin + nutTrang(trang - 1, ICON_TRAI, trang <= 1, "Trang trước");
        var truoc = 0;
        for (var p = 1; p <= soT; p++) {
            if (p !== 1 && p !== soT && Math.abs(p - trang) > 2) continue;
            if (truoc && p - truoc > 1) html += '<span class="dm-pg off">…</span>';
            html += '<button type="button" class="dm-pg' + (p === trang ? " on" : "") + '" data-trang="' + p + '">' + p + "</button>";
            truoc = p;
        }
        html += nutTrang(trang + 1, ICON_PHAI, trang >= soT, "Trang sau");
        html += '<label class="dm-per-page">Số dòng mỗi trang <select id="chon-so-dong">' +
            $.map(DS_SO_DONG, function (n) { return '<option value="' + n + '"' + (n === giaoDien.soDong ? " selected" : "") + ">" + n + "</option>"; }).join("") +
            "</select></label>";
        $("#phan-trang-duoi").html(html);
    }

    function nutTrang(p, icon, tat, ten) {
        return '<button type="button" class="dm-pg' + (tat ? " off" : "") + '"' + (tat ? " disabled" : ' data-trang="' + p + '"') +
            ' title="' + ten + '">' + icon + "</button>";
    }

    function veBang() {
        var dm = theoMa[giaoDien.ma];
        dongChuThichNho();
        if (!ketQua.ds.length) {
            veKhongCo(dm);
            return;
        }
        var dsCot = $.grep(dm.dsCot, function (c) { return !c.gopVao && cotHien(dm, c); });
        var dsPhu = $.grep(dm.dsCot, function (c) { return c.gopVao && cotHien(dm, c); });
        var sx = laySapXep(dm);

        var html = '<div class="dm-card"><table class="dm-table"><colgroup>';
        $.each(dsCot, function (i, c) { html += c.rong ? '<col style="width:' + c.rong + 'px">' : "<col>"; });
        html += "</colgroup><thead><tr>";
        $.each(dsCot, function (i, c) {
            var lop = (c.canGiua ? "center " : "") + (c.gt ? "dm-th-sort " : "") + (sx.cot === c.ma ? "sort-on" : "");
            var mui = (sx.cot === c.ma) ? (sx.chieu === 1 ? " ▲" : " ▼") : "";
            html += '<th class="' + lop + '"' + (c.gt ? ' data-sapxep="' + c.ma + '"' : "") +
                (c.goiY ? ' title="' + maHoa(c.goiY) + '"' : "") + ">" + maHoa(c.tieuDe) + mui + "</th>";
        });
        html += "</tr></thead><tbody>";

        var dau = (giaoDien.trang - 1) * giaoDien.soDong;
        var cuoi = Math.min(ketQua.ds.length, dau + giaoDien.soDong);
        for (var k = dau; k < cuoi; k++) {
            var i = ketQua.ds[k], dong = dm.DSDong[i];
            html += '<tr data-dong="' + i + '"' + (laLoaiBo(dm, dong) ? ' class="row-removed"' : "") + ">";
            $.each(dsCot, function (j, c) {
                var o = c.o(dm, dong, k + 1);
                $.each(dsPhu, function (n, p) { if (p.gopVao === c.ma) o += p.o(dm, dong); });
                html += "<td" + (c.canGiua ? ' class="center"' : (c.laStt ? ' class="num"' : "")) + ">" + o + "</td>";
            });
            html += "</tr>";
        }
        html += '</tbody></table></div><div class="dm-pager dm-pager-duoi" id="phan-trang-duoi"></div>';
        $("#noi-dung-danh-muc").html(html);
    }

    // không có dòng nào: đang tìm thì gợi ý các danh mục khác có kết quả
    function veKhongCo(dm) {
        var html = '<div class="dm-empty"><div class="pane-empty-icon">' + ICON_KHONG_CO + "</div>";
        if (giaoDien.tim && ketQua.demTim === 0) {
            html += "<h2>Không có \"" + maHoa(giaoDien.timGoc) + "\" trong " + maHoa(tenDayDu(dm)) + "</h2>";
            var dsKhac = $.grep(duLieu.DSDanhMuc, function (x) { return x.Ma !== dm.Ma && x.soKhop > 0; });
            if (dsKhac.length) {
                html += "<p>Có kết quả ở các danh mục khác:</p><div class=\"dm-other-hits\">";
                $.each(dsKhac, function (i, x) {
                    html += '<button type="button" class="dm-other-hit" data-ma="' + x.Ma + '">' +
                        maHoa(tenDayDu(x)) + ' <span class="dm-hit">' + dinhDangSo(x.soKhop) + "</span></button>";
                });
                html += "</div>";
            } else {
                html += "<p>Không danh mục nào có kết quả. Thử từ khóa ngắn hơn hoặc chọn \"Mọi cột\".</p>";
            }
        } else {
            html += "<h2>Không có dòng nào</h2><p>Có " + dinhDangSo(ketQua.demTim) + " dòng bị ẩn bởi bộ lọc. Bấm các nút lọc phía trên để hiện thêm.</p>";
        }
        $("#noi-dung-danh-muc").html(html + "</div>");
    }

    function tenDayDu(dm) {
        return TEN_NHOM_NGAN[dm.Nhom] + " · " + (dm.Annex ? "Annex " + dm.Annex : dm.TenNgan);
    }

    // tính lại và vẽ phần phụ thuộc kết quả (giữ nguyên ô tìm để không mất con trỏ)
    function capNhat(veLaiTrai) {
        var dm = theoMa[giaoDien.ma];
        tinhKetQua(dm);
        if (giaoDien.trang > soTrang()) giaoDien.trang = soTrang();
        if (veLaiTrai) veSoKhop();
        veNutLoc();
        veBang();
        vePhanTrang();
    }

    function chonDanhMuc(ma) {
        if (!theoMa[ma]) return;
        dongMenuCot();
        dongNganChuThich();
        giaoDien.ma = ma;
        giaoDien.trang = 1;
        ghiLuu();
        $("#ds-danh-muc .dm-item").removeClass("active").filter('[data-ma="' + ma + '"]').addClass("active");
        veDauTrang();
        capNhat(false);
        $("#khung-cuon").scrollTop(0);
    }

    // ===================== Menu Cột =====================
    function moMenuCot() {
        var dm = theoMa[giaoDien.ma];
        var html = '<div class="dm-popover" id="menu-cot"><div class="dm-pop-head">Cột hiển thị</div><div class="dm-pop-body">';
        $.each(dm.dsCot, function (i, c) {
            if (c.laStt || (dm.Ma === "a-4" && /^F\d$/.test(c.ma))) return;
            var hien = cotHien(dm, c);
            html += '<label class="dm-menu-item' + (c.khoa ? " khoa" : "") + '"><input type="checkbox" data-cot="' + c.ma + '"' +
                (hien ? " checked" : "") + (c.khoa ? " disabled" : "") + " /> " + maHoa(tenTrongMenu(c)) + "</label>";
        });
        html += '</div><div class="dm-pop-foot"><button type="button" id="cot-mac-dinh">Về mặc định</button>' +
            '<button type="button" id="cot-tat-ca">Hiện tất cả</button></div></div>';
        $(".result-pane").append(html);
        veNutCot();
    }

    // tên cột trong menu; cột phụ ghi rõ hiện dưới cột nào
    function tenTrongMenu(c) {
        if (c.gopVao) return c.tieuDe + " (dưới " + (c.gopVao === "CAS" ? "CAS" : "tên chất") + ")";
        return c.tieuDe;
    }

    function dongMenuCot() {
        if (!$("#menu-cot").length) return;
        $("#menu-cot").remove();
        veNutCot();
    }

    function doiCot(ma, hien) {
        var dm = theoMa[giaoDien.ma];
        if (!giaoDien.cot[dm.Ma]) giaoDien.cot[dm.Ma] = {};
        giaoDien.cot[dm.Ma][ma] = hien;
        ghiLuu();
        veNutCot();
        veBang();
        vePhanTrang();
    }

    // ===================== Chú thích Annex ASEAN =====================
    // số chú thích trong nội dung: (8), (2.1), (1*)
    function mauSoChuThich() {
        return /\((\d+(?:\.\d+)?\*?)\)/g;
    }

    // lời dẫn (dau) và chú thích cuối (cuoi) của một Annex ASEAN, theoSo: số -> nội dung
    function layChuThich(dm) {
        if (!dm || dm.Nhom !== "ASEAN") return null;
        if (dm.chuThich) return dm.chuThich;
        var ct = { dau: [], cuoi: [], theoSo: {} };
        $.each(duLieu.DSChuThich, function (i, x) {
            if (x.Annex !== dm.Annex) return;
            if (x.Loai === "Preamble") {
                ct.dau.push(x);
            } else {
                ct.cuoi.push(x);
                ct.theoSo[x.So] = x.NoiDung;
            }
        });
        dm.chuThich = ct;
        return ct;
    }

    // "(8)" trong nội dung ASEAN thành nút xem chú thích (chỉ khi Annex có chú thích số đó)
    function ganChuThich(html, dm) {
        var ct = layChuThich(dm);
        if (!ct || !ct.cuoi.length) return html;
        return html.replace(mauSoChuThich(), function (toan, so) {
            return ct.theoSo[so] ? '<button type="button" class="dm-note-ref" data-so="' + so + '">' + toan + "</button>" : toan;
        });
    }

    // các số chú thích xuất hiện trong một dòng ASEAN (không trùng, theo thứ tự gặp)
    function soChuThichCuaDong(dm, dong) {
        var ct = layChuThich(dm), ds = [], m;
        var chu = $.map(["TenChat", "PhamVi", "GioiHan", "Khac", "CanhBao"], function (c) { return layO(dm, dong, c) || ""; }).join("\n");
        var mau = mauSoChuThich();
        while ((m = mau.exec(chu)) !== null)
            if (ct.theoSo[m[1]] && $.inArray(m[1], ds) < 0) ds.push(m[1]);
        return ds;
    }

    // số chú thích -> các dòng đang dùng (tính một lần)
    function dongDungChuThich(dm) {
        var ct = layChuThich(dm);
        if (ct.dungO) return ct.dungO;
        ct.dungO = {};
        $.each(dm.DSDong, function (i, dong) {
            if (laLoaiBo(dm, dong)) return;
            $.each(soChuThichCuaDong(dm, dong), function (j, so) {
                (ct.dungO[so] = ct.dungO[so] || []).push(i);
            });
        });
        return ct.dungO;
    }

    // ref ngắn gọn (dòng đầu), Annex IV thêm "CI "
    function refNgan(dm, dong) {
        var ref = (layO(dm, dong, "RefNo") || "").split("\n")[0];
        return (dm.Ma === "a-4" ? "CI " : "") + ref;
    }

    // bấm "(8)" trong bảng: hộp nhỏ ngay cạnh nút
    function moChuThichNho($nut) {
        dongChuThichNho();
        var dm = theoMa[giaoDien.ma], so = String($nut.data("so")), ct = layChuThich(dm);
        if (!ct || !ct.theoSo[so]) return;
        var $khung = $(".result-pane");
        $khung.append('<div class="dm-popover dm-note-pop" id="chu-thich-nho">' +
            '<div class="dm-pop-head">Chú thích (' + maHoa(so) + ") · Annex " + maHoa(dm.Annex) +
            '<button type="button" class="dm-pop-close" aria-label="Đóng">×</button></div>' +
            '<div class="dm-pop-body dm-text">' + maHoa(ct.theoSo[so]) + "</div>" +
            '<div class="dm-pop-foot"><button type="button" class="nut-mo-ngan" data-tab="cuoi">Xem tất cả chú thích Annex ' + maHoa(dm.Annex) + "</button></div></div>");
        var $hop = $("#chu-thich-nho");
        var nut = $nut[0].getBoundingClientRect(), khung = $khung[0].getBoundingClientRect();
        var trai = Math.max(16, Math.min(nut.left - khung.left - 20, khung.width - $hop.outerWidth() - 16));
        var tren = nut.bottom - khung.top + 6;
        if (tren + $hop.outerHeight() > khung.height - 8) tren = nut.top - khung.top - $hop.outerHeight() - 6;
        $hop.css({ left: trai, top: tren, right: "auto" });
        $nut.addClass("on");
    }

    function dongChuThichNho() {
        $("#chu-thich-nho").remove();
        $(".dm-note-ref.on").removeClass("on");
    }

    // ngăn bên phải: lời dẫn hoặc toàn bộ chú thích của Annex đang xem
    function moNganChuThich(tab) {
        var dm = theoMa[giaoDien.ma], ct = layChuThich(dm);
        if (!ct) return;
        dongChuThichNho();
        dongMenuCot();
        if (tab !== "dau" && tab !== "cuoi") tab = ct.dau.length ? "dau" : "cuoi";
        var ds = (tab === "dau") ? ct.dau : ct.cuoi;
        var dungO = (tab === "cuoi") ? dongDungChuThich(dm) : {};

        var html = '<div class="dm-drawer" id="ngan-chu-thich"><div class="dm-drawer-head"><div>' +
            '<h3 class="dm-drawer-title">Chú thích Annex ' + maHoa(dm.Annex) + "</h3>" +
            '<div class="dm-drawer-sub">ANNEX ASEAN · ' + maHoa(dm.TenTiengAnh || "") + "</div></div>" +
            '<button type="button" class="modal-close" id="dong-ngan" aria-label="Đóng">×</button></div>' +
            '<div class="dm-sub-tabs">' +
            '<button type="button" class="dm-sub-tab' + (tab === "dau" ? " active" : "") + '" data-tab="dau">Lời dẫn (Preamble) · ' + ct.dau.length + "</button>" +
            '<button type="button" class="dm-sub-tab' + (tab === "cuoi" ? " active" : "") + '" data-tab="cuoi">Chú thích · ' + ct.cuoi.length + "</button></div>" +
            '<div class="dm-drawer-body">';
        if (!ds.length) html += '<p class="dm-trong">Annex ' + maHoa(dm.Annex) + " không có " + (tab === "dau" ? "lời dẫn." : "chú thích.") + "</p>";
        $.each(ds, function (i, x) {
            var dung = dungO[x.So] || [];
            html += '<div class="dm-note-item"><div class="dm-text">' + maHoa(x.NoiDung) + "</div>";
            if (dung.length) {
                html += '<div class="dm-note-used">Dùng ở ref: ' + $.map(dung.slice(0, 15), function (iDong) {
                    return '<button type="button" class="ref-btn" data-dong="' + iDong + '">' + maHoa(refNgan(dm, dm.DSDong[iDong])) + "</button>";
                }).join(" ") + (dung.length > 15 ? " … " + dung.length + " ref" : "") + "</div>";
            }
            html += "</div>";
        });
        html += "</div></div>";
        $("#ngan-chu-thich").remove();
        $(".result-pane").append(html);
        $("#nut-chu-thich").addClass("active");
    }

    function dongNganChuThich() {
        $("#ngan-chu-thich").remove();
        $("#nut-chu-thich").removeClass("active");
    }

    // ===================== Hộp chi tiết =====================
    function oTruong(nhan, giaTri) {
        var co = !!(giaTri && String(giaTri).replace(/\s+/g, ""));
        return '<div class="field"><div class="field-label">' + nhan + "</div>" +
            '<div class="field-value' + (co ? "" : " field-empty") + '">' + (co ? maHoa(giaTri) : "Không có dữ liệu") + "</div></div>";
    }

    // trường chỉ hiện khi danh mục có cột đó
    function oTruongCo(dm, dong, cot, nhan) {
        return (dm.viTri[cot] === undefined) ? "" : oTruong(nhan, layO(dm, dong, cot));
    }

    function dauHop(tieuDe, phu, them) {
        return '<div class="modal-head"><div><h3 class="modal-title">' + maHoa(tieuDe) + "</h3>" +
            '<div class="modal-sub">' + phu + "</div>" + (them || "") +
            '</div><button type="button" class="modal-close" aria-label="Đóng">×</button></div>';
    }

    function moHop(html) {
        dongChuThichNho();
        $("#noi-dung-hop-dm").html(html);
        $("#hop-chi-tiet-dm").removeClass("an").scrollTop(0);
        $("body").addClass("modal-open");
    }

    function dongHop() {
        $("#hop-chi-tiet-dm").addClass("an");
        $("body").removeClass("modal-open");
    }

    function hopDangMo() {
        return !$("#hop-chi-tiet-dm").hasClass("an");
    }

    // phần Annex EU của hộp chi tiết: mọi cột của dòng, kể cả cột đang ẩn trên bảng
    function noiDungEU(dm, dong) {
        var html = '<div class="field-row">' + oTruong("REF. NO", dm.Annex + "/" + (layO(dm, dong, "RefNo") || "")) +
            oTruongCo(dm, dong, "CAS", "CAS NUMBER") + oTruongCo(dm, dong, "EC", "EC NUMBER") + oTruongCo(dm, dong, "Mau", "COLOR") + "</div>";
        html += oTruongCo(dm, dong, "TenINCI", dm.Annex === "IV" ? "COLOUR INDEX NUMBER / NAME OF COMMON INGREDIENTS GLOSSARY" : "NAME OF COMMON INGREDIENTS GLOSSARY");
        html += oTruongCo(dm, dong, "PhamVi", "PRODUCT TYPE, BODY PARTS");
        html += oTruongCo(dm, dong, "GioiHan", "MAXIMUM CONCENTRATION IN READY FOR USE PREPARATION");
        html += oTruongCo(dm, dong, "Khac", "OTHER");
        html += oTruongCo(dm, dong, "CanhBao", "WORDING OF CONDITIONS OF USE AND WARNINGS");
        html += '<div class="field-row">' + oTruong("REGULATION", layO(dm, dong, "Regulation")) +
            oTruong("UPDATE DATE", layO(dm, dong, "NgayCapNhat")) + oTruong("CMR", layO(dm, dong, "CMR")) + "</div>";
        html += oTruong("CHEMICAL/IUPAC NAME", layO(dm, dong, "IUPAC"));
        html += oTruong("IDENTIFIED INGREDIENTS OR SUBSTANCES", layO(dm, dong, "ThanhPhanNhanDien"));
        html += oTruong("OTHER DIRECTIVES/REGULATIONS", layO(dm, dong, "ChiThiKhac"));
        html += oTruong("SCCS OPINIONS", layO(dm, dong, "SCCS"));
        return html;
    }

    // phần danh mục Sở Y tế của hộp chi tiết
    function noiDungSyt(syt) {
        var dm = syt.dm, dong = syt.dong;
        var loai = (LOAI_SYT[dm.Bang] || { ten: dm.TenNgan }).ten;
        return '<div class="field-row">' + oTruong("TÊN CHẤT", layO(dm, dong, "Ten")) + oTruong("LOẠI DANH MỤC", loai) +
            oTruong("MÔ TẢ", layO(dm, dong, "MoTa")) + "</div>" +
            oTruong("THÔNG TIN", layO(dm, dong, "ThongTin")) + oTruong("GHI CHÚ", layO(dm, dong, "GhiChu"));
    }

    // khoaSyt có giá trị: mở từ ô ref của chất SYT (giống hộp chi tiết EU của trang Tra cứu); không có: mở từ bảng Annex EU
    function moHopEU(khoaEU, khoaSyt) {
        var eu = dongEU[khoaEU];
        if (!eu) return;
        var dm = eu.dm, dong = eu.dong, ref = dm.Annex + "/" + (layO(dm, dong, "RefNo") || "");
        var syt = khoaSyt ? dongSyt[khoaSyt] : null;
        var html;

        if (syt) {
            var dsRef = $.map(lienKetTheoSyt[khoaSyt] || [], function (lk) {
                if (!lk.khoa) return '<span class="ref-btn missing" title="Tham chiếu này không có trong Annex EU">' + maHoa(lk.ref) + "</span>";
                return '<button type="button" class="ref-btn' + (lk.khoa === khoaEU ? " ref-active" : "") + '" data-khoa-eu="' + maHoa(lk.khoa) +
                    '" data-khoa-syt="' + maHoa(khoaSyt) + '">' + maHoa(lk.ref) + "</button>";
            }).join(" ");
            html = dauHop(layO(syt.dm, syt.dong, "Ten"),
                "Annex EU " + maHoa(ref) + " · Danh mục: " + maHoa(((LOAI_SYT[syt.dm.Bang] || {}).ten || "") + " (EU)"),
                '<div class="modal-refs">Tham chiếu của chất này: ' + dsRef + "</div>") +
                '<div class="modal-body"><div class="modal-section">DANH MỤC EU</div>' + noiDungSyt(syt) +
                '<div class="modal-section">ANNEX EU ' + maHoa(ref) + "</div>" +
                oTruong("CHEMICAL NAME / INN", layO(dm, dong, "TenChat")) + noiDungEU(dm, dong) + "</div>";
        } else {
            var phu = "Annex EU " + maHoa(ref);
            if (layO(dm, dong, "TenINCI")) phu += " · INCI: " + maHoa(layO(dm, dong, "TenINCI"));
            var dsSyt = sytTheoEU[khoaEU] || [];
            html = dauHop(layO(dm, dong, "TenChat") || ref, phu) + '<div class="modal-body">' + noiDungEU(dm, dong) +
                '<div class="modal-section">DANH MỤC EU LIÊN KẾT · ' + dsSyt.length + "</div>";
            if (!dsSyt.length) html += '<div class="field"><div class="field-value field-empty">Không có chất nào trong Danh mục EU tham chiếu tới ref này</div></div>';
            $.each(dsSyt, function (i, x) {
                var s = dongSyt[x.bang + "#" + x.id];
                if (!s) return;
                var loai = LOAI_SYT[x.bang] || { ten: x.bang, lop: "gioihan" };
                html += '<div class="field"><div class="dm-link-row"><span class="dm-tag ' + loai.lop + '">' + loai.ten + "</span>" +
                    '<span class="cell-name">' + maHoa(layO(s.dm, s.dong, "Ten")) + "</span>" +
                    '<span class="dm-trong">Mô tả: ' + maHoa(layO(s.dm, s.dong, "MoTa") || "-") + "</span>" +
                    '<button type="button" class="dm-more dm-mo-syt" data-ma="' + loai.ma + '" data-ten="' + maHoa(layO(s.dm, s.dong, "Ten")) + '">Mở trong danh mục ›</button></div></div>';
            });
            html += "</div>";
        }
        moHop(html);
    }

    // chất SYT không có ref nào khớp Annex EU: chỉ hiện phần Sở Y tế
    function moHopSyt(khoaSyt) {
        var syt = dongSyt[khoaSyt];
        if (!syt) return;
        moHop(dauHop(layO(syt.dm, syt.dong, "Ten"), "Danh mục: " + maHoa(((LOAI_SYT[syt.dm.Bang] || {}).ten || "") + " (EU)")) +
            '<div class="modal-body"><div class="modal-section">DANH MỤC EU</div>' + noiDungSyt(syt) + "</div>");
    }

    function moHopAsean(dm, iDong) {
        var dong = dm.DSDong[iDong];
        if (!dong) return;
        var tt = timTrangThai(layO(dm, dong, "TrangThaiHieuLuc"));
        var tieuDe = (dm.Annex === "IV") ? "Colour Index " + refNgan(dm, dong).replace(/^CI /, "") + (layO(dm, dong, "Mau") ? " (" + layO(dm, dong, "Mau") + ")" : "")
            : layO(dm, dong, "TenChat");
        var html = dauHop(tieuDe, "ANNEX ASEAN " + maHoa(dm.Annex) + " · Ref " + maHoa(refNgan(dm, dong)) +
            ' · <span class="dm-tag ' + tt.lop + '">' + maHoa(tt.nhan) + "</span>") + '<div class="modal-body">';

        if (dm.Annex === "II") {
            html += oTruong("CAS NUMBER", layO(dm, dong, "CAS"));
        } else if (dm.Annex === "IV") {
            var dsPhamVi = [];
            $.each(["F1", "F2", "F3", "F4"], function (i, f) {
                var cot = timCot(dm, f);
                if (layO(dm, dong, f)) dsPhamVi.push((i + 1) + ". " + cot.goiY);
            });
            html += oTruong("FIELD OF APPLICATION", dsPhamVi.join("\n"));
            html += oTruong("OTHER LIMITATIONS AND REQUIREMENTS", layO(dm, dong, "Khac"));
        } else {
            html += oTruongCo(dm, dong, "PhamVi", "FIELD OF APPLICATION AND/OR USE");
            html += oTruong("MAXIMUM AUTHORISED CONCENTRATION IN THE FINISHED COSMETIC PRODUCT", layO(dm, dong, "GioiHan"));
            html += oTruong("OTHER LIMITATIONS AND REQUIREMENTS", layO(dm, dong, "Khac"));
            html += oTruong("CONDITIONS OF USE AND WARNING WHICH MUST BE PRINTED ON THE LABELS", layO(dm, dong, "CanhBao"));
        }

        html += '<div class="modal-section">CĂN CỨ VÀ HIỆU LỰC</div><div class="field-row">' +
            oTruong("CĂN CỨ", layO(dm, dong, "CanCu")) + oTruong("NGÀY ÁP DỤNG", layO(dm, dong, "NgayApDung")) +
            oTruong("NGÀY HẾT HIỆU LỰC", layO(dm, dong, "NgayHetHieuLuc")) + "</div>" + oTruong("GHI CHÚ", layO(dm, dong, "GhiChu"));

        var dsSo = soChuThichCuaDong(dm, dong), ct = layChuThich(dm);
        if (dsSo.length) {
            html += '<div class="modal-section">CHÚ THÍCH LIÊN QUAN</div>';
            $.each(dsSo, function (i, so) {
                html += '<div class="dm-note-item"><div class="dm-text">' + maHoa(ct.theoSo[so]) + "</div></div>";
            });
        }
        moHop(html + "</div>");
    }

    // bấm vào dòng của bảng đang xem
    function moChiTietDong(iDong) {
        var dm = theoMa[giaoDien.ma], dong = dm.DSDong[iDong];
        if (!dong) return;
        if (dm.Nhom === "ASEAN") {
            moHopAsean(dm, iDong);
        } else if (dm.Nhom === "EU") {
            moHopEU(khoaDong(dm, dong));
        } else {
            var khoaSyt = khoaDong(dm, dong);
            var dsKhop = $.grep(lienKetTheoSyt[khoaSyt] || [], function (lk) { return !!lk.khoa; });
            if (dsKhop.length) moHopEU(dsKhop[0].khoa, khoaSyt);
            else moHopSyt(khoaSyt);
        }
    }

    // ===================== Tải dữ liệu =====================
    function baoLoi(thongBao) {
        $("#ds-danh-muc").html("");
        $("#noi-dung-danh-muc").html('<div class="pane-notice">' + maHoa(thongBao) + "</div>");
    }

    // dựng các chỉ mục tra nhanh: liên kết SYT -> EU, EU -> SYT, dòng EU theo khóa
    function dungChiMuc(kq) {
        theoMa = {};
        lienKetTheoSyt = {};
        sytTheoEU = {};
        dongEU = {};
        dongSyt = {};
        $.each(kq.DSDanhMuc, function (i, dm) {
            dm.viTri = {};
            $.each(dm.DSCot, function (j, cot) { dm.viTri[cot] = j; });
            dm.chuoiTim = {};
            theoMa[dm.Ma] = dm;
            if (dm.Nhom === "EU")
                $.each(dm.DSDong, function (j, dong) { dongEU[khoaDong(dm, dong)] = { dm: dm, dong: dong }; });
            if (dm.Nhom === "SYT")
                $.each(dm.DSDong, function (j, dong) { dongSyt[khoaDong(dm, dong)] = { dm: dm, dong: dong }; });
        });
        $.each(kq.DSLienKet, function (i, lk) {
            var khoaSyt = lk[0] + "#" + lk[1];
            (lienKetTheoSyt[khoaSyt] = lienKetTheoSyt[khoaSyt] || []).push({ ref: lk[2], khoa: lk[3] });
            if (lk[3]) (sytTheoEU[lk[3]] = sytTheoEU[lk[3]] || []).push({ bang: lk[0], id: lk[1] });
        });
        $.each(kq.DSDanhMuc, function (i, dm) {
            dm.dsCot = (dm.Nhom === "SYT") ? dsCotSyt(dm) : (dm.Nhom === "EU" ? dsCotEU(dm) : dsCotAsean(dm));
            dm.soDong = $.grep(dm.DSDong, function (dong) { return !laLoaiBo(dm, dong); }).length;
        });
    }

    function taiDuLieu() {
        $.ajax({
            url: $("#trang-danh-muc").data("url-du-lieu"),
            type: "GET",
            dataType: "json",
            cache: false
        }).done(function (kq) {
            if (!kq || kq.Loi) {
                baoLoi(kq && kq.Loi ? kq.Loi : "Không đọc được dữ liệu danh mục.");
                return;
            }
            duLieu = kq;
            dungChiMuc(kq);
            if (!theoMa[giaoDien.ma]) giaoDien.ma = kq.DSDanhMuc[0].Ma;
            veThanhTrai();
            chonDanhMuc(giaoDien.ma);
        }).fail(function () {
            baoLoi("Lỗi khi tải dữ liệu: không kết nối được máy chủ.");
        });
    }

    // ===================== Sự kiện =====================
    function datTuKhoa(chu) {
        giaoDien.timGoc = chu;
        giaoDien.tim = chuanHoa(chu);
        giaoDien.trang = 1;
        $("#nut-xoa-tim").toggleClass("an", !giaoDien.tim);
        capNhat(true);
    }

    function ganSuKien() {
        $("#ds-danh-muc").on("click", ".dm-item", function () {
            chonDanhMuc($(this).data("ma"));
            // màn hình hẹp: bảng nằm dưới danh sách danh mục, cuộn xuống để thấy bảng vừa chọn
            if (window.matchMedia && window.matchMedia("(max-width: 767px)").matches)
                document.getElementById("dau-danh-muc").scrollIntoView();
        });

        var $dau = $("#dau-danh-muc"), $noiDung = $("#noi-dung-danh-muc");

        // gõ tìm: chờ 200 ms sau phím cuối mới lọc
        $dau.on("input", "#o-tim", function () {
            var chu = $(this).val();
            clearTimeout(henGioTim);
            henGioTim = setTimeout(function () { datTuKhoa(chu); }, 200);
        });
        $dau.on("keydown", "#o-tim", function (e) {
            if (e.key === "Escape" || e.keyCode === 27) {
                $(this).val("");
                clearTimeout(henGioTim);
                datTuKhoa("");
            }
        });
        $dau.on("click", "#nut-xoa-tim", function () {
            $("#o-tim").val("").focus();
            datTuKhoa("");
        });
        $dau.on("click", "#pham-vi-tim button", function () {
            giaoDien.phamVi = $(this).data("phamvi");
            ghiLuu();
            vePhamVi();
            giaoDien.trang = 1;
            capNhat(true);
        });

        // nút lọc: SYT chọn một; ASEAN bật/tắt từng trạng thái
        $dau.on("click", "#nut-loc .dm-chip", function () {
            var dm = theoMa[giaoDien.ma], giaTri = String($(this).data("loc"));
            if (dm.Nhom === "ASEAN") giaoDien.loc[dm.Ma][giaTri] = !giaoDien.loc[dm.Ma][giaTri];
            else giaoDien.loc[dm.Ma] = giaTri || null;
            giaoDien.trang = 1;
            capNhat(false);
        });

        // phân trang (trên và dưới)
        $(".result-pane").on("click", ".dm-pg[data-trang]", function () {
            giaoDien.trang = parseInt($(this).data("trang"), 10);
            veBang();
            vePhanTrang();
            $("#khung-cuon").scrollTop(0);
        });
        $noiDung.on("change", "#chon-so-dong", function () {
            giaoDien.soDong = parseInt($(this).val(), 10);
            giaoDien.trang = 1;
            ghiLuu();
            veBang();
            vePhanTrang();
            $("#khung-cuon").scrollTop(0);
        });

        // bấm tiêu đề cột: sắp xếp, bấm lại thì đảo chiều
        $noiDung.on("click", "th[data-sapxep]", function () {
            var sx = laySapXep(theoMa[giaoDien.ma]), cot = $(this).data("sapxep");
            if (sx.cot === cot) sx.chieu = -sx.chieu;
            else { sx.cot = cot; sx.chieu = 1; }
            giaoDien.trang = 1;
            capNhat(false);
        });

        // xem thêm / thu gọn ô chữ dài
        $noiDung.on("click", ".dm-more", function () {
            var $chu = $(this).prev(".dm-text"), mo = !$chu.hasClass("mo");
            $chu.toggleClass("mo", mo);
            $(this).text(mo ? "thu gọn" : "xem thêm");
        });

        $noiDung.on("click", ".dm-other-hit", function () {
            chonDanhMuc($(this).data("ma"));
        });

        // menu Cột
        $dau.on("click", "#nut-cot", function (e) {
            e.stopPropagation();
            if ($("#menu-cot").length) dongMenuCot();
            else moMenuCot();
        });
        $(".result-pane").on("change", "#menu-cot input[data-cot]", function () {
            doiCot($(this).data("cot"), this.checked);
        });
        $(".result-pane").on("click", "#cot-mac-dinh, #cot-tat-ca", function () {
            var dm = theoMa[giaoDien.ma], tatCa = (this.id === "cot-tat-ca");
            giaoDien.cot[dm.Ma] = {};
            if (tatCa) $.each(dm.dsCot, function (i, c) { giaoDien.cot[dm.Ma][c.ma] = true; });
            ghiLuu();
            dongMenuCot();
            moMenuCot();
            veBang();
            vePhanTrang();
        });
        $(document).on("click", function (e) {
            if ($("#menu-cot").length && !$(e.target).closest("#menu-cot, #nut-cot").length) dongMenuCot();
            if ($("#chu-thich-nho").length && !$(e.target).closest("#chu-thich-nho, .dm-note-ref").length) dongChuThichNho();
        });

        // Esc: đóng lần lượt hộp chi tiết, hộp chú thích nhỏ, menu Cột, ngăn chú thích
        $(document).on("keydown", function (e) {
            if (e.keyCode !== 27) return;
            if (hopDangMo()) dongHop();
            else if ($("#chu-thich-nho").length) dongChuThichNho();
            else if ($("#menu-cot").length) dongMenuCot();
            else dongNganChuThich();
        });

        // ===== Chú thích Annex =====
        var $khung = $(".result-pane");
        $noiDung.on("click", ".dm-note-ref", function (e) {
            e.stopPropagation();
            if ($(this).hasClass("on")) dongChuThichNho();
            else moChuThichNho($(this));
        });
        $khung.on("click", ".dm-pop-close", dongChuThichNho);
        $dau.on("click", "#nut-chu-thich", function () {
            if ($("#ngan-chu-thich").length) dongNganChuThich();
            else moNganChuThich();
        });
        $khung.on("click", ".nut-mo-ngan", function () {
            moNganChuThich($(this).data("tab"));
        });
        $khung.on("click", "#dong-ngan", dongNganChuThich);
        $khung.on("click", "#ngan-chu-thich .dm-sub-tab", function () {
            moNganChuThich($(this).data("tab"));
        });
        $khung.on("click", "#ngan-chu-thich .ref-btn[data-dong]", function () {
            moHopAsean(theoMa[giaoDien.ma], parseInt($(this).data("dong"), 10));
        });

        // ===== Hộp chi tiết =====
        // ô ref của chất SYT: hộp chi tiết EU của đúng ref đó
        $noiDung.on("click", ".ref-btn[data-khoa-eu]", function () {
            var dm = theoMa[giaoDien.ma], i = $(this).closest("tr").data("dong");
            moHopEU($(this).data("khoa-eu"), khoaDong(dm, dm.DSDong[i]));
        });
        // ô ref của dòng EU/ASEAN hoặc bấm vào dòng (trừ khi đang bôi đen chữ hoặc bấm nút khác)
        $noiDung.on("click", ".dm-table tbody tr", function (e) {
            if ($(e.target).closest("button:not(.dm-ref-dong), a").length) return;
            if (window.getSelection && String(window.getSelection()).length) return;
            moChiTietDong($(this).data("dong"));
        });
        $("#hop-chi-tiet-dm").on("click", function (e) {
            if (e.target === this) dongHop();
        });
        $("#hop-chi-tiet-dm").on("click", ".modal-close", dongHop);
        // đổi ref ngay trong hộp
        $("#hop-chi-tiet-dm").on("click", ".ref-btn[data-khoa-eu]", function () {
            moHopEU($(this).data("khoa-eu"), $(this).data("khoa-syt"));
        });
        // "Mở trong danh mục": chuyển sang danh mục SYT và tìm theo tên chất
        $("#hop-chi-tiet-dm").on("click", ".dm-mo-syt", function () {
            var ten = String($(this).data("ten"));
            dongHop();
            giaoDien.timGoc = ten;
            giaoDien.tim = chuanHoa(ten);
            giaoDien.loc[$(this).data("ma")] = null;
            chonDanhMuc($(this).data("ma"));
            veSoKhop();
        });
    }

    // ===================== Khởi động =====================
    $(function () {
        if (!$("#trang-danh-muc").length) return;
        docLuu();
        ganSuKien();
        taiDuLieu();
    });
})(jQuery);
