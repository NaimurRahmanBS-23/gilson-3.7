$(function () {

    $(document).on("nopAjaxFiltersFiltrationCompleteEvent", function () {
        $(".pager").show();
        if ($("#pans").attr("class") === "filterItemSelected" || $("#covers").attr("class") === "filterItemSelected") {
            getPansCovers();
        }

        //Handle back button for pans / covers
        if ($("#optionIDs").val() === "") {
            if ($("#pansChecked").val() === "1") {
                if ($("#coversChecked").val() === "1") {
                    $("#covers").attr("class", "filterItemSelected");
                }
                $("#pans").click();
            }
            else if ($("#coversChecked").val() === "1") {
                $("#covers").click();
            }
        } 
    });
    var pc = "Pans & Covers";
    var pco = '<li><a id="pans" class="filterItemUnselected">Pans</a></li><li><a id="covers" class="filterItemUnselected">Covers</a></li>';
    if ($("h1").text().indexOf("Trays") != -1) {
        pc = "Pans";
        pco = '<li><a id="pans" class="filterItemUnselected">Pans</a></li>';
    }
    $(".filtersPanel").append('<div class="block filter-block specificationFilterPanel7Spikes"><div class="title"><a class="toggleControl">' + pc + '</a><a id="clearPansCovers" class="clearFilterOptions" title="Clear selected [Pans & Covers]" style="display: none;">Clear</a></div><div class="filtersGroupPanel"><ul class="checkbox-list">' + pco + '</ul><form id="frmSelectedOptionIDs" action="" method="post"><input id="optionIDs" name="optionIDs" type="hidden" /></form></div></div>');

    $("#pans, #covers").click(function () {
        if ($(this).attr("class") === "filterItemUnselected") {
            $(this).attr("id") === "pans" ? $("#pansChecked").val("1") : $("#coversChecked").val("1");
            $(this).attr("class", "filterItemSelected");

            if ($(".panRow, .coverRow").length > 0) {
                if (hideSieves) {
                    $(".sieveRow, .trayRow").hide();
                    $(".pager").hide();
                }
                $(this).attr("id") === "pans" ? $(".panRow").show() : $(".coverRow").show();
            }
            else {
                getPansCovers();
            }
            $("#clearPansCovers").show();
        }
        else {
            $(this).attr("id") === "pans" ? $("#pansChecked").val("0") : $("#coversChecked").val("0");
            $(this).attr("class", "filterItemUnselected");

            if ($("#pans").attr("class") === "filterItemUnselected" && $("#covers").attr("class") === "filterItemUnselected") {
                $(".sieveRow, .trayRow").show();
                $(".panRow, .coverRow").hide();
                $(".pager").show();
            }
            else {
                $($(this).attr("id") === "pans" ? ".panRow" : ".coverRow").each(function () { $(this).find(".qty-input").val(""); });
                $(this).attr("id") === "pans" ? $(".panRow").hide() : $(".coverRow").hide();
            }
            if ($("#pans, #covers").attr("class") === "filterItemUnselected") {
                $("#clearPansCovers").hide();
            }
        }
    });
    $("#clearPansCovers").click(function () {
        $(".sieveRow, .trayRow").show();
        $("#pans, #covers").attr("class", "filterItemUnselected");
        $(".panRow, .coverRow").hide();
        $(this).hide();
    });
});

var hideSieves = true;
function getPansCovers() {
    var selected = [];
    var a = false;
    hideSieves = true;

    $(".nopAjaxFilters7Spikes .filtersGroupPanel").each(function (b, c) {
        var g = $(this).attr("id");
        $(c).find("a[data-option-id], a[data-optionsgroupId]").each(function (a, b) {
            if ($(b).attr("class") === "filterItemSelected") {
                selected.push($(b).attr("data-option-id"));
                if (g === "opening-size")
                    hideSieves = false;
            }
        });
    });

    $("#optionIDs").val(selected.join());
    $.ajax({
        cache: false,
        url: "getpanscovers/" + $(".nopAjaxFilters7Spikes").attr("data-categoryId"),
        data: $("#frmSelectedOptionIDs").serialize(),
        type: "post",
        beforeSend: showAjaxBusy,
        success: pansCoversSuccess,
        error: pansCoversFailure
    });
}

function showAjaxBusy() {
    var a = $(".page").height(),
        b = $(".page").width();
    $(".productPanelAjaxBusy").height(a);
    $(".productPanelAjaxBusy").width(b);
    $(".productPanelAjaxBusy").show();
    displayAjaxLoading(true);
}

function pansCoversSuccess(response) {
    if (response.updatecoverspans) {
        $(".products-table tbody").append(response.updatecoverspans);

        $(".panRow, .coverRow").each(function () {if ($(".read-more-topic").length > 0) $(this).append("<td></td>");});

        $(".panRow td .qty-input, .coverRow td .qty-input")
            .focus(function() { $(this).select(); })
            .bind("cut copy contextmenu paste", function(e) { e.preventDefault(); })
            .on("input",
                function() {
                    q = $(this);
                    if (isNaN(q.val()) || Number(q.val()) <= 0) {
                        q.val("");
                    }
                });

        if (hideSieves) {
            $(".sieveRow, .trayRow").hide();
            $(".pager").hide();
        }

        if ($("#pans").attr("class") === "filterItemUnselected") {
            $(".panRow").hide();
        }
        if ($("#covers").attr("class") === "filterItemUnselected") {
            $(".coverRow").hide();
        }
    }

    $(".productPanelAjaxBusy").hide();
     displayAjaxLoading(false);
}

function pansCoversFailure() {
    $(".productPanelAjaxBusy").hide();
    displayAjaxLoading(false);
    alert("Failed to get selected products.");
}