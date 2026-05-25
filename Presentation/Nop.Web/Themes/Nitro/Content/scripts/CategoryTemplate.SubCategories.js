$(document).on("nopAjaxFiltersFiltrationCompleteEvent", function () {
    var c = $(".sub-category-grid");
    var p = $("#products-container");

    if ($(".filterItemSelected").length >= 1) {
        c.hide();
        p.show();
    }
    else {
        c.show();
        p.hide();
    }
});