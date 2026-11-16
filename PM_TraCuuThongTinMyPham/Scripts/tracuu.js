// Trang tra cứu (/TraCuu): grid nhập tên, thùng rác, vẽ kết quả từ JSON (TraCuu/TraDanhSach, TraCuu/ChiTiet)
// Danh sách + thùng rác lưu trong sessionStorage: còn khi F5, mất khi đóng tab (công thức không ở lại trên máy dùng chung)
$(function () {
    var $trang = $("#trang-tra-cuu");
    if (!$trang.length)
        return;

    // khóa lưu danh sách + thùng rác trong sessionStorage của trình duyệt
    var KHOA_LUU = "phasolpro.tracuu.v2";

    // ===== Trạng thái =====
    // dsDong: các dòng theo thứ tự nhập (STT = vị trí + 1); mỗi dòng { id, ten, goiYXoa }
    //   goiYXoa = true khi dòng đã từng được tự tích vì trùng (người dùng bỏ tích thì không tự tích lại)
    // thungRac: các tên đã xóa { id, ten, viTri (vị trí cũ, tính từ 0), lan (mã lượt xóa) }
    var duLieu = {
        dsDong: [],
        thungRac: [],
        idKeTiep: 1,
        lanXoaKeTiep: 1
    };

    // trạng thái giao diện (không lưu)
    var giaoDien = {
        tab: "eu",            // eu / asean
        cheDo: "chinhxac",    // chinhxac / tuongdoi
        sapXep: { cot: "stt", tang: true },
        daChon: {},           // id dòng đang được tích
        chonRac: {},          // id tên trong thùng rác đang được tích
        idDangSua: null,      // id dòng đang sửa tên
        xemEU: "ketqua",      // tab con EU: ketqua / chinhta / chophep / gioihan / cam / khongco
        xemAsean: "ketqua",   // tab con ASEAN: ketqua / annex
        annexChon: "II"       // tab Annex đang mở trong tab con Annex
    };

    // ===== Hàm phụ trợ =====

    // thoát ký tự đặc biệt trước khi chèn chữ vào HTML
    function maHoa(value) {
        return String(value == null ? "" : value)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }

    // bỏ khoảng trắng thừa (kể cả tab, xuống dòng) ở giữa và hai đầu
    function lamGon(value) {
        return String(value == null ? "" : value).replace(/\s+/g, " ").trim();
    }

    // khóa so trùng: không phân biệt hoa thường, bỏ khoảng trắng thừa, dấu nháy cong coi như ' (giống SoKhopTen.ChuanHoa)
    function khoaTen(value) {
        return lamGon(value).replace(/[’‘′´ʼ]/g, "'").toLowerCase();
    }

    // tách đoạn chữ dán vào thành danh sách tên: mỗi dòng một tên, dòng có nhiều ô (Excel) lấy ô đầu tiên không rỗng
    function tachDanhSach(text) {
        var kq = [];
        $.each(String(text || "").split(/\r\n|\r|\n/), function (i, dong) {
            var cacO = dong.split("\t");
            for (var j = 0; j < cacO.length; j++) {
                var ten = lamGon(cacO[j]);
                if (ten !== "") {
                    kq.push(ten);
                    break;
                }
            }
        });
        return kq;
    }

    function timViTri(id) {
        for (var i = 0; i < duLieu.dsDong.length; i++) {
            if (duLieu.dsDong[i].id === id)
                return i;
        }
        return -1;
    }

    // ===== Lưu / đọc sessionStorage (lỗi thì bỏ qua, trang vẫn chạy) =====

    function luuDuLieu() {
        try {
            window.sessionStorage.setItem(KHOA_LUU, JSON.stringify(duLieu));
        } catch (e) {
            // trình duyệt chặn sessionStorage: không lưu được, không ảnh hưởng thao tác
        }
    }

    function docDuLieu() {
        try {
            // bản cũ lưu trong localStorage (còn lại sau khi đóng trình duyệt): xóa đi
            window.localStorage.removeItem(KHOA_LUU);
        } catch (e) {
            // bỏ qua
        }
        try {
            var text = window.sessionStorage.getItem(KHOA_LUU);
            if (!text)
                return;
            var daLuu = JSON.parse(text);
            if (daLuu && $.isArray(daLuu.dsDong) && $.isArray(daLuu.thungRac)) {
                duLieu.dsDong = daLuu.dsDong;
                duLieu.thungRac = daLuu.thungRac;
                duLieu.idKeTiep = daLuu.idKeTiep || 1;
                duLieu.lanXoaKeTiep = daLuu.lanXoaKeTiep || 1;
            }
        } catch (e) {
            // dữ liệu lưu bị hỏng: bắt đầu danh sách trống
        }
    }

    // ===== Phân tích trùng =====

    // trả về { laTrung: {id: true}, soLan: {khoa: số lần}, soDongTrung }; lần xuất hiện đầu (theo thứ tự nhập) không tính là trùng
    function phanTichTrung() {
        var kq = { laTrung: {}, soLan: {}, soDongTrung: 0 };
        $.each(duLieu.dsDong, function (i, dong) {
            var khoa = khoaTen(dong.ten);
            kq.soLan[khoa] = (kq.soLan[khoa] || 0) + 1;
            if (kq.soLan[khoa] > 1) {
                kq.laTrung[dong.id] = true;
                kq.soDongTrung++;
            }
        });
        return kq;
    }

    // dòng trùng mới xuất hiện thì tự tích sẵn để gợi ý xóa (mỗi dòng chỉ tự tích một lần)
    function tuTichDongTrung(trung) {
        $.each(duLieu.dsDong, function (i, dong) {
            if (trung.laTrung[dong.id] && !dong.goiYXoa) {
                dong.goiYXoa = true;
                giaoDien.daChon[dong.id] = true;
            }
        });
    }

    // ===== Vẽ grid =====

    // các dòng theo thứ tự đang hiển thị (STT hoặc tên A→Z / Z→A)
    function layThuTuHienThi() {
        var ds = $.map(duLieu.dsDong, function (dong, i) {
            return { dong: dong, stt: i + 1 };
        });
        var sx = giaoDien.sapXep;
        if (sx.cot === "ten") {
            ds.sort(function (a, b) {
                var kq = a.dong.ten.localeCompare(b.dong.ten, "vi", { sensitivity: "base" });
                if (kq === 0)
                    kq = a.stt - b.stt;
                return sx.tang ? kq : -kq;
            });
        } else if (!sx.tang) {
            ds.reverse();
        }
        return ds;
    }

    function veGrid() {
        var trung = phanTichTrung();
        tuTichDongTrung(trung);

        // bỏ tích các id không còn trong danh sách
        var conLai = {};
        $.each(duLieu.dsDong, function (i, dong) { conLai[dong.id] = true; });
        $.each(giaoDien.daChon, function (id) {
            if (!conLai[id])
                delete giaoDien.daChon[id];
        });

        var html = [];
        $.each(layThuTuHienThi(), function (i, item) {
            var dong = item.dong;
            var laTrung = !!trung.laTrung[dong.id];
            var dangSua = (giaoDien.idDangSua === dong.id);
            var lop = "ig-row" + (laTrung ? " row-dup" : "") + (dangSua ? " row-edit" : "");
            var oTen = dangSua
                ? '<input class="ig-input o-sua-ten" type="text" autocomplete="off" value="' + maHoa(dong.ten) + '" />'
                : '<span class="ig-name" title="Bấm để sửa">' + maHoa(dong.ten) + (laTrung ? '<span class="dup-mark">TRÙNG</span>' : "") + "</span>";
            html.push(
                '<div class="' + lop + '" data-id="' + dong.id + '">' +
                '<span class="ig-chk"><input type="checkbox" class="chon-dong"' + (giaoDien.daChon[dong.id] ? " checked" : "") + " /></span>" +
                '<span class="ig-stt">' + item.stt + "</span>" +
                oTen +
                "</div>");
        });
        $("#luoi-dong").html(html.join(""));

        var soDong = duLieu.dsDong.length;
        $("#dem-thanh-phan").text(soDong);
        $("#luoi-trong").toggleClass("an", soDong > 0);

        // dải vàng báo dòng trùng
        if (trung.soDongTrung > 0) {
            // chỉ ghi "đã tích sẵn" khi mọi dòng trùng đang được tích
            var soTrungDaChon = 0;
            $.each(trung.laTrung, function (id) {
                if (giaoDien.daChon[id])
                    soTrungDaChon++;
            });
            var ghiChuChon = (soTrungDaChon === trung.soDongTrung) ? " - đã tích sẵn để xóa." : ".";
            $("#dai-bao-trung").html("Phát hiện <b>" + trung.soDongTrung + " dòng trùng tên</b>" + ghiChuChon +
                " Nếu giữ lại, tên trùng chỉ tra một lần.").removeClass("an");
        } else {
            $("#dai-bao-trung").addClass("an").empty();
        }

        // nút Xóa đã chọn + ô chọn tất cả
        var soChon = 0;
        $.each(giaoDien.daChon, function () { soChon++; });
        $("#nut-xoa-da-chon").prop("disabled", soChon === 0).text(soChon > 0 ? "Xóa đã chọn · " + soChon : "Xóa đã chọn");
        $("#chon-tat-ca").prop("checked", soDong > 0 && soChon === soDong).prop("indeterminate", soChon > 0 && soChon < soDong);

        // biểu tượng sắp xếp trên tiêu đề cột
        $(".ig-sort").each(function () {
            var cot = $(this).data("sapxep");
            var dangDung = (giaoDien.sapXep.cot === cot);
            $(this).toggleClass("sort-on", dangDung)
                .find(".sort-icon").text(dangDung ? (giaoDien.sapXep.tang ? "▲" : "▼") : "⇅");
        });

        capNhatKhungChuaTra(trung);
    }

    // khung phải lúc chưa tra: báo số thành phần sẵn sàng (đã tra thì khung này bị ẩn)
    function capNhatKhungChuaTra(trung) {
        if (ketQua.daTra)
            return;
        var soDong = duLieu.dsDong.length;
        if (soDong === 0) {
            $("#tieu-de-chua-tra").text("Chưa có kết quả tra cứu");
            $("#mo-ta-chua-tra").text("Kết quả tra cứu theo Danh mục EU và Danh mục ANNEX ASEAN sẽ hiển thị tại đây.");
            return;
        }
        $("#tieu-de-chua-tra").text(soDong + " thành phần sẵn sàng tra cứu");
        var moTa = "Bấm Tra cứu ở cuối thanh bên trái để tra theo Danh mục EU và Danh mục ANNEX ASEAN.";
        if (trung.soDongTrung > 0)
            moTa = "Có " + trung.soDongTrung + " dòng trùng tên. " + moTa;
        $("#mo-ta-chua-tra").text(moTa);
    }

    // ===== Vẽ thùng rác =====

    function veThungRac() {
        var ds = duLieu.thungRac;

        // bỏ tích các id không còn trong thùng rác
        var conLai = {};
        $.each(ds, function (i, item) { conLai[item.id] = true; });
        $.each(giaoDien.chonRac, function (id) {
            if (!conLai[id])
                delete giaoDien.chonRac[id];
        });
        var soChon = 0;
        $.each(giaoDien.chonRac, function () { soChon++; });

        $("#dem-thung-rac").text(ds.length);
        $("#nut-hoan-tac-rac").prop("disabled", soChon === 0).text(soChon > 0 ? "↶ Hoàn tác · " + soChon : "↶ Hoàn tác");
        $("#chon-tat-ca-rac").prop("disabled", ds.length === 0)
            .prop("checked", ds.length > 0 && soChon === ds.length).prop("indeterminate", soChon > 0 && soChon < ds.length);
        if (ds.length === 0) {
            $("#ds-thung-rac").html('<div class="bin-empty">Thùng rác trống</div>');
            return;
        }
        // tên xóa sau cùng nằm trên cùng
        var html = [];
        for (var i = ds.length - 1; i >= 0; i--) {
            var item = ds[i];
            html.push(
                '<label class="bin-row" data-id="' + item.id + '">' +
                '<span class="ig-chk"><input type="checkbox" class="chon-rac"' + (giaoDien.chonRac[item.id] ? " checked" : "") + " /></span>" +
                '<span class="bin-name" title="' + maHoa(item.ten) + '">' + maHoa(item.ten) + "</span>" +
                '<span class="bin-pos">vị trí ' + (item.viTri + 1) + "</span>" +
                "</label>");
        }
        $("#ds-thung-rac").html(html.join(""));
    }

    // vẽ lại cả grid và thùng rác, rồi lưu
    function veLai() {
        veGrid();
        veThungRac();
        luuDuLieu();
    }

    // ===== Thêm / sửa / xóa / hoàn tác =====

    // thêm các tên vào cuối danh sách
    function themTen(dsTen) {
        $.each(dsTen, function (i, ten) {
            ten = lamGon(ten);
            if (ten !== "")
                duLieu.dsDong.push({ id: duLieu.idKeTiep++, ten: ten });
        });
        veLai();
    }

    // lưu tên đang sửa; tên rỗng thì chuyển dòng vào thùng rác. Trả về true nếu tên thay đổi
    function luuTenDangSua() {
        var id = giaoDien.idDangSua;
        if (id === null)
            return false;
        giaoDien.idDangSua = null;

        var viTri = timViTri(id);
        var $o = $("#luoi-dong .o-sua-ten");
        if (viTri < 0 || !$o.length)
            return false;

        var tenMoi = lamGon($o.val());
        var dong = duLieu.dsDong[viTri];
        if (tenMoi === "") {
            xoaDong([id]);
            return true;
        }
        if (tenMoi === dong.ten)
            return false;

        var khoaCu = khoaTen(dong.ten);
        dong.ten = tenMoi;
        // tên đã sửa coi như dòng mới: được tự tích lại nếu trở thành trùng
        dong.goiYXoa = false;
        if (khoaCu !== khoaTen(tenMoi))
            khiSuaTen(dong);
        return true;
    }

    // bắt đầu sửa tên một dòng (bấm 1 lần vào ô tên)
    function batDauSua(id) {
        if (giaoDien.idDangSua !== null && giaoDien.idDangSua !== id)
            luuTenDangSua();
        giaoDien.idDangSua = id;
        veLai();
        var $o = $("#luoi-dong .o-sua-ten");
        $o.focus();
        var dai = $o.val().length;
        $o[0].setSelectionRange(dai, dai);
        cuonToiDong($o.closest(".ig-row"));
    }

    // chuyển các dòng (theo id) vào thùng rác, cùng một lượt xóa để Hoàn tác trên thông báo trả lại cả lượt
    function xoaDong(dsId) {
        if (!dsId.length)
            return;
        var canXoa = {};
        $.each(dsId, function (i, id) { canXoa[id] = true; });

        var lan = duLieu.lanXoaKeTiep++;
        var giuLai = [];
        $.each(duLieu.dsDong, function (viTri, dong) {
            if (canXoa[dong.id]) {
                // vị trí cũ tính trong danh sách trước khi xóa: khôi phục theo thứ tự tăng dần sẽ về đúng chỗ
                duLieu.thungRac.push({ id: dong.id, ten: dong.ten, viTri: viTri, lan: lan });
                delete giaoDien.daChon[dong.id];
            } else {
                giuLai.push(dong);
            }
        });
        var soXoa = duLieu.dsDong.length - giuLai.length;
        duLieu.dsDong = giuLai;
        if (canXoa[giaoDien.idDangSua])
            giaoDien.idDangSua = null;
        veLai();
        khiDanhSachDoi();
        if (soXoa > 0)
            hienThongBao("Đã chuyển " + soXoa + " dòng vào thùng rác", lan);
    }

    // đưa các tên trong thùng rác (theo id) về đúng vị trí cũ trong grid
    function hoanTac(dsId) {
        var canTra = {};
        $.each(dsId, function (i, id) { canTra[id] = true; });

        var traVe = [];
        var conLai = [];
        $.each(duLieu.thungRac, function (i, item) {
            if (canTra[item.id])
                traVe.push(item);
            else
                conLai.push(item);
        });
        if (!traVe.length)
            return;

        // chèn theo vị trí tăng dần để các dòng cùng lượt xóa về đúng thứ tự ban đầu
        traVe.sort(function (a, b) { return a.viTri - b.viTri; });
        $.each(traVe, function (i, item) {
            var viTri = Math.min(item.viTri, duLieu.dsDong.length);
            // dòng khôi phục giữ nguyên trạng thái "đã gợi ý xóa" để không bị tự tích lại
            duLieu.dsDong.splice(viTri, 0, { id: item.id, ten: item.ten, goiYXoa: true });
        });
        duLieu.thungRac = conLai;
        veLai();
        khiDanhSachDoi();
    }

    // ===== Thông báo nhỏ góc dưới khung phải, có nút Hoàn tác cho lượt xóa vừa rồi =====

    var henGioThongBao = null;

    function hienThongBao(noiDung, lanXoa) {
        var $tb = $("#thong-bao-nho");
        if (!$tb.length)
            $tb = $('<div class="toast-v2" id="thong-bao-nho"></div>').appendTo(".result-pane");
        var html = "<span>" + maHoa(noiDung) + "</span>";
        if (lanXoa)
            html += '<button type="button" class="toast-undo" data-lan="' + lanXoa + '">Hoàn tác</button>';
        $tb.html(html).addClass("hien");
        clearTimeout(henGioThongBao);
        henGioThongBao = setTimeout(function () { $tb.removeClass("hien"); }, 5000);
    }

    $(document).on("click", "#thong-bao-nho .toast-undo", function () {
        var lan = $(this).data("lan");
        var dsId = $.map(duLieu.thungRac, function (item) { return item.lan === lan ? item.id : null; });
        hoanTac(dsId);
        $("#thong-bao-nho").removeClass("hien");
    });

    // ===== Chỗ nối grid với phần kết quả =====

    // danh sách đổi do xóa / hoàn tác: vẽ lại kết quả ngay trên trình duyệt, không gọi server
    function khiDanhSachDoi() {
        if (ketQua.daTra)
            veKetQua();
    }

    // một dòng đổi tên sau khi đã tra: gửi tra lại riêng tên này
    function khiSuaTen(dong) {
        if (!ketQua.daTra)
            return;
        goiTraCuu([dong.ten], false);
    }

    // ===== Sự kiện: dòng nhập mới cuối grid =====

    $("#o-nhap-moi").on("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            var ten = lamGon($(this).val());
            if (ten !== "") {
                themTen([ten]);
                $(this).val("");
                cuonXuongCuoi();
            }
        } else if (e.key === "ArrowUp" && duLieu.dsDong.length) {
            // ↑ từ dòng nhập mới: sửa dòng cuối cùng đang hiển thị
            e.preventDefault();
            var ds = layThuTuHienThi();
            batDauSua(ds[ds.length - 1].dong.id);
        }
    });

    // dán nhiều dòng (Ctrl+V) ở dòng nhập mới hoặc ô đang sửa: luôn nối xuống cuối danh sách
    $("#luoi-than").on("paste", "input.ig-input", function (e) {
        var clip = (e.originalEvent || e).clipboardData || window.clipboardData;
        if (!clip)
            return;
        var text = clip.getData("text") || "";
        // dán một tên (không xuống dòng, không tab) thì để trình duyệt dán bình thường vào ô
        if (!/[\r\n\t]/.test(text.trim()))
            return;
        e.preventDefault();
        danVaoDanhSach(text);
    });

    // tách đoạn chữ thành các tên rồi nối xuống cuối danh sách (dùng cho Ctrl+V và nút Dán)
    function danVaoDanhSach(text) {
        var dsTen = tachDanhSach(text);
        if (!dsTen.length) {
            hienThongBao("Không có tên nào để dán.");
            return;
        }
        if (giaoDien.idDangSua !== null)
            luuTenDangSua();
        themTen(dsTen);
        hienThongBao("Đã thêm " + dsTen.length + " tên vào cuối danh sách");
        $("#o-nhap-moi").focus();
        cuonXuongCuoi();
    }

    // nút Dán: đọc bộ nhớ tạm (chỉ được trên localhost/https và khi người dùng cho phép);
    // không đọc được thì mở hộp nhỏ để người dùng tự nhấn Ctrl+V
    $("#nut-dan").on("click", function () {
        if (navigator.clipboard && navigator.clipboard.readText) {
            navigator.clipboard.readText().then(danVaoDanhSach, moHopDan);
        } else {
            moHopDan();
        }
    });

    function moHopDan() {
        moHop('<div class="modal-head"><div><h3 class="modal-title">Dán danh sách thành phần</h3></div>' +
            '<button type="button" class="modal-close" aria-label="Đóng">×</button></div>' +
            '<div class="modal-body">' +
            '<textarea class="paste-box" id="o-dan" placeholder="Nhấn Ctrl+V để dán vào đây, mỗi dòng một tên"></textarea>' +
            '<div class="file-actions"><button type="button" class="btn btn-outline" id="dan-huy">Hủy</button>' +
            '<button type="button" class="btn btn-primary" id="dan-them">Thêm vào danh sách</button></div></div>', "paste-dialog");
        $("#o-dan").focus();
    }

    $("#noi-dung-hop").on("click", "#dan-huy", function () {
        dongHop();
    });

    $("#noi-dung-hop").on("click", "#dan-them", function () {
        var text = $("#o-dan").val();
        dongHop();
        danVaoDanhSach(text);
    });

    // ===== Sự kiện: các dòng trong grid =====

    // bấm 1 lần vào ô tên để sửa
    $("#luoi-dong").on("mousedown", ".ig-name", function (e) {
        e.preventDefault();
        batDauSua($(this).closest(".ig-row").data("id"));
    });

    $("#luoi-dong").on("keydown", ".o-sua-ten", function (e) {
        var id = $(this).closest(".ig-row").data("id");
        if (e.key === "Enter") {
            e.preventDefault();
            luuTenDangSua();
            veLai();
            $("#o-nhap-moi").focus();
        } else if (e.key === "Escape") {
            e.preventDefault();
            giaoDien.idDangSua = null;
            veLai();
            $("#o-nhap-moi").focus();
        } else if (e.key === "ArrowUp" || e.key === "ArrowDown") {
            // ↑/↓: lưu rồi sửa dòng trên / dưới theo thứ tự đang hiển thị; ↓ ở dòng cuối thì về dòng nhập mới
            e.preventDefault();
            var ds = layThuTuHienThi();
            var viTri = -1;
            for (var i = 0; i < ds.length; i++) {
                if (ds[i].dong.id === id)
                    viTri = i;
            }
            var viTriMoi = viTri + (e.key === "ArrowUp" ? -1 : 1);
            luuTenDangSua();
            if (viTriMoi >= 0 && viTriMoi < ds.length && timViTri(ds[viTriMoi].dong.id) >= 0) {
                batDauSua(ds[viTriMoi].dong.id);
            } else {
                veLai();
                if (viTriMoi >= ds.length)
                    $("#o-nhap-moi").focus();
            }
        }
    });

    // rời ô đang sửa (bấm ra ngoài) thì lưu
    $("#luoi-dong").on("blur", ".o-sua-ten", function () {
        setTimeout(function () {
            if (giaoDien.idDangSua !== null && !$("#luoi-dong .o-sua-ten").is(":focus")) {
                luuTenDangSua();
                veLai();
            }
        }, 0);
    });

    // tích chọn từng dòng
    $("#luoi-dong").on("change", ".chon-dong", function () {
        var id = $(this).closest(".ig-row").data("id");
        if (this.checked)
            giaoDien.daChon[id] = true;
        else
            delete giaoDien.daChon[id];
        veGrid();
    });

    // chọn / bỏ chọn tất cả
    $("#chon-tat-ca").on("change", function () {
        var chon = this.checked;
        giaoDien.daChon = {};
        if (chon) {
            $.each(duLieu.dsDong, function (i, dong) { giaoDien.daChon[dong.id] = true; });
        }
        veGrid();
    });

    // Xóa đã chọn: chuyển các dòng đang tích vào thùng rác
    $("#nut-xoa-da-chon").on("click", function () {
        var dsId = $.map(duLieu.dsDong, function (dong) { return giaoDien.daChon[dong.id] ? dong.id : null; });
        xoaDong(dsId);
    });

    // Xóa thông tin: xóa trắng danh sách, thùng rác và kết quả (bắt đầu công thức mới)
    $("#nut-xoa-thong-tin").on("click", function () {
        if (!duLieu.dsDong.length && !duLieu.thungRac.length && !ketQua.daTra)
            return;
        if (!window.confirm("Xóa toàn bộ thông tin tra cứu (danh sách, thùng rác và kết quả)?"))
            return;
        duLieu.dsDong = [];
        duLieu.thungRac = [];
        giaoDien.daChon = {};
        giaoDien.chonRac = {};
        giaoDien.idDangSua = null;
        // kết quả đang chờ server (nếu có) sẽ bị bỏ qua nhờ tăng mã lượt
        ketQua.luot++;
        ketQua.daTra = false;
        ketQua.dangTra = false;
        ketQua.loi = null;
        ketQua.theoKhoa = {};
        ketQua.syt = {};
        ketQua.eu = {};
        ketQua.asean = {};
        $("#nut-tra-cuu").prop("disabled", false);
        veLai();
        veKetQua();
        hienThongBao("Đã xóa thông tin tra cứu");
        $("#o-nhap-moi").focus();
    });

    // bấm tiêu đề cột để sắp xếp; bấm lại để đảo chiều
    $(".ig-sort").on("click", function () {
        var cot = $(this).data("sapxep");
        if (giaoDien.sapXep.cot === cot)
            giaoDien.sapXep.tang = !giaoDien.sapXep.tang;
        else
            giaoDien.sapXep = { cot: cot, tang: true };
        if (giaoDien.idDangSua !== null)
            luuTenDangSua();
        veGrid();
    });

    // ===== Sự kiện: thùng rác =====

    // tích từng tên trong thùng rác
    $("#ds-thung-rac").on("change", ".chon-rac", function () {
        var id = $(this).closest(".bin-row").data("id");
        if (this.checked)
            giaoDien.chonRac[id] = true;
        else
            delete giaoDien.chonRac[id];
        veThungRac();
    });

    $("#chon-tat-ca-rac").on("change", function () {
        var chon = this.checked;
        giaoDien.chonRac = {};
        if (chon) {
            $.each(duLieu.thungRac, function (i, item) { giaoDien.chonRac[item.id] = true; });
        }
        veThungRac();
    });

    // Hoàn tác: đưa các tên đang tích về đúng vị trí cũ trong danh sách
    $("#nut-hoan-tac-rac").on("click", function () {
        var dsId = $.map(duLieu.thungRac, function (item) { return giaoDien.chonRac[item.id] ? item.id : null; });
        hoanTac(dsId);
    });

    // ===== Cuộn grid =====

    function cuonXuongCuoi() {
        var than = document.getElementById("luoi-than");
        than.scrollTop = than.scrollHeight;
    }

    // cuộn để dòng đang sửa luôn nằm trong vùng nhìn thấy
    function cuonToiDong($dong) {
        if (!$dong.length)
            return;
        var than = document.getElementById("luoi-than");
        var dong = $dong[0];
        if (dong.offsetTop < than.scrollTop)
            than.scrollTop = dong.offsetTop;
        else if (dong.offsetTop + dong.offsetHeight > than.scrollTop + than.clientHeight)
            than.scrollTop = dong.offsetTop + dong.offsetHeight - than.clientHeight;
    }

    // ===== Kết quả tra cứu =====

    // kết quả server trả về, gộp qua các lần tra (tra lại một tên thì ghi đè tên đó)
    var ketQua = {
        daTra: false,
        dangTra: false,
        luot: 0,        // mã lượt dữ liệu: Xóa thông tin tăng mã này để bỏ kết quả server trả về muộn
        loi: null,
        theoKhoa: {},   // khóa tên -> { Ten, Khoa, SytChinhXac, SytTuongDoi, AseanChinhXac, AseanTuongDoi }
        syt: {},
        eu: {},
        asean: {}
    };

    // mức độ của một tên trong danh mục EU / ASEAN (để chia nhóm ở tab con Kết quả)
    var MUC_CAM = 4, MUC_GIOI_HAN = 3, MUC_CHO_PHEP = 2, MUC_KHONG_CO = 0;

    // 3 danh mục EU (dữ liệu ở 3 bảng SYT_DanhMuc...) theo thứ tự tab con của app cũ: Cho phép, Giới hạn, Cấm sử dụng
    var DS_DANH_MUC = [
        { ma: "chophep", loai: "Cho phép", muc: MUC_CHO_PHEP, tenTab: "Cho phép", tenMuc: "DANH MỤC CHO PHÉP", lop: "st-chophep" },
        { ma: "gioihan", loai: "Giới hạn", muc: MUC_GIOI_HAN, tenTab: "Giới hạn", tenMuc: "DANH MỤC GIỚI HẠN", lop: "st-gioihan" },
        { ma: "cam", loai: "Cấm", muc: MUC_CAM, tenTab: "Cấm sử dụng", tenMuc: "DANH MỤC CẤM", lop: "st-cam" }
    ];

    function timDanhMuc(ma) {
        for (var i = 0; i < DS_DANH_MUC.length; i++) {
            if (DS_DANH_MUC[i].ma === ma)
                return DS_DANH_MUC[i];
        }
        return null;
    }

    function mucSyt(loai) {
        if (loai === "Cấm") return MUC_CAM;
        if (loai === "Giới hạn") return MUC_GIOI_HAN;
        if (loai === "Cho phép") return MUC_CHO_PHEP;
        return MUC_KHONG_CO;
    }

    // tên hiển thị của danh mục EU theo app cũ: "Cấm sử dụng (EU)", "Giới hạn (EU)", "Cho phép (EU)"
    function tenDanhMucEU(loai) {
        return (loai === "Cấm" ? "Cấm sử dụng" : loai) + " (EU)";
    }

    // Annex ASEAN: II là danh mục cấm, các Annex còn lại là giới hạn
    function mucAsean(annex) {
        return annex === "II" ? MUC_CAM : MUC_GIOI_HAN;
    }

    // chữ nhiều dòng (ThongTin, nồng độ...) -> HTML giữ xuống dòng, gộp các dòng trống liền nhau
    function maHoaNhieuDong(value) {
        var text = String(value == null ? "" : value).replace(/\r\n|\r/g, "\n").replace(/\n[ \t]*(\n[ \t]*)+/g, "\n").trim();
        return maHoa(text).replace(/\n/g, "<br>");
    }

    // so sánh 2 cặp (tên nhập, chất) ở chế độ Tương đối: theo tên nhập, rồi tên chất
    function soSanhTuongDoi(a, b, tenA, tenB) {
        var kq = a.item.ten.localeCompare(b.item.ten, "vi", { sensitivity: "base" });
        if (kq !== 0)
            return kq;
        return tenA.localeCompare(tenB, "vi", { sensitivity: "base" });
    }

    // tiêu đề cột "DÒ TƯƠNG TỰ" (chế độ Chính xác): các chất có tên chứa tên nhập
    function dauCotDoTuongTu() {
        return '<div class="result-col-head head-similar">DÒ TƯƠNG TỰ (tên chứa từ khóa)</div>';
    }

    // các tên duy nhất theo thứ tự nhập, kèm số lần nhập và kết quả (null = chưa tra)
    function layDsTen() {
        var ds = [];
        var theoKhoa = {};
        $.each(duLieu.dsDong, function (i, dong) {
            var khoa = khoaTen(dong.ten);
            if (khoa === "")
                return;
            if (theoKhoa[khoa]) {
                theoKhoa[khoa].soLan++;
                return;
            }
            // kq: kết quả server trả về cho tên này
            var item = { khoa: khoa, ten: dong.ten, stt: i + 1, soLan: 1, kq: ketQua.theoKhoa[khoa] || null };
            theoKhoa[khoa] = item;
            ds.push(item);
        });
        return ds;
    }

    // mức của một tên theo khớp chính xác ở danh mục EU và ANNEX ASEAN (null = chưa tra)
    function danhGia(item) {
        var kq = item.kq;
        var eu = { muc: MUC_KHONG_CO };
        var asean = { muc: MUC_KHONG_CO, ghiChu: "" };
        if (!kq)
            return null;

        // Danh mục EU (3 bảng SYT_DanhMuc...): mức cao nhất trong các chất khớp
        $.each(kq.SytChinhXac, function (i, khoa) {
            eu.muc = Math.max(eu.muc, mucSyt(ketQua.syt[khoa].LoaiDanhMuc));
        });

        // ANNEX ASEAN: ghi chú trạng thái hiệu lực (khác "Đang áp dụng") của dòng ở mức cao nhất
        $.each(kq.AseanChinhXac, function (i, khoa) {
            asean.muc = Math.max(asean.muc, mucAsean(ketQua.asean[khoa].Annex));
        });
        $.each(kq.AseanChinhXac, function (i, khoa) {
            var dong = ketQua.asean[khoa];
            if (!asean.ghiChu && mucAsean(dong.Annex) === asean.muc && dong.TrangThaiHieuLuc && dong.TrangThaiHieuLuc !== "Đang áp dụng")
                asean.ghiChu = dong.TrangThaiHieuLuc;
        });

        return { eu: eu, asean: asean };
    }

    // ===== Gọi server =====

    // tra danh sách tên; traHet = true thì thay toàn bộ kết quả cũ, false thì chỉ ghi đè các tên vừa tra
    function goiTraCuu(dsTen, traHet) {
        if (!dsTen.length)
            return;
        ketQua.dangTra = true;
        $("#nut-tra-cuu").prop("disabled", true);
        veKetQua();
        var luot = ketQua.luot;

        $.ajax({
            url: $trang.data("url-tra-cuu"),
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ dsTen: dsTen }),
            dataType: "json"
        }).done(function (data) {
            // người dùng đã bấm Xóa thông tin trong lúc chờ: bỏ kết quả này
            if (luot !== ketQua.luot)
                return;
            if (data.Loi) {
                ketQua.loi = data.Loi;
                return;
            }
            ketQua.loi = null;
            if (traHet) {
                ketQua.theoKhoa = {};
                ketQua.syt = {};
                ketQua.eu = {};
                ketQua.asean = {};
            }
            $.each(data.DSTen, function (i, dong) { ketQua.theoKhoa[dong.Khoa] = dong; });
            $.extend(ketQua.syt, data.Syt);
            $.extend(ketQua.eu, data.EU);
            $.extend(ketQua.asean, data.Asean);
            ketQua.daTra = true;
        }).fail(function (xhr) {
            if (luot === ketQua.luot)
                ketQua.loi = "Lỗi khi tải dữ liệu: không kết nối được máy chủ (mã " + xhr.status + ").";
        }).always(function () {
            if (luot !== ketQua.luot)
                return;
            ketQua.dangTra = false;
            $("#nut-tra-cuu").prop("disabled", false);
            veKetQua();
            // màn hình hẹp: kết quả nằm dưới danh sách nhập, cuộn xuống để thấy kết quả
            if (window.matchMedia && window.matchMedia("(max-width: 767px)").matches)
                document.getElementById("hang-tab").scrollIntoView();
        });
    }

    // nút Tra cứu: tra tất cả tên trong danh sách (tên trùng chỉ gửi một lần)
    function traCuuTatCa() {
        if (giaoDien.idDangSua !== null) {
            luuTenDangSua();
            veLai();
        }
        var dsTen = $.map(layDsTen(), function (item) { return item.ten; });
        if (!dsTen.length) {
            hienThongBao("Hãy nhập ít nhất một thành phần.");
            $("#o-nhap-moi").focus();
            return;
        }
        goiTraCuu(dsTen, true);
    }

    // ===== Vẽ khung kết quả =====

    function veKetQua() {
        // tab và công tắc đang chọn
        $("#hang-tab .tab-btn").each(function () {
            $(this).toggleClass("active", $(this).data("tab") === giaoDien.tab);
        });
        $("#cong-tac-che-do .mode-btn").each(function () {
            $(this).toggleClass("active", $(this).data("chedo") === giaoDien.cheDo);
        });

        var $vung = $("#ket-qua-v2");
        if (!$vung.length)
            $vung = $('<div id="ket-qua-v2"></div>').appendTo("#noi-dung-ket-qua");

        if (!ketQua.daTra && !ketQua.loi) {
            $("#khung-chua-tra").removeClass("an");
            $vung.html(ketQua.dangTra ? '<div class="pane-loading">Đang tra cứu…</div>' : "");
            if (ketQua.dangTra)
                $("#khung-chua-tra").addClass("an");
            return;
        }
        $("#khung-chua-tra").addClass("an");

        var tuongDoi = (giaoDien.cheDo === "tuongdoi");
        var dsTen = layDsTen();
        var dsDaTra = [];
        var soChuaTra = 0;
        $.each(dsTen, function (i, item) {
            item.dg = danhGia(item);
            if (item.dg)
                dsDaTra.push(item);
            else
                soChuaTra++;
        });

        var html = [];
        if (ketQua.loi)
            html.push('<div class="notice notice-error">' + maHoa(ketQua.loi) + "</div>");
        if (ketQua.dangTra)
            html.push('<div class="pane-notice">Đang tra cứu…</div>');
        else if (soChuaTra > 0)
            html.push('<div class="pane-notice">' + soChuaTra + " tên mới chưa tra - bấm <b>Tra cứu</b> để cập nhật.</div>");

        if (ketQua.daTra) {
            if (giaoDien.tab === "eu")
                html.push(veEU(dsDaTra, tuongDoi));
            else
                html.push(veAsean(dsDaTra, tuongDoi));
        }

        $vung.html(html.join(""));
        danhDauXemThem($vung);
    }

    // ----- Tab EU: ô tham chiếu (bấm để mở hộp chi tiết của đúng ref) -----
    function oThamChieu(chat, tenNhap) {
        if (!chat.DSThamChieu.length)
            return '<span class="muted">-</span>';
        return $.map(chat.DSThamChieu, function (tc) {
            if (tc.KhoaEU)
                return '<button type="button" class="ref-btn" data-syt="' + maHoa(chat.Khoa) + '" data-eu="' + maHoa(tc.KhoaEU) +
                    '" data-ten="' + maHoa(tenNhap) + '">' + maHoa(tc.ThamChieu) + "</button>";
            return '<span class="ref-btn missing" title="Không tìm thấy tham chiếu này trong Annex EU">' + maHoa(tc.ThamChieu) + "</span>";
        }).join(" ");
    }

    function oThongTin(value) {
        if (!lamGon(value))
            return '<span class="muted">-</span>';
        return '<div class="info-clamp">' + maHoaNhieuDong(value) + "</div>";
    }

    // cột Data (Gốc / Dự kiến bổ sung) không hiện, như app cũ (đối chiếu 2026-11-11)
    function oTenChat(chat, tenNhap, coDongPhu) {
        return '<span class="cell-name">' + maHoa(chat.Ten) + "</span>" +
            (coDongPhu ? '<span class="cell-sub">Tên nhập: ' + maHoa(tenNhap) + "</span>" : "");
    }

    // các cột Annex EU II ghép vào dòng chất Cấm; chất có nhiều ref thì xếp chồng theo ref; rutGon: chữ dài rút gọn 3 dòng + "xem thêm"
    function cotEUCam(chat, truong, rutGon) {
        var ds = [];
        $.each(chat.DSThamChieu, function (i, tc) {
            var dong = tc.KhoaEU ? ketQua.eu[tc.KhoaEU] : null;
            if (dong && lamGon(dong[truong]))
                ds.push(rutGon ? '<div class="info-clamp">' + maHoaNhieuDong(dong[truong]) + "</div>" : maHoaNhieuDong(dong[truong]));
        });
        return ds.length ? ds.join("<hr class=\"cell-sep\">") : '<span class="muted">-</span>';
    }

    // bảng của một danh mục EU (Cấm sử dụng / Giới hạn / Cho phép); dsCap = [{ item (tên nhập), chat }]; coTenNhap: thêm cột Tên nhập (chế độ Tương đối)
    // Cấm, Cho phép đủ các cột như app cũ (đối chiếu 2026-11-11); bảng Cấm rộng nên cuộn ngang
    function veBangDanhMuc(dm, dsCap, coTenNhap) {
        var html = ['<div class="card table-card"><table class="data-table syt-table' + (dm.ma === "cam" ? " wide-table" : "") +
            '"><thead><tr><th style="width:52px">STT</th>'];
        var soCot;
        // độ rộng cột cố định để cột Thông tin luôn đủ chỗ đọc
        var thTenNhap = coTenNhap ? '<th style="width:124px">Tên nhập</th>' : "";
        if (dm.ma === "cam") {
            html.push('<th style="width:186px">Tên chất</th>' + thTenNhap +
                '<th style="width:84px">Ref</th><th style="width:190px">Chemical name / INN</th><th style="width:96px">CAS</th><th style="width:92px">EC</th>' +
                '<th style="width:260px">Thông tin</th><th style="width:160px">Ghi chú</th><th style="width:130px">Regulation</th>' +
                '<th style="width:180px">Other Directives/Regulations</th><th style="width:180px">SCCS opinions</th>' +
                '<th style="width:200px">Chemical/IUPAC Name</th><th style="width:200px">Identified ingredients</th>');
            soCot = coTenNhap ? 15 : 14;
        } else if (dm.ma === "gioihan") {
            html.push('<th style="width:230px">Tên chất</th>' + thTenNhap +
                '<th style="width:150px">Tham chiếu Annex EU</th><th>Thông tin</th><th style="width:140px">Ghi chú</th>');
            soCot = coTenNhap ? 6 : 5;
        } else {
            html.push('<th style="width:220px">Tên chất</th><th style="width:150px">Tên nhập</th>' +
                (coTenNhap ? '<th style="width:150px">Tham chiếu Annex EU</th>' : "") +
                '<th style="width:110px">Mô tả</th><th>Thông tin</th><th style="width:180px">Ghi chú</th>');
            soCot = coTenNhap ? 7 : 6;
        }
        html.push("</tr></thead><tbody>");

        if (!dsCap.length)
            html.push('<tr><td colspan="' + soCot + '" class="empty-row">Không có thành phần nào thuộc danh mục ' +
                dm.loai.toLowerCase() + " trong danh sách đã tra.</td></tr>");

        $.each(dsCap, function (i, cap) {
            var chat = cap.chat;
            var tenNhap = cap.item.ten;
            var khopChinhXac = coTenNhap && $.inArray(chat.Khoa, cap.item.kq.SytChinhXac) >= 0;
            // nhãn ở chế độ Tương đối: KHỚP CHÍNH XÁC
            var nhanKhop = khopChinhXac ? '<span class="exact-chip">KHỚP CHÍNH XÁC</span>' : "";
            var oTen = oTenChat(chat, tenNhap, !coTenNhap) + nhanKhop;
            var oNhap = coTenNhap ? "<td>" + maHoa(tenNhap) + "</td>" : "";
            var lop = (cap.dauNhom && i > 0) ? ' class="group-first"' : "";
            html.push("<tr" + lop + "><td>" + (i + 1) + "</td>");
            if (dm.ma === "cam") {
                html.push("<td>" + oTen + "</td>" + oNhap + "<td>" + oThamChieu(chat, tenNhap) + "</td>" +
                    "<td>" + cotEUCam(chat, "TenChat") + "</td><td>" + cotEUCam(chat, "CAS") + "</td><td>" + cotEUCam(chat, "EC") + "</td>" +
                    "<td>" + oThongTin(chat.ThongTin) + "</td><td>" + oThongTin(chat.GhiChu) + "</td>" +
                    "<td>" + cotEUCam(chat, "Regulation", true) + "</td><td>" + cotEUCam(chat, "ChiThiKhac", true) + "</td>" +
                    "<td>" + cotEUCam(chat, "SCCS", true) + "</td><td>" + cotEUCam(chat, "IUPAC", true) + "</td>" +
                    "<td>" + cotEUCam(chat, "ThanhPhanNhanDien", true) + "</td>");
            } else if (dm.ma === "gioihan") {
                html.push("<td>" + oTen + "</td>" + oNhap + "<td>" + oThamChieu(chat, tenNhap) + "</td>" +
                    "<td>" + oThongTin(chat.ThongTin) + "</td><td>" + oThongTin(chat.GhiChu) + "</td>");
            } else {
                html.push("<td>" + oTenChat(chat, tenNhap, false) + nhanKhop + "</td>" +
                    "<td>" + maHoa(tenNhap) + "</td>" + (coTenNhap ? "<td>" + oThamChieu(chat, tenNhap) + "</td>" : "") +
                    "<td>" + (lamGon(chat.MoTa) ? maHoa(chat.MoTa) : '<span class="muted">-</span>') + "</td>" +
                    "<td>" + oThongTin(chat.ThongTin) + "</td><td>" + oThongTin(chat.GhiChu) + "</td>");
            }
            html.push("</tr>");
        });
        html.push("</tbody></table></div>");
        return html.join("");
    }

    // các cặp (tên nhập, chất SYT) chia theo danh mục; truong = SytChinhXac hoặc SytTuongDoi
    function chiaTheoDanhMuc(dsDaTra, truong) {
        var kq = { cam: [], gioihan: [], chophep: [] };
        $.each(dsDaTra, function (i, item) {
            $.each(item.kq[truong], function (j, khoa) {
                var chat = ketQua.syt[khoa];
                var muc = mucSyt(chat.LoaiDanhMuc);
                var ma = muc === MUC_CAM ? "cam" : (muc === MUC_GIOI_HAN ? "gioihan" : "chophep");
                kq[ma].push({ item: item, chat: chat });
            });
        });
        return kq;
    }

    // ----- Hàng tab con (dùng chung cho tab EU, ASEAN và các Annex) -----
    // dsTab = [{ ma, chu, dem (null = không hiện số), lop (màu chữ theo loại danh mục, có thể bỏ) }]; thuocTinh: tên data-* nhận mã tab
    function veHangTabCon(dsTab, dangChon, thuocTinh, lopThem) {
        var html = ['<nav class="sub-tabs' + (lopThem ? " " + lopThem : "") + '">'];
        $.each(dsTab, function (i, tab) {
            html.push('<button type="button" class="sub-tab' + (tab.lop ? " " + tab.lop : "") + (tab.ma === dangChon ? " active" : "") + '" ' +
                thuocTinh + '="' + tab.ma + '">' + tab.chu +
                (tab.dem == null ? "" : ' <span class="sub-count">' + tab.dem + "</span>") + "</button>");
        });
        html.push("</nav>");
        return html.join("");
    }

    // ----- Tab Danh mục EU: tab con như app cũ (Kết quả · Dò chính tả · Cho phép · Giới hạn · Cấm sử dụng · Không có) -----
    function veEU(dsDaTra, tuongDoi) {
        var truong = tuongDoi ? "SytTuongDoi" : "SytChinhXac";
        var theoDm = chiaTheoDanhMuc(dsDaTra, truong);
        if (tuongDoi) {
            $.each(DS_DANH_MUC, function (i, dm) { sapXepTuongDoiSyt(theoDm[dm.ma]); });
        }
        // Không có: tên không có chất nào theo chế độ đang chọn
        var khongCo = $.grep(dsDaTra, function (item) { return !item.kq[truong].length; });

        var dsTab = [{ ma: "ketqua", chu: "Kết quả", dem: null }, { ma: "chinhta", chu: "Dò chính tả", dem: null }];
        $.each(DS_DANH_MUC, function (i, dm) {
            dsTab.push({ ma: dm.ma, chu: dm.tenTab, dem: theoDm[dm.ma].length, lop: dm.lop });
        });
        dsTab.push({ ma: "khongco", chu: "Không có", dem: khongCo.length });

        var html = [veHangTabCon(dsTab, giaoDien.xemEU, "data-xem-eu")];
        var dm = timDanhMuc(giaoDien.xemEU);
        if (giaoDien.xemEU === "ketqua")
            html.push(tuongDoi ? veEUKetQuaTuongDoi(theoDm, khongCo) : veEUKetQuaChinhXac(dsDaTra));
        else if (giaoDien.xemEU === "chinhta")
            html.push(veDoChinhTa(dsDaTra));
        else if (dm)
            html.push(veBangDanhMuc(dm, theoDm[dm.ma], tuongDoi));
        else
            html.push(veKhongCo(khongCo, "Danh mục EU"));
        return html.join("");
    }

    // chế độ Tương đối: sắp theo tên nhập, rồi tên chất; đánh dấu dòng đầu mỗi tên nhập để kẻ vạch ngăn nhóm
    function sapXepTuongDoiSyt(ds) {
        ds.sort(function (a, b) {
            return soSanhTuongDoi(a, b, a.chat.Ten, b.chat.Ten);
        });
        $.each(ds, function (j, cap) { cap.dauNhom = (j === 0 || ds[j - 1].item.khoa !== cap.item.khoa); });
    }

    // nhãn màu theo loại danh mục EU (Cấm / Giới hạn / Cho phép)
    function lopTagSyt(chat) {
        var muc = mucSyt(chat.LoaiDanhMuc);
        return muc === MUC_CAM ? "tag-cam" : (muc === MUC_GIOI_HAN ? "tag-gioihan" : "tag-chophep");
    }

    // tab con Kết quả, chế độ Chính xác (như app cũ): trái "Dò trùng khớp" nhóm A-D, phải "Dò tương tự" cho tên chưa khớp.
    // Tên khớp chất ở nhiều danh mục (vd vừa Cấm vừa Giới hạn) hiện ở MỌI nhóm đó, như app cũ (đối chiếu 2026-11-11)
    function veEUKetQuaChinhXac(dsDaTra) {
        var html = [];
        var nhom = { cam: [], khongco: [], gioihan: [], chophep: [] };
        $.each(dsDaTra, function (i, item) {
            var coMuc = {};
            $.each(item.kq.SytChinhXac, function (j, khoa) {
                coMuc[mucSyt(ketQua.syt[khoa].LoaiDanhMuc)] = true;
            });
            if (coMuc[MUC_CAM]) nhom.cam.push(item);
            if (coMuc[MUC_GIOI_HAN]) nhom.gioihan.push(item);
            if (coMuc[MUC_CHO_PHEP]) nhom.chophep.push(item);
            if (!item.kq.SytChinhXac.length) nhom.khongco.push(item);
        });

        function veNhom(tieuDe, ds, muc) {
            var h = ['<div class="result-group"><div class="result-group-title">' + tieuDe + "</div>"];
            if (!ds.length) {
                h.push('<p class="muted">Không có kết quả</p></div>');
                return h.join("");
            }
            h.push('<ul class="result-list">');
            $.each(ds, function (i, item) {
                h.push("<li>" + (muc === MUC_KHONG_CO ? '<span class="result-name muted">' : '<span class="result-name">') + maHoa(item.ten) + "</span>");
                $.each(item.kq.SytChinhXac, function (j, khoa) {
                    var chat = ketQua.syt[khoa];
                    if (mucSyt(chat.LoaiDanhMuc) === muc)
                        h.push(oThamChieu(chat, item.ten).replace('<span class="muted">-</span>', ""));
                });
                h.push("</li>");
            });
            h.push("</ul></div>");
            return h.join("");
        }

        html.push('<div class="result-columns"><div class="result-col"><div class="result-col-head head-exact">' +
            "DÒ TRÙNG KHỚP (chính xác)" + '</div><div class="result-col-body">');
        html.push(veNhom("A. DANH MỤC CẤM", nhom.cam, MUC_CAM));
        html.push(veNhom("B. KHÔNG CÓ KẾT QUẢ", nhom.khongco, MUC_KHONG_CO));
        html.push(veNhom("C. DANH MỤC GIỚI HẠN", nhom.gioihan, MUC_GIOI_HAN));
        html.push(veNhom("D. DANH MỤC CHO PHÉP", nhom.chophep, MUC_CHO_PHEP));
        html.push("</div></div>");

        html.push('<div class="result-col">' + dauCotDoTuongTu() + '<div class="result-col-body">');
        if (!nhom.khongco.length) {
            html.push('<p class="similar-empty muted">Không có gợi ý tương tự - tất cả thành phần đều đã khớp chính xác.</p>');
        }
        $.each(nhom.khongco, function (i, item) {
            var ds = item.kq.SytTuongDoi;
            html.push('<div class="result-group"><div class="similar-title">Tên nhập: <b>' + maHoa(item.ten) + "</b>" +
                (ds.length ? ' <span class="muted">· ' + ds.length + ' chất có tên chứa "' + maHoa(item.khoa) + '"</span>' : "") + "</div>");
            if (!ds.length) {
                html.push('<p class="muted">Không có chất nào có tên chứa "' + maHoa(item.khoa) + '".</p></div>');
                return;
            }
            html.push('<ul class="suggest-list">');
            $.each(ds, function (j, khoa) {
                var chat = ketQua.syt[khoa];
                var muc = mucSyt(chat.LoaiDanhMuc);
                var lopTag = muc === MUC_CAM ? "tag-cam" : (muc === MUC_GIOI_HAN ? "tag-gioihan" : "tag-chophep");
                html.push('<li><span class="suggest-tag ' + lopTag + '">' + maHoa(chat.LoaiDanhMuc) + "</span>" +
                    '<span class="suggest-name">' + maHoa(chat.Ten) + "</span>" +
                    '<span class="suggest-action">' + oThamChieu(chat, item.ten).replace('<span class="muted">-</span>', "") + "</span></li>");
            });
            html.push("</ul></div>");
        });
        html.push("</div></div></div>");
        return html.join("");
    }

    // tab con Kết quả, chế độ Tương đối (như app cũ): liệt kê chất tìm được theo danh mục, rồi các tên không có kết quả
    function veEUKetQuaTuongDoi(theoDm, khongCo) {
        var html = ['<div class="card result-single">'];
        function veMuc(tieuDe, dsCap) {
            html.push('<div class="result-group"><div class="result-group-title">' + tieuDe + "</div>");
            if (!dsCap.length) {
                html.push('<p class="muted">Không có kết quả</p></div>');
                return;
            }
            html.push('<ul class="result-list">');
            $.each(dsCap, function (i, cap) {
                var khop = $.inArray(cap.chat.Khoa, cap.item.kq.SytChinhXac) >= 0;
                html.push('<li><span class="result-name">' + maHoa(cap.chat.Ten) + "</span>" +
                    (khop ? '<span class="exact-chip">KHỚP CHÍNH XÁC</span>' : "") +
                    oThamChieu(cap.chat, cap.item.ten).replace('<span class="muted">-</span>', "") +
                    '<span class="result-input">Tên nhập: ' + maHoa(cap.item.ten) + "</span></li>");
            });
            html.push("</ul></div>");
        }
        veMuc("DANH MỤC CẤM", theoDm.cam);
        html.push('<div class="result-group"><div class="result-group-title">KHÔNG CÓ KẾT QUẢ</div>');
        if (khongCo.length) {
            html.push('<ul class="result-list">' + $.map(khongCo, function (item) {
                return '<li><span class="result-name muted">' + maHoa(item.ten) + "</span></li>";
            }).join("") + "</ul></div>");
        } else {
            html.push('<p class="muted">Không có kết quả</p></div>');
        }
        veMuc("DANH MỤC GIỚI HẠN", theoDm.gioihan);
        veMuc("DANH MỤC CHO PHÉP", theoDm.chophep);
        html.push("</div>");
        return html.join("");
    }

    // tab con Dò chính tả (như app cũ): DÒ TRÙNG KHỚP = tên nhập có đúng trong Danh mục EU (đúng chính tả);
    // DÒ TƯƠNG TỰ = tên nhập có tên trong Danh mục EU CHỨA nó. Tên không nằm trong hai mục này nên kiểm tra lại chính tả.
    // Không phụ thuộc công tắc Chính xác / Tương đối; tên trùng chỉ hiện một lần (dsDaTra đã bỏ trùng)
    function veDoChinhTa(dsDaTra) {
        // đúng chính tả = có đúng tên trong danh mục
        var trungKhop = $.grep(dsDaTra, function (item) { return item.kq.SytChinhXac.length > 0; });
        var tuongTu = $.grep(dsDaTra, function (item) { return item.kq.SytTuongDoi.length > 0; });
        var html = ['<div class="card result-single">'];
        function veMuc(tieuDe, ds, goiY) {
            html.push('<div class="result-group"><div class="result-group-title">' + tieuDe + "</div>");
            if (!ds.length) {
                html.push('<p class="muted">Không có kết quả</p></div>');
                return;
            }
            html.push('<ul class="result-list">' + $.map(ds, function (item) {
                return '<li><span class="result-name" title="' + goiY + '">' + maHoa(item.ten) + "</span></li>";
            }).join("") + "</ul></div>");
        }
        veMuc("DÒ TRÙNG KHỚP", trungKhop, "Có đúng tên này trong Danh mục EU");
        veMuc("DÒ TƯƠNG TỰ", tuongTu, "Có tên trong Danh mục EU chứa tên này");
        html.push("</div>");
        return html.join("");
    }

    // tab con Không có: các tên nhập không có kết quả
    function veKhongCo(ds, tenDanhMuc) {
        if (!ds.length)
            return '<p class="muted">Tất cả thành phần đều có kết quả trong ' + tenDanhMuc + ".</p>";
        return '<div class="card result-single"><ul class="result-list">' + $.map(ds, function (item) {
            return '<li><span class="result-name">' + maHoa(item.ten) + "</span></li>";
        }).join("") + "</ul></div>";
    }

    // ----- Tab ANNEX ASEAN -----

    var DS_ANNEX_ASEAN = ["II", "III", "IV", "VI", "VII"];

    // nút "Xem chi tiết" một dòng Annex ASEAN
    function nutChiTietAsean(dong, tenNhap) {
        return '<button type="button" class="btn-detail asean-btn" data-asean="' + maHoa(dong.Khoa) + '" data-ten="' + maHoa(tenNhap) + '">Xem chi tiết</button>';
    }

    // ghi chú trạng thái hiệu lực khác "Đang áp dụng" (Sắp áp dụng, sắp thay đổi...)
    function ghiChuHieuLuc(dong) {
        if (!dong.TrangThaiHieuLuc || dong.TrangThaiHieuLuc === "Đang áp dụng")
            return "";
        return '<span class="cell-warn">' + maHoa(dong.TrangThaiHieuLuc) + "</span>";
    }

    // Annex IV: tên chất dạng "77891 (White)" -> lấy phần trong ngoặc cuối làm Colour
    function layMauAnnexIV(dong) {
        var khop = /\(([^()]*)\)\s*$/.exec(dong.TenChat || "");
        return khop ? khop[1] : "";
    }

    // các cặp (tên nhập, dòng ASEAN) chia theo Annex; truong = AseanChinhXac hoặc AseanTuongDoi
    function chiaTheoAnnex(dsDaTra, truong) {
        var kq = {};
        $.each(DS_ANNEX_ASEAN, function (i, annex) { kq[annex] = []; });
        $.each(dsDaTra, function (i, item) {
            $.each(item.kq[truong], function (j, khoa) {
                var dong = ketQua.asean[khoa];
                if (kq[dong.Annex])
                    kq[dong.Annex].push({ item: item, dong: dong });
            });
        });
        return kq;
    }

    // bảng một Annex ASEAN; coTenNhap: thêm cột Tên nhập + nhãn khớp chính xác (chế độ Tương đối)
    function veBangAnnex(annex, dsCap, coTenNhap) {
        var html = ['<div class="annex-table-wrap"><table class="data-table syt-table"><thead><tr><th style="width:52px">STT</th>'];
        var thTenNhap = coTenNhap ? '<th style="width:124px">Tên nhập</th>' : "";
        var soCot;
        if (annex === "II") {
            html.push("<th>Substances</th>" + thTenNhap + '<th style="width:90px">Ref. No</th><th style="width:150px">CAS Number</th><th style="width:220px">Note</th>');
            soCot = coTenNhap ? 6 : 5;
        } else if (annex === "IV") {
            html.push("<th>Colour Index</th>" + thTenNhap + '<th>Colour</th><th style="width:120px"></th>');
            soCot = coTenNhap ? 5 : 4;
        } else {
            html.push("<th>Substances</th>" + thTenNhap + '<th style="width:90px">Ref. No</th><th style="width:260px">Max. authorised concentration</th><th style="width:120px"></th>');
            soCot = coTenNhap ? 6 : 5;
        }
        html.push("</tr></thead><tbody>");

        if (!dsCap.length)
            html.push('<tr><td colspan="' + soCot + '" class="empty-row">Không có thành phần nào thuộc Annex ' + annex + " trong danh sách đã tra.</td></tr>");

        $.each(dsCap, function (i, cap) {
            var dong = cap.dong;
            var tenNhap = cap.item.ten;
            var khopChinhXac = coTenNhap && $.inArray(dong.Khoa, cap.item.kq.AseanChinhXac) >= 0;
            var dongPhu = coTenNhap
                ? (khopChinhXac ? '<span class="exact-chip" style="margin-left:0">KHỚP CHÍNH XÁC</span>' : "")
                : '<span class="cell-sub">Tên nhập: ' + maHoa(tenNhap) + "</span>";
            var hieuLuc = ghiChuHieuLuc(dong);
            var oNhap = coTenNhap ? "<td>" + maHoa(tenNhap) + "</td>" : "";
            var lop = (cap.dauNhom && i > 0) ? ' class="group-first"' : "";
            html.push("<tr" + lop + "><td>" + (i + 1) + "</td>");
            if (annex === "IV") {
                html.push('<td><span class="cell-name">' + maHoa(dong.RefNo) + "</span>" + dongPhu + (hieuLuc ? "<br>" + hieuLuc : "") + "</td>" + oNhap +
                    "<td>" + maHoa(layMauAnnexIV(dong)) + '</td><td class="col-action">' + nutChiTietAsean(dong, tenNhap) + "</td>");
            } else {
                html.push('<td><div class="info-clamp"><span class="cell-name">' + maHoaNhieuDong(dong.TenChat) + "</span></div>" + dongPhu +
                    (hieuLuc ? "<br>" + hieuLuc : "") + "</td>" + oNhap + "<td>" + maHoa(dong.RefNo) + "</td>");
                if (annex === "II")
                    html.push("<td>" + (lamGon(dong.CAS) ? maHoaNhieuDong(dong.CAS) : '<span class="muted">-</span>') + "</td><td>" + oThongTin(dong.GhiChu) + "</td>");
                else
                    html.push("<td>" + oThongTin(dong.GioiHanToiDa) + '</td><td class="col-action">' + nutChiTietAsean(dong, tenNhap) + "</td>");
            }
            html.push("</tr>");
        });
        html.push("</tbody></table></div>");
        return html.join("");
    }

    // ----- Tab Danh mục ANNEX ASEAN: tab con như app cũ (Kết quả · Annex, trong Annex có tab Annex II, III, IV, VI, VII) -----

    // tiêu đề bảng mỗi Annex, giữ chữ của app cũ
    var TIEU_DE_ANNEX_ASEAN = {
        II: "Danh sách chất bị cấm trong sản phẩm mỹ phẩm (ASEAN)",
        III: "Danh sách chất bị giới hạn trong sản phẩm mỹ phẩm (ASEAN)",
        IV: "Danh sách phẩm màu cho phép trong sản phẩm mỹ phẩm (ASEAN)",
        VI: "Danh sách chất bảo quản cho phép trong sản phẩm mỹ phẩm (ASEAN)",
        VII: "Danh sách chất chống tia UV cho phép trong sản phẩm mỹ phẩm (ASEAN)"
    };

    function veAsean(dsDaTra, tuongDoi) {
        var truong = tuongDoi ? "AseanTuongDoi" : "AseanChinhXac";
        var theoAnnex = chiaTheoAnnex(dsDaTra, truong);
        if (tuongDoi) {
            $.each(DS_ANNEX_ASEAN, function (i, annex) { sapXepTuongDoiAsean(theoAnnex[annex]); });
        }

        var html = [veHangTabCon([{ ma: "ketqua", chu: "Kết quả", dem: null }, { ma: "annex", chu: "Annex", dem: null }],
            giaoDien.xemAsean, "data-xem-asean")];
        if (giaoDien.xemAsean === "annex") {
            var dsTab = $.map(DS_ANNEX_ASEAN, function (annex) {
                return { ma: annex, chu: "Annex " + annex, dem: theoAnnex[annex].length };
            });
            html.push(veHangTabCon(dsTab, giaoDien.annexChon, "data-annex", "sub-tabs-sm"));
            html.push('<div class="annex-title">' + TIEU_DE_ANNEX_ASEAN[giaoDien.annexChon] + "</div>");
            html.push(veBangAnnex(giaoDien.annexChon, theoAnnex[giaoDien.annexChon], tuongDoi));
            return html.join("");
        }

        var khongCo = $.grep(dsDaTra, function (item) { return !item.kq[truong].length; });
        html.push(veAseanKetQua(theoAnnex, khongCo, tuongDoi));
        return html.join("");
    }

    function sapXepTuongDoiAsean(ds) {
        ds.sort(function (a, b) {
            return soSanhTuongDoi(a, b, lamGon(a.dong.TenChat), lamGon(b.dong.TenChat));
        });
        $.each(ds, function (j, cap) { cap.dauNhom = (j === 0 || ds[j - 1].item.khoa !== cap.item.khoa); });
    }

    // tab con Kết quả (như app cũ): các chất tìm được theo từng Annex, cuối cùng là tên không có kết quả
    function veAseanKetQua(theoAnnex, khongCo, tuongDoi) {
        var html = ['<div class="card result-single">'];
        $.each(DS_ANNEX_ASEAN, function (i, annex) {
            var dsCap = theoAnnex[annex];
            html.push('<div class="result-group"><div class="result-group-title">DANH MỤC ANNEX ' + annex + "</div>");
            if (!dsCap.length) {
                html.push('<p class="muted">Không có kết quả</p></div>');
                return;
            }
            html.push('<ul class="result-list">');
            $.each(dsCap, function (j, cap) {
                var dong = cap.dong;
                var khop = tuongDoi && $.inArray(dong.Khoa, cap.item.kq.AseanChinhXac) >= 0;
                var tenChat = annex === "IV" ? "CI " + dong.RefNo + (layMauAnnexIV(dong) ? " (" + layMauAnnexIV(dong) + ")" : "") : lamGon(dong.TenChat);
                html.push('<li><span class="result-name">' + maHoa(tenChat) + "</span>" +
                    (khop ? '<span class="exact-chip">KHỚP CHÍNH XÁC</span>' : "") +
                    '<button type="button" class="ref-btn asean-btn" data-asean="' + maHoa(dong.Khoa) + '" data-ten="' + maHoa(cap.item.ten) + '">' +
                    maHoa(dong.Annex + "/" + dong.RefNo) + "</button>" + ghiChuHieuLuc(dong) +
                    '<span class="result-input">Tên nhập: ' + maHoa(cap.item.ten) + "</span></li>");
            });
            html.push("</ul></div>");
        });
        html.push('<div class="result-group"><div class="result-group-title">KHÔNG CÓ KẾT QUẢ</div>');
        if (khongCo.length) {
            html.push('<ul class="result-list">' + $.map(khongCo, function (item) {
                return '<li><span class="result-name muted">' + maHoa(item.ten) + "</span></li>";
            }).join("") + "</ul></div>");
        } else {
            html.push('<p class="muted">Không có kết quả</p></div>');
        }
        html.push("</div>");
        return html.join("");
    }

    // cột Thông tin dài: rút gọn 3 dòng, chỉ hiện "xem thêm" khi chữ thật sự bị cắt
    function danhDauXemThem($vung) {
        $vung.find(".info-clamp").each(function () {
            if (this.scrollHeight > this.clientHeight + 2)
                $(this).after('<button type="button" class="more-link">xem thêm</button>');
        });
    }

    // ===== Hộp chi tiết EU (bấm ô tham chiếu) =====

    function moHopChiTietEU(khoaSyt, khoaEU, tenNhap) {
        var chat = ketQua.syt[khoaSyt];
        var dongEU = ketQua.eu[khoaEU];
        if (!chat || !dongEU)
            return;

        var tenRef = "";
        var oRef = $.map(chat.DSThamChieu, function (tc) {
            if (tc.KhoaEU === khoaEU)
                tenRef = tc.ThamChieu;
            if (!tc.KhoaEU)
                return '<span class="ref-btn missing" title="Không tìm thấy tham chiếu này trong Annex EU">' + maHoa(tc.ThamChieu) + "</span>";
            return '<button type="button" class="ref-btn' + (tc.KhoaEU === khoaEU ? " ref-active" : "") + '" data-syt="' + maHoa(chat.Khoa) +
                '" data-eu="' + maHoa(tc.KhoaEU) + '" data-ten="' + maHoa(tenNhap) + '">' + maHoa(tc.ThamChieu) + "</button>";
        }).join(" ");

        var html = [];
        html.push('<div class="modal-head"><div>' +
            '<h3 class="modal-title">' + maHoa(lamGon(dongEU.TenChat) || chat.Ten) + "</h3>" +
            '<div class="modal-sub">Annex EU ' + maHoa(tenRef) + " · " + maHoa(tenDanhMucEU(chat.LoaiDanhMuc)) + ' · Tên nhập: "' + maHoa(tenNhap) + '"</div>' +
            '<div class="modal-refs">Tham chiếu của chất này: ' + oRef + "</div>" +
            '</div><button type="button" class="modal-close" aria-label="Đóng">×</button></div>');
        html.push('<div class="modal-body">');
        html.push('<div class="modal-section">' + maHoa(tenDanhMucEU(chat.LoaiDanhMuc).toUpperCase()) + "</div>");
        html.push('<div class="field-row">' +
            oTruong("TÊN CHẤT", chat.Ten) +
            oTruong("LOẠI DANH MỤC", tenDanhMucEU(chat.LoaiDanhMuc)) +
            oTruong("MÔ TẢ", chat.MoTa) + "</div>");
        html.push(oTruong("THÔNG TIN", chat.ThongTin));
        html.push(oTruong("GHI CHÚ", chat.GhiChu));
        html.push('<div class="modal-section">ANNEX EU ' + maHoa(tenRef) + "</div>");
        html.push('<div id="chi-tiet-eu"><div class="modal-loading">Đang tải nội dung Annex EU…</div></div>');
        html.push("</div>");
        moHop(html.join(""));

        $.ajax({
            url: $trang.data("url-chi-tiet"),
            data: { nguon: "EU", bang: dongEU.Bang, rowId: dongEU.RowID },
            dataType: "json",
            cache: false
        }).done(function (ct) {
            if (ct.Loi) {
                $("#chi-tiet-eu").html('<div class="notice notice-error">' + maHoa(ct.Loi) + "</div>");
                return;
            }
            // đủ các cột Annex EU như khung "Annex III, IV, V, VI" của app cũ (đối chiếu 2026-11-11)
            $("#chi-tiet-eu").html(
                '<div class="field-row">' + oTruong("REF. NO", ct.RefNo) + oTruong("CAS NUMBER", ct.CAS) + oTruong("EC NUMBER", ct.EC) + "</div>" +
                (dongEU.Annex === "IV" ? '<div class="field-row">' + oTruong("COLOUR INDEX", ct.ColourIndex) + oTruong("COLOUR", ct.MauSac) + "</div>" : "") +
                oTruong("FIELD OF APPLICATION AND/OR USE", ct.PhamViSuDung) +
                oTruong("MAXIMUM AUTHORISED CONCENTRATION IN THE FINISHED COSMETIC PRODUCT", ct.GioiHanToiDa) +
                oTruong("OTHER LIMITATIONS AND REQUIREMENTS", ct.DieuKienKhac) +
                oTruong("CONDITIONS OF USE AND WARNING WHICH MUST BE PRINTED ON THE LABELS", ct.CanhBaoNhan) +
                '<div class="field-row">' + oTruong("REGULATION", ct.Regulation) + oTruong("NGÀY CẬP NHẬT", ct.NgayCapNhat) + oTruong("CMR", ct.CMR) + "</div>" +
                oTruong("OTHER DIRECTIVES/REGULATIONS", ct.ChiThiKhac) +
                oTruong("SCCS OPINIONS", ct.SCCS) +
                oTruong("CHEMICAL/IUPAC NAME", ct.IUPAC) +
                oTruong("IDENTIFIED INGREDIENTS OR SUBSTANCES", ct.ThanhPhanNhanDien));
        }).fail(function (xhr) {
            $("#chi-tiet-eu").html('<div class="notice notice-error">Lỗi khi tải dữ liệu: không kết nối được máy chủ (mã ' + xhr.status + ").</div>");
        });
    }

    // ===== Hộp chi tiết ASEAN (bấm "Xem chi tiết" hoặc ô Annex/Ref ở xem Kết quả) =====

    function moHopChiTietAsean(khoaAsean, tenNhap) {
        var dong = ketQua.asean[khoaAsean];
        if (!dong)
            return;
        var trangThai = (dong.TrangThaiHieuLuc && dong.TrangThaiHieuLuc !== "Đang áp dụng") ? " · " + dong.TrangThaiHieuLuc : "";
        var tieuDe = dong.Annex === "IV" ? "Colour Index " + dong.RefNo + (layMauAnnexIV(dong) ? " (" + layMauAnnexIV(dong) + ")" : "") : lamGon(dong.TenChat);

        moHop('<div class="modal-head"><div>' +
            '<h3 class="modal-title">' + maHoa(tieuDe) + "</h3>" +
            '<div class="modal-sub">Annex ' + maHoa(dong.Annex) + ' · Tên nhập: "' + maHoa(tenNhap) + '"' + maHoa(trangThai) + "</div>" +
            '</div><button type="button" class="modal-close" aria-label="Đóng">×</button></div>' +
            '<div class="modal-body" id="chi-tiet-asean"><div class="modal-loading">Đang tải nội dung Annex ASEAN…</div></div>');

        $.ajax({
            url: $trang.data("url-chi-tiet"),
            data: { nguon: "ACD", bang: dong.Bang, rowId: dong.RowID },
            dataType: "json",
            cache: false
        }).done(function (ct) {
            if (ct.Loi) {
                $("#chi-tiet-asean").html('<div class="notice notice-error">' + maHoa(ct.Loi) + "</div>");
                return;
            }
            var html = [];
            html.push('<div class="field-row">' + oTruong(dong.Annex === "IV" ? "COLOUR INDEX" : "REF. NO", ct.RefNo) +
                (dong.Annex === "II" ? oTruong("CAS NUMBER", ct.CAS) : "") + "</div>");
            if (dong.Annex !== "II") {
                html.push(oTruong("FIELD OF APPLICATION AND/OR USE", ct.PhamViSuDung));
                html.push(oTruong("MAXIMUM AUTHORISED CONCENTRATION IN THE FINISHED COSMETIC PRODUCT", ct.GioiHanToiDa));
                html.push(oTruong("OTHER LIMITATIONS AND REQUIREMENTS", ct.DieuKienKhac));
                html.push(oTruong("CONDITIONS OF USE AND WARNING WHICH MUST BE PRINTED ON THE LABELS", ct.CanhBaoNhan));
            }
            html.push(oTruong("NOTE", ct.GhiChu));
            // các ô ngày và căn cứ chỉ hiện khi có dữ liệu
            if (lamGon(ct.NgayApDung) || lamGon(ct.NgayHetHieuLuc) || lamGon(ct.CanCu)) {
                html.push('<div class="field-row">' + oTruong("NGÀY ÁP DỤNG", ct.NgayApDung) + oTruong("NGÀY HẾT HIỆU LỰC", ct.NgayHetHieuLuc) +
                    oTruong("CĂN CỨ", ct.CanCu) + "</div>");
            }
            $("#chi-tiet-asean").html(html.join(""));
        }).fail(function (xhr) {
            $("#chi-tiet-asean").html('<div class="notice notice-error">Lỗi khi tải dữ liệu: không kết nối được máy chủ (mã ' + xhr.status + ").</div>");
        });
    }

    // một ô chỉ đọc trong hộp chi tiết; trống thì hiện chữ nghiêng "Không có dữ liệu"
    function oTruong(nhan, giaTri) {
        var co = lamGon(giaTri) !== "";
        return '<div class="field"><div class="field-label">' + nhan + "</div>" +
            '<div class="field-value' + (co ? "" : " field-empty") + '">' + (co ? maHoaNhieuDong(giaTri) : "Không có dữ liệu") + "</div></div>";
    }

    // lop: class thêm cho khung hộp (vd "file-dialog" cho hộp xem trước file, rộng hơn)
    function moHop(html, lop) {
        $("#hop-chi-tiet-v2 .modal-dialog").attr("class", "modal-dialog" + (lop ? " " + lop : ""));
        $("#noi-dung-hop").html(html);
        $("#hop-chi-tiet-v2").removeClass("an");
        $("body").addClass("modal-open");
    }

    function dongHop() {
        $("#hop-chi-tiet-v2").addClass("an");
        $("body").removeClass("modal-open");
    }

    // ===== Sự kiện khung kết quả =====

    $("#nut-tra-cuu").on("click", traCuuTatCa);

    $("#hang-tab").on("click", ".tab-btn", function () {
        giaoDien.tab = $(this).data("tab");
        veKetQua();
    });

    $("#cong-tac-che-do").on("click", ".mode-btn", function () {
        giaoDien.cheDo = $(this).data("chedo");
        veKetQua();
    });

    // tab con: EU (Kết quả, Dò chính tả, ...), ASEAN (Kết quả, Annex) và các tab Annex
    $("#noi-dung-ket-qua").on("click", ".sub-tab", function () {
        var $tab = $(this);
        if ($tab.attr("data-xem-eu"))
            giaoDien.xemEU = $tab.attr("data-xem-eu");
        else if ($tab.attr("data-xem-asean"))
            giaoDien.xemAsean = $tab.attr("data-xem-asean");
        else if ($tab.attr("data-annex"))
            giaoDien.annexChon = $tab.attr("data-annex");
        veKetQua();
    });


    // "xem thêm" / "thu gọn" cột Thông tin
    $("#noi-dung-ket-qua").on("click", ".more-link", function () {
        var $o = $(this).prev(".info-clamp");
        var moRong = !$o.hasClass("mo-rong");
        $o.toggleClass("mo-rong", moRong);
        $(this).text(moRong ? "thu gọn" : "xem thêm");
    });

    // ô tham chiếu EU ở bảng kết quả và trong hộp chi tiết (chuyển ref khác)
    $(document).on("click", "button.ref-btn:not(.asean-btn)", function () {
        moHopChiTietEU($(this).data("syt"), $(this).data("eu"), $(this).data("ten"));
    });

    // tab ASEAN: nút "Xem chi tiết" và ô Annex/Ref ở xem Kết quả
    $(document).on("click", "button.asean-btn", function () {
        moHopChiTietAsean($(this).data("asean"), $(this).data("ten"));
    });

    $("#hop-chi-tiet-v2").on("click", function (e) {
        if (e.target === this || $(e.target).closest(".modal-close").length)
            dongHop();
    });

    $(document).on("keydown", function (e) {
        if (e.key === "Escape" && !$("#hop-chi-tiet-v2").hasClass("an"))
            dongHop();
    });

    // ===== Đọc file công thức (.docx, .csv, .xlsx, .xls) =====
    // đọc ngay trên trình duyệt (SheetJS, Scripts/xlsx.full.min.js), tự đoán cột tên rồi hiện hộp xem trước;
    // bấm "Thêm N tên" mới nối vào cuối grid, không tự tra (chờ bấm Tra cứu)

    var FILE_TOI_DA = 10 * 1024 * 1024;   // 10 MB
    var SO_DONG_XEM_TRUOC = 10;
    var SO_TEN_TRA_TOI_DA = 1000;          // giống SoTenToiDa ở TraCuuController
    // từ khóa tìm cột tên trong dòng tiêu đề (đã bỏ dấu, chữ thường), ưu tiên từ đầu danh sách
    var TU_KHOA_COT_TEN = ["inci", "ingredient", "thanh phan", "ten chat", "hoat chat", "nguyen lieu", "substance", "ten", "name"];

    // file đang xem trước: { tenFile, dsSheet: [{ ten, dsDong, dsSoDong }], sheet, cot, coTieuDe, dongTieuDe, tachPhay }
    var fileDoc = null;

    // bỏ dấu tiếng Việt, chữ thường, gọn khoảng trắng (để so tiêu đề cột)
    function boDau(value) {
        var text = lamGon(value).toLowerCase().replace(/đ/g, "d");
        if (text.normalize)
            text = text.normalize("NFD").replace(/[̀-ͯ]/g, "");
        return text;
    }

    // tên cột kiểu Excel: 0 -> A, 25 -> Z, 26 -> AA
    function tenCotExcel(i) {
        var ten = "";
        for (i = i + 1; i > 0; i = Math.floor((i - 1) / 26))
            ten = String.fromCharCode(65 + (i - 1) % 26) + ten;
        return ten;
    }

    // tên cột trong hộp xem trước: Word đánh số 1, 2, 3; Excel/CSV dùng A, B, C
    function tenCot(i) {
        return fileDoc && fileDoc.laWord ? String(i + 1) : tenCotExcel(i);
    }

    // CSV: thử UTF-8 trước; hỏng dấu (byte không hợp lệ) thì đọc lại theo Windows-1258 (Excel tiếng Việt hay lưu kiểu này)
    function giaiMaCsv(bytes) {
        try {
            return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
        } catch (e) {
            return new TextDecoder("windows-1258").decode(bytes);
        }
    }

    // đọc workbook thành các sheet có dữ liệu: { ten, dsDong (mảng dòng, mỗi dòng là mảng chuỗi), dsSoDong (số dòng trong Excel) }
    function docWorkbook(buffer, laCsv) {
        var bytes = new Uint8Array(buffer);
        var wb = laCsv
            ? XLSX.read(giaiMaCsv(bytes), { type: "string", raw: true })
            : XLSX.read(bytes, { type: "array" });
        var dsSheet = [];
        $.each(wb.SheetNames, function (i, ten) {
            var ws = wb.Sheets[ten];
            if (!ws || !ws["!ref"])
                return;
            // giữ cả dòng trống khi đọc để biết số dòng thật, rồi mới bỏ dòng trống
            var dongDau = XLSX.utils.decode_range(ws["!ref"]).s.r;
            var tatCa = XLSX.utils.sheet_to_json(ws, { header: 1, raw: false, defval: "", blankrows: true });
            var sheet = { ten: ten, dsDong: [], dsSoDong: [] };
            $.each(tatCa, function (r, dong) {
                var oChu = $.map(dong, function (o) { return lamGon(o); });
                if (oChu.join("") === "")
                    return;
                sheet.dsDong.push(oChu);
                sheet.dsSoDong.push(dongDau + r + 1);
            });
            if (sheet.dsDong.length)
                dsSheet.push(sheet);
        });
        return dsSheet;
    }

    // đọc file Word .docx: CHỈ lấy các bảng (chữ ngoài bảng bỏ qua), mỗi bảng coi như một sheet;
    // .docx là file nén zip, giải nén bằng XLSX.CFB (có sẵn trong SheetJS) rồi đọc word/document.xml
    function docDocx(buffer) {
        var zip = XLSX.CFB.read(new Uint8Array(buffer), { type: "array" });
        var tep = XLSX.CFB.find(zip, "/word/document.xml") || XLSX.CFB.find(zip, "word/document.xml");
        if (!tep || !tep.content)
            throw new Error("Không phải tệp Word .docx");
        var xml = new TextDecoder("utf-8").decode(new Uint8Array(tep.content));
        var doc = new DOMParser().parseFromString(xml, "application/xml");
        var W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        // các con trực tiếp có tên thẻ (bỏ tiền tố w:)
        function conTrucTiep(node, ten) {
            var kq = [];
            for (var n = node.firstChild; n; n = n.nextSibling) {
                if (n.nodeType === 1 && n.localName === ten)
                    kq.push(n);
            }
            return kq;
        }

        // chữ của một ô: mỗi đoạn (paragraph, xuống dòng bằng Enter) một dòng; layTenTuFile coi mỗi dòng là một tên
        function chuCuaO(o) {
            var dsDoan = [];
            $.each(o.getElementsByTagNameNS(W, "p"), function (i, p) {
                var chu = "";
                $.each(p.getElementsByTagNameNS(W, "*"), function (j, n) {
                    if (n.localName === "t")
                        chu += n.textContent;
                    else if (n.localName === "tab" || n.localName === "br")
                        chu += " ";
                });
                chu = lamGon(chu);
                if (chu !== "")
                    dsDoan.push(chu);
            });
            return dsDoan.join("\n");
        }

        var dsSheet = [];
        $.each(doc.getElementsByTagNameNS(W, "tbl"), function (i, bang) {
            var sheet = { ten: "Bảng " + (i + 1), dsDong: [], dsSoDong: [] };
            $.each(conTrucTiep(bang, "tr"), function (r, dong) {
                var oChu = $.map(conTrucTiep(dong, "tc"), function (o) { return chuCuaO(o); });
                if (oChu.join("") === "")
                    return;
                sheet.dsDong.push(oChu);
                sheet.dsSoDong.push(r + 1);
            });
            if (sheet.dsDong.length) {
                sheet.ten += " (" + sheet.dsDong.length + " dòng)";
                dsSheet.push(sheet);
            }
        });
        return dsSheet;
    }

    // đoán cột tên: tìm dòng tiêu đề trong 10 dòng đầu theo từ khóa;
    // không thấy thì lấy cột có nhiều ô chữ nhất (bỏ cột STT, cột số %)
    function doanCot(dsDong) {
        var soDong = Math.min(dsDong.length, SO_DONG_XEM_TRUOC);
        for (var k = 0; k < TU_KHOA_COT_TEN.length; k++) {
            for (var r = 0; r < soDong; r++) {
                for (var c = 0; c < dsDong[r].length; c++) {
                    var tieuDe = boDau(dsDong[r][c]);
                    // tiêu đề là cụm ngắn chứa từ khóa (tránh nhầm với một tên chất dài)
                    if (tieuDe.length <= 40 && (" " + tieuDe + " ").indexOf(" " + TU_KHOA_COT_TEN[k]) >= 0)
                        return { cot: c, coTieuDe: true, dongTieuDe: r, uuTien: k };
                }
            }
        }

        var diem = [];
        $.each(dsDong, function (r, dong) {
            $.each(dong, function (c, o) {
                if (/[A-Za-zÀ-ỹ]/.test(o) && o.length >= 2)
                    diem[c] = (diem[c] || 0) + 1;
            });
        });
        var cotTot = 0;
        for (var i = 0; i < diem.length; i++) {
            if ((diem[i] || 0) > (diem[cotTot] || 0))
                cotTot = i;
        }
        return { cot: cotTot, coTieuDe: false, dongTieuDe: 0 };
    }

    // tách một ô kiểu nhãn sản phẩm "Aqua, Glycerin; Parfum." theo ", " và ";"
    // (phẩy không có khoảng trắng phía sau thì giữ, vd "1,4-Dihydroxybenzene")
    function tachOTheoPhay(o) {
        return $.map(o.split(/\s*;\s*|,\s+/), function (phan) {
            phan = lamGon(phan).replace(/\.$/, "");
            return phan === "" ? null : phan;
        });
    }

    // cột trông giống danh sách nhãn sản phẩm (có ô gồm từ 3 phần trở lên) thì gợi ý bật tách theo dấu phẩy
    function nenTachPhay(dsDong, cot, batDau) {
        for (var r = batDau; r < dsDong.length; r++) {
            if (tachOTheoPhay(dsDong[r][cot] || "").length >= 3)
                return true;
        }
        return false;
    }

    // các tên sẽ thêm theo lựa chọn hiện tại trong hộp xem trước
    function layTenTuFile() {
        var sheet = fileDoc.dsSheet[fileDoc.sheet];
        var batDau = fileDoc.coTieuDe ? fileDoc.dongTieuDe + 1 : 0;
        var ds = [];
        for (var r = batDau; r < sheet.dsDong.length; r++) {
            // ô bảng Word có nhiều dòng: mỗi dòng một tên (ô Excel/CSV chỉ có một dòng)
            $.each(String(sheet.dsDong[r][fileDoc.cot] || "").split("\n"), function (j, o) {
                $.each(fileDoc.tachPhay ? tachOTheoPhay(o) : [lamGon(o)], function (i, ten) {
                    // bỏ ô trống và ô chỉ có số (STT, hàm lượng %)
                    if (ten !== "" && !/^[\d\s.,%]+$/.test(ten))
                        ds.push(ten);
                });
            });
        }
        return ds;
    }

    // chọn sheet: đoán cột và gợi ý tách theo dấu phẩy cho sheet đó
    function chonSheet(i) {
        var sheet = fileDoc.dsSheet[i];
        var doan = doanCot(sheet.dsDong);
        fileDoc.sheet = i;
        fileDoc.cot = doan.cot;
        fileDoc.coTieuDe = doan.coTieuDe;
        fileDoc.dongTieuDe = doan.dongTieuDe;
        fileDoc.tachPhay = nenTachPhay(sheet.dsDong, doan.cot, doan.coTieuDe ? doan.dongTieuDe + 1 : 0);
    }

    // sheet (bảng Word) mở sẵn: sheet có tiêu đề khớp từ khóa ưu tiên nhất (INCI, thành phần... trước "tên"),
    // bằng nhau thì sheet nhiều dòng hơn; không sheet nào có tiêu đề thì lấy sheet nhiều dòng nhất
    function timSheetTot(dsSheet) {
        var tot = 0;
        var uuTienTot = TU_KHOA_COT_TEN.length;
        for (var i = 0; i < dsSheet.length; i++) {
            var doan = doanCot(dsSheet[i].dsDong);
            var uuTien = doan.coTieuDe ? doan.uuTien : TU_KHOA_COT_TEN.length;
            if (uuTien < uuTienTot || (uuTien === uuTienTot && dsSheet[i].dsDong.length > dsSheet[tot].dsDong.length)) {
                tot = i;
                uuTienTot = uuTien;
            }
        }
        return tot;
    }

    // vẽ (lại) nội dung hộp xem trước theo fileDoc
    function veHopFile() {
        var sheet = fileDoc.dsSheet[fileDoc.sheet];
        var soCot = 0;
        $.each(sheet.dsDong, function (r, dong) { soCot = Math.max(soCot, dong.length); });
        var dsTen = layTenTuFile();

        // số tên khác nhau sau khi thêm, để báo trước nếu vượt giới hạn một lần tra
        var daCo = {};
        var soKhac = 0;
        $.each($.map(duLieu.dsDong, function (dong) { return dong.ten; }).concat(dsTen), function (i, ten) {
            var khoa = khoaTen(ten);
            if (khoa !== "" && !daCo[khoa]) {
                daCo[khoa] = true;
                soKhac++;
            }
        });

        var html = [];
        html.push('<div class="modal-head"><div><h3 class="modal-title">Thêm thành phần từ tệp</h3>' +
            '<div class="modal-sub">' + maHoa(fileDoc.tenFile) + " · " + sheet.dsDong.length + " dòng có dữ liệu</div></div>" +
            '<button type="button" class="modal-close" aria-label="Đóng">×</button></div>');
        html.push('<div class="modal-body">');

        // hàng lựa chọn: sheet, cột, dòng tiêu đề, tách theo dấu phẩy
        html.push('<div class="file-options">');
        if (fileDoc.dsSheet.length > 1) {
            html.push('<label class="file-opt">' + (fileDoc.laWord ? "Bảng" : "Sheet") + ' <select id="file-sheet">' + $.map(fileDoc.dsSheet, function (s, i) {
                return '<option value="' + i + '"' + (i === fileDoc.sheet ? " selected" : "") + ">" + maHoa(s.ten) + "</option>";
            }).join("") + "</select></label>");
        }
        var dongMau = sheet.dsDong[fileDoc.coTieuDe ? fileDoc.dongTieuDe : 0] || [];
        html.push('<label class="file-opt">Cột tên thành phần <select id="file-cot">');
        for (var c = 0; c < soCot; c++) {
            var mau = dongMau[c] || "";
            html.push('<option value="' + c + '"' + (c === fileDoc.cot ? " selected" : "") + ">Cột " + tenCot(c) +
                (mau ? " - " + maHoa(mau.length > 40 ? mau.substring(0, 40) + "…" : mau) : "") + "</option>");
        }
        html.push("</select></label>");
        html.push('<label class="file-check" title="Bỏ qua dòng tiêu đề và các dòng phía trên nó"><input type="checkbox" id="file-tieu-de"' +
            (fileDoc.coTieuDe ? " checked" : "") + "> Có dòng tiêu đề (dòng " + sheet.dsSoDong[fileDoc.dongTieuDe] + ")</label>");
        html.push('<label class="file-check" title="Ô chứa nhiều thành phần như nhãn sản phẩm: tách tại &quot;, &quot; và &quot;;&quot; (không tách 1,4-...)">' +
            '<input type="checkbox" id="file-tach-phay"' + (fileDoc.tachPhay ? " checked" : "") + "> Tách theo dấu phẩy</label>");
        html.push("</div>");

        // bảng xem trước các dòng đầu, cột đã chọn tô màu, dòng bị bỏ qua làm mờ
        html.push('<div class="file-preview"><table class="file-table"><thead><tr><th></th>');
        for (c = 0; c < soCot; c++)
            html.push('<th class="' + (c === fileDoc.cot ? "col-chon" : "") + '">' + tenCot(c) + "</th>");
        html.push("</tr></thead><tbody>");
        $.each(sheet.dsDong.slice(0, SO_DONG_XEM_TRUOC), function (r, dong) {
            var boQua = fileDoc.coTieuDe && r <= fileDoc.dongTieuDe;
            html.push('<tr class="' + (boQua ? "row-skip" : "") + '"><td class="row-no">' + sheet.dsSoDong[r] + "</td>");
            for (var cc = 0; cc < soCot; cc++)
                html.push('<td class="' + (cc === fileDoc.cot ? "col-chon" : "") + '">' + maHoaNhieuDong(dong[cc] || "") + "</td>");
            html.push("</tr>");
        });
        html.push("</tbody></table></div>");
        if (sheet.dsDong.length > SO_DONG_XEM_TRUOC)
            html.push('<div class="file-more">… và ' + (sheet.dsDong.length - SO_DONG_XEM_TRUOC) + " dòng nữa</div>");

        // các tên sẽ thêm
        html.push('<div class="file-result"><div class="file-result-head">Sẽ thêm <b>' + dsTen.length + "</b> tên vào cuối danh sách</div>");
        if (dsTen.length) {
            html.push('<div class="file-names">' + $.map(dsTen.slice(0, 30), function (ten) {
                return '<span class="file-name">' + maHoa(ten) + "</span>";
            }).join("") + (dsTen.length > 30 ? '<span class="file-name-more">… và ' + (dsTen.length - 30) + " tên khác</span>" : "") + "</div>");
        } else {
            html.push('<p class="muted">Cột này không có tên nào. Hãy chọn cột khác.</p>');
        }
        html.push("</div>");
        if (soKhac > SO_TEN_TRA_TOI_DA)
            html.push('<div class="notice notice-warn">Sau khi thêm, danh sách có ' + soKhac + " tên khác nhau, vượt giới hạn " +
                SO_TEN_TRA_TOI_DA + " tên mỗi lần tra. Hãy bớt tên trước khi bấm Tra cứu.</div>");

        html.push('<div class="file-actions"><button type="button" class="btn btn-outline" id="file-huy">Hủy</button>' +
            '<button type="button" class="btn btn-primary" id="file-them"' + (dsTen.length ? "" : " disabled") + ">Thêm " + dsTen.length + " tên</button></div>");
        html.push("</div>");

        if ($("#hop-chi-tiet-v2").hasClass("an"))
            moHop(html.join(""), "file-dialog");
        else
            $("#noi-dung-hop").html(html.join(""));
    }

    // đọc file người dùng chọn rồi mở hộp xem trước
    function docFile(file) {
        var phanMoRong = (/\.([^.]+)$/.exec(file.name) || ["", ""])[1].toLowerCase();
        if (phanMoRong === "doc") {
            hienThongBao("Chưa đọc được tệp Word 97-2003 (.doc). Hãy mở bằng Word và lưu lại dạng .docx.");
            return;
        }
        if ($.inArray(phanMoRong, ["docx", "csv", "xlsx", "xls"]) < 0) {
            hienThongBao("Chỉ đọc được tệp .docx, .xlsx, .xls hoặc .csv.");
            return;
        }
        if (file.size > FILE_TOI_DA) {
            hienThongBao("Tệp quá lớn (tối đa 10 MB).");
            return;
        }
        if (typeof XLSX === "undefined" || typeof TextDecoder === "undefined") {
            hienThongBao("Trình duyệt chưa tải được thư viện đọc file. Hãy tải lại trang (F5).");
            return;
        }

        var reader = new FileReader();
        reader.onload = function () {
            var dsSheet;
            var laWord = (phanMoRong === "docx");
            try {
                dsSheet = laWord ? docDocx(reader.result) : docWorkbook(reader.result, phanMoRong === "csv");
            } catch (e) {
                hienThongBao("Không đọc được tệp " + file.name + " (tệp hỏng, có mật khẩu hoặc sai định dạng).");
                return;
            }
            if (!dsSheet.length) {
                hienThongBao(laWord ? "Tệp " + file.name + " không có bảng nào." : "Tệp " + file.name + " không có dữ liệu.");
                return;
            }
            fileDoc = { tenFile: file.name, dsSheet: dsSheet, laWord: laWord };
            chonSheet(timSheetTot(dsSheet));
            veHopFile();
        };
        reader.onerror = function () {
            hienThongBao("Không đọc được tệp " + file.name + ".");
        };
        reader.readAsArrayBuffer(file);
    }

    // chế độ nhập: Nhập trực tiếp (gõ, nút Dán) / Nhập từ tệp (vùng chọn tệp); grid dùng chung cho cả hai
    $("#che-do-nhap").on("click", ".mode-btn", function () {
        var cheDoNhap = $(this).data("nhap");
        $("#che-do-nhap .mode-btn").each(function () {
            $(this).toggleClass("active", $(this).data("nhap") === cheDoNhap);
        });
        $("#vung-nhap-tay").toggleClass("an", cheDoNhap !== "tay");
        $("#vung-tai-file").toggleClass("an", cheDoNhap !== "file");
    });

    $("#vung-tha-file").on("click", function () {
        $("#chon-file").val("").trigger("click");
    });

    // kéo thả tệp vào vùng Nhập từ tệp
    $("#vung-tha-file").on("dragover dragenter", function (e) {
        e.preventDefault();
        $(this).addClass("keo-tha");
    }).on("dragleave drop", function (e) {
        e.preventDefault();
        $(this).removeClass("keo-tha");
        var dt = (e.originalEvent || e).dataTransfer;
        if (e.type === "drop" && dt && dt.files && dt.files.length)
            docFile(dt.files[0]);
    });

    $("#chon-file").on("change", function () {
        if (this.files && this.files.length)
            docFile(this.files[0]);
    });

    $("#noi-dung-hop").on("change", "#file-sheet", function () {
        chonSheet(parseInt($(this).val(), 10));
        veHopFile();
    });

    $("#noi-dung-hop").on("change", "#file-cot", function () {
        fileDoc.cot = parseInt($(this).val(), 10);
        fileDoc.tachPhay = nenTachPhay(fileDoc.dsSheet[fileDoc.sheet].dsDong, fileDoc.cot, fileDoc.coTieuDe ? fileDoc.dongTieuDe + 1 : 0);
        veHopFile();
    });

    $("#noi-dung-hop").on("change", "#file-tieu-de", function () {
        fileDoc.coTieuDe = this.checked;
        veHopFile();
    });

    $("#noi-dung-hop").on("change", "#file-tach-phay", function () {
        fileDoc.tachPhay = this.checked;
        veHopFile();
    });

    $("#noi-dung-hop").on("click", "#file-huy", function () {
        dongHop();
    });

    // nối vào cuối danh sách, không tự tra (thêm tên mới thì chờ người dùng bấm Tra cứu)
    $("#noi-dung-hop").on("click", "#file-them", function () {
        var dsTen = layTenTuFile();
        if (!dsTen.length)
            return;
        if (giaoDien.idDangSua !== null)
            luuTenDangSua();
        themTen(dsTen);
        dongHop();
        hienThongBao("Đã thêm " + dsTen.length + " tên từ tệp " + fileDoc.tenFile);
        cuonXuongCuoi();
        $("#o-nhap-moi").focus();
    });

    // ===== Khởi động =====
    docDuLieu();
    veLai();
    veKetQua();
    // mở lại trang (F5) mà còn danh sách đã lưu thì tự tra lại
    if (duLieu.dsDong.length)
        goiTraCuu($.map(layDsTen(), function (item) { return item.ten; }), true);
    $("#o-nhap-moi").focus();
});
