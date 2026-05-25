$(function () {
	$(".clear-all").click(function () {
		$(".qty-input").val("");
		if ($(".verification-select").length > 0) {
			$(".verification-select").val("0");
		}
	});

	$(".add-to-cart").click(function () {
	    AjaxCart.addsievestraystocart("#products-table-form"); return false;
	});

	initVerification();
});

$(document).on("nopAjaxFiltersFiltrationCompleteEvent", function () { initVerification(); });

function initVerification() {

    $(".products-table caption").html($(".page-title h1").html());

    $(".qty-input").focus(function() { $(this).select(); })
        .bind("cut copy contextmenu paste", function (e) { e.preventDefault(); })
        .forceNumeric()
        .on("input", function() {
        	var i = $(this).attr("id");
        	var id, q, v, vq;
        	if (i.match("^qty")) {
        		id = i.replace("qty", "");
            	q = $(this);
            	v = $("#verification" + id);
            	vq = $("#verification-qty" + id);
            	if (isNaN(q.val()) || Number(q.val()) <= 0) {
            		q.val("");
            		v.val("0");
	                vq.val("");
	            }
            } else {
        		id = i.replace("verification-qty", "");
        		q = $("#qty" + id);
        		v = $("#verification" + id);
        		vq = $(this);
        		if (isNaN(vq.val()) || Number(vq.val()) <= 0) {
		            if (v.val() !== "0") {
		                vq.val(q.val());
		                vq.select();
		            } else {
		            	vq.val("");
		            }
		        } else {
        			if (isNaN(q.val()) || Number(q.val()) <= 0) {
			                q.val(vq.val());
			        }
        			else if (Number(vq.val()) > Number(q.val())) {
			                vq.val(q.val());
			        }
		        }
            }
        });


    $(".verification-select").change(function () {
    	var i = $(this).attr("id");
    	var id = i.replace("verification", "");
    	var q = $("#qty" + id);
    	var v = $(this);
    	var vq = $("#verification-qty" + id);
		if (v.val() === "0") {
			vq.val("");
		} else {
			if (isNaN(q.val()) || Number(q.val()) <= 0) {
				q.val("1");
			}
			if (isNaN(vq.val()) || Number(vq.val()) <= 0) {
				vq.val(q.val());
			}
		}
    });
}