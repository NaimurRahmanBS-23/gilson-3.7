$(function () {
	$(".clear-all").click(function () {
		$(".qty-input").val("");
	});

	$(".add-to-cart").click(function () {
	    AjaxCart.addcuttoordertocart("#products-table-form"); return false;
	});

	initCTO();
});

$(document).on("nopAjaxFiltersFiltrationCompleteEvent", function () { initCTO(); });

function initCTO() {
    $(".products-table caption").html($(".page-title h1").html());
    $(".qty-input").focus(function() { $(this).select(); })
        .bind("cut copy contextmenu paste", function (e) { e.preventDefault(); })
        .forceNumeric()
        .on("input", function() {
            var id = $(this).attr("id").replace("length", "").replace("width", "").replace("pieceqty", "");
        	var l = $("#length"+id);
        	var w = $("#width"+id);
        	var pq = $("#pieceqty" + id);
        	if (Number(w.val()) > 46) w.val(46);
        	if (l.val().length > 0 && w.val().length > 0 && pq.val().length > 0) {
        	    var sf = (Math.ceil((l.val() * w.val()) / 144) * pq.val());
        	    var tp = sf * $("#price"+id).text().replace("$", "");
        	    $("#qty"+id).val(sf);
        	    $("#tp"+id).text(tp);
        	    $("#tp"+id).formatCurrency();
        	}
        	else {
        	    $("#qty"+id).val("");
        	    $("#tp"+id).text("");
        	}
    });
}