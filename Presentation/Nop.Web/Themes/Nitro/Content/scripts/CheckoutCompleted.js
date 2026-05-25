$(function () {
    Completed.init();
});


var Completed = {

    init: function () {

        //callback handler for form submit
        $("#completed-register-form").submit(function (e) {
            e.preventDefault(); //STOP default action

            var postData = $(this).serialize();
            var formURL = $(this).attr("action");

            if ($(this).valid()) {
                Completed.setLoadWaiting(true);
                if ($(this).valid()) {
                    $.ajax({
                        url: formURL,
                        type: "POST",
                        data: postData,
                        success: Completed.handleSaveResponse,
                        error: this.ajaxFailure
                    });
                }
            }
        });

        $("#Password").keyup(function () {
            if ($("#Password").val().trim().length == 0 || $("#Password").val().trim().length < 6) {
                $(".register-error-msg").html("");
            }
        });
    },

    setLoadWaiting: function (show) {
        if (show) {
            $("#completed-register-please-wait").show();
        } else {
            $("#completed-register-please-wait").hide();
        }
    },

    handleSaveResponse: function (response) {
        if (response.error) {
            if ((typeof response.message) == "string") {
                $(".register-error-msg").html(response.message);
            } else {
                $(".register-error-msg").html(response.message.join("<br />"));
            }
            Completed.setLoadWaiting(false);
            return;
        }

        if (response.register_success) {
            $("#completed-register-panel").html(response.register_success).css("font-weight", "700").css("color", "green").css("text-decoration", "underline");
            $(".ico-register").attr("href", "/customer/info").attr("class", "ico-account").attr("title", $("#GuestEmail").text());
            $(".ico-login").attr("href", "/logout").attr("title", "Logout");
        }
    },

    ajaxFailure: function () {
        alert("Error encountered.  Please try again or contact us for assistance.");
        this.setLoadWaiting(false);
    },

};