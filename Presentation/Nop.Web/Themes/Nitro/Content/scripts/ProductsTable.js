$(function () {
	$(".clear-all").click(function () {
		$(".qty-input").val("");
	});

	$(".add-to-cart").click(function () {
	    AjaxCart.addsievestraystocart("#products-table-form"); return false;
	});

	initProductsTable();
});

$(document).on("nopAjaxFiltersFiltrationCompleteEvent", function () { initProductsTable(); });

function initProductsTable() {
    $(".products-table caption").html($(".page-title h1").html());

    $(".qty-input").focus(function() { $(this).select(); })
        .bind("cut copy contextmenu paste", function (e) { e.preventDefault(); })
        .forceNumeric()
        .on("input", function() {q = $(this);if (isNaN(q.val()) || Number(q.val()) <= 0) {q.val("");}
    });
}