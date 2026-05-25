$(function () {

    $(".qty-input")
        .focus(function() { $(this).select();})
        .bind("cut copy contextmenu paste", function (e) { e.preventDefault();})
        .keydown(function(event) {if (event.keyCode == 13) {$("#add-to-cart-button").click();return false;}})
        .keyup(function () {$("#verification-qty").val($(this).val());if ($(this).val().length == 0) $(this).val("1");})
        .forceNumeric();
    
    $("#add-to-cart-button").click(function () {
        AjaxCart.addsievestraystocart("#product-details-form"); return false;
    });

});