// Form tài khoản (Đăng ký, Admin thêm / sửa tài khoản, Tài khoản của tôi):
// người liên hệ của doanh nghiệp: thêm / bỏ dòng, đánh lại số thứ tự [0], [1]... để server đọc đúng danh sách
$(function () {
    var $ds = $("#ds-lien-he");
    if (!$ds.length)
        return;

    function danhSoLai() {
        $ds.find(".lien-he-row").each(function (i) {
            $(this).find("input").each(function () {
                this.name = this.name.replace(/DSLienHe\[\d+\]/, "DSLienHe[" + i + "]");
            });
        });
    }

    $("#nut-them-lien-he").on("click", function () {
        var $mau = $ds.find(".lien-he-row").first();
        var $moi = $mau.clone();
        $moi.find("input").val("");
        $ds.append($moi);
        danhSoLai();
        $moi.find("input").first().focus();
    });

    $ds.on("click", ".lien-he-xoa", function () {
        var $dong = $(this).closest(".lien-he-row");
        if ($ds.find(".lien-he-row").length > 1)
            $dong.remove();
        else
            $dong.find("input").val("");
        danhSoLai();
    });
});
