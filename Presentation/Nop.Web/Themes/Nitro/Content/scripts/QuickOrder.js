var QuickOrderIDs = 10;
$(function () {

    $('.quick-order input[type="number"]').focus(function () { $(this).select(); });
    $('.quick-order input[type="number"]').forceNumeric();
    $('.quick-order input[type="number"]').bind("cut copy contextmenu paste", function (e) { e.preventDefault(); });

    $("#btnNewRow").click(function () {
        var lastRow = $("#QO" + (QuickOrderIDs));
        var cloned = $(lastRow).clone();
        QuickOrderIDs++;
        $(cloned).attr("id", "QO" + QuickOrderIDs);
        cloned.find('input:text, input[type="number"]').each(function () {
            var id = $(this).attr("id");
            id = id.substring(0, id.length - (QuickOrderIDs - 1).toString().length) + QuickOrderIDs;
            $(this).attr("id", id);
            $(this).attr("name", id);
            $(this).val("");
        });
        cloned.insertAfter(lastRow);
    });

    $("#btnQuickOrder").click(function () {
        var e = 0;
        var q = 0;
        for (var i = 1; i <= QuickOrderIDs; i++) {
            if ($.trim($("#txtModel" + i).val()).length == 0 && $.trim($("#txtQty" + i).val()).length > 0) {
                $("#txtModel" + i).css("border", "1px solid red");
                e++;
            }
            else { $("#txtModel" + i).css("border", "1px solid #d3d3d3"); }
            if ($.trim($("#txtQty" + i).val()).length == 0 && $.trim($("#txtModel" + i).val()).length > 0) {
                $("#txtQty" + i).css("border", "1px solid red");
                e++;
            }
            else { $("#txtQty" + i).css("border", "1px solid #d3d3d3"); }
            if ($.trim($("#txtModel" + i).val()).length >= 0 && $.trim($("#txtQty" + i).val()).length > 0) { q++; }
        }

        if ((e == 0 && q > 0)) {
            AjaxCart.addquickordertocart('#frmQuickOrder');
        }
        else { displayBarNotification('Quick Order is missing a required field.', 'error', 0); }
    });

    $("#btnViewCart").click(function() { setLocation('/cart'); });
});
function handleInvalidModels(m) {
    for (var i = 0; i <= m.length - 1; i++) {
        var e = $("input").filter(function () { return $(this).val() === m[i]; });
        $(e).css("border", "1px solid red");
    }
}