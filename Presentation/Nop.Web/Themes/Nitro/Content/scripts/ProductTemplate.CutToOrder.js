$(function () {
    $("#add-to-cart-button").click(function () {
        AjaxCart.addcuttoordertocart("#product-details-form"); return false;
    });

    $(".qty-input").focus(function () { $(this).select(); })
        .bind("cut copy contextmenu paste", function (e) { e.preventDefault(); })
        .forceNumeric()
        .on("input", function () {
            var id = $(this).attr("id").replace("length", "").replace("width", "").replace("pieceqty", "");
            var l = $("#length"+id);
            var w = $("#width"+id);
            var pq = $("#pieceqty"+id);
            if (Number(w.val()) > 46) w.val(46);
            if (l.val().length > 0 && w.val().length > 0 && pq.val().length > 0) {
                var sf = (Math.ceil((l.val() * w.val()) / 144) * pq.val());
                var tp = sf * $("#price" + id).text().replace("$", "");
                $("#qty" + id).val(sf);
                $(".sq-ft").text(sf);
                $("#tp" + id).text(tp);
                $("#tp" + id).formatCurrency();
            }
            else {
                $(".sq-ft").text("");
                $("#qty"+id).val("1");
            }
        });
});