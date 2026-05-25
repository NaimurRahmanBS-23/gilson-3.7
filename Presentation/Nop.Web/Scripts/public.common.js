/*
** nopCommerce custom js functions
*/

jQuery.fn.forceNumeric = function () { return this.each(function () { $(this).keydown(function (e) { var key = e.which || e.keyCode; if (!e.shiftKey && !e.altKey && !e.ctrlKey && key >= 48 && key <= 57 || key >= 96 && key <= 105 || key == 8 || key == 9 || key == 13 || key == 35 || key == 36 || key == 37 || key == 39 || key == 46 || key == 45) return true; return false; }); }); }

function OpenWindow(query, w, h, scroll) {
    var l = (screen.width - w) / 2;
    var t = (screen.height - h) / 2;

    winprops = 'resizable=0, height=' + h + ',width=' + w + ',top=' + t + ',left=' + l + 'w';
    if (scroll) winprops += ',scrollbars=1';
    var f = window.open(query, "_blank", winprops);
}

function setLocation(url) {
    window.location.href = url;
}

function displayAjaxLoading(display) {
    if (display) {
        $('.ajax-loading-block-window').show();
    }
    else {
        $('.ajax-loading-block-window').hide('slow');
    }
}

function displayPopupNotification(message, messagetype, modal) {
    //types: success, error
    var container;
    if (messagetype == 'success') {
        //success
        container = $('#dialog-notifications-success');
    }
    else if (messagetype == 'error') {
        //error
        container = $('#dialog-notifications-error');
    }
    else {
        //other
        container = $('#dialog-notifications-success');
    }

    //we do not encode displayed message
    var htmlcode = '';
    if ((typeof message) == 'string') {
        htmlcode = '<p>' + message + '</p>';
    } else {
        for (var i = 0; i < message.length; i++) {
            htmlcode = htmlcode + '<p>' + message[i] + '</p>';
        }
    }

    container.html(htmlcode);

    var isModal = (modal ? true : false);
    container.dialog({modal:isModal});
}

var barNotificationTimeout;
function displayBarNotification(message, messagetype, timeout) {
    clearTimeout(barNotificationTimeout);

    //types: success, error
    var cssclass = 'success';
    if (messagetype == 'success') {
        cssclass = 'success';
    }
    else if (messagetype == 'error') {
        cssclass = 'error';
    }
    //remove previous CSS classes and notifications
    $('#bar-notification')
        .removeClass('success')
        .removeClass('error');
    $('#bar-notification .content').remove();

    //we do not encode displayed message

    //add new notifications
    var htmlcode = '';
    if ((typeof message) == 'string') {
        htmlcode = '<p class="content">' + message + '</p>';
    } else {
        for (var i = 0; i < message.length; i++) {
            htmlcode = htmlcode + '<p class="content">' + message[i] + '</p>';
        }
    }
    $('#bar-notification').append(htmlcode)
        .addClass(cssclass)
        .fadeIn('slow')
        .mouseenter(function ()
            {
                clearTimeout(barNotificationTimeout);
            });

    $('#bar-notification .close').unbind('click').click(function () {
        $('#bar-notification').fadeOut('slow');
    });

    //timeout (if set)
    if (timeout > 0) {
        barNotificationTimeout = setTimeout(function () {
            $('#bar-notification').fadeOut('slow');
        }, timeout);
    }
}

function htmlEncode(value) {
    return $('<div/>').text(value).html();
}

function htmlDecode(value) {
    return $('<div/>').html(value).text();
}


// CSRF (XSRF) security
function addAntiForgeryToken(data) {
    //if the object is undefined, create a new one.
    if (!data) {
        data = {};
    }
    //add token
    var tokenInput = $('input[name=__RequestVerificationToken]');
    if (tokenInput.length) {
        data.__RequestVerificationToken = tokenInput.val();
    }
    return data;
};

function getQueryString(p) {
    var r = new RegExp('[?&]'+p+'=([^&#]*)', 'i');
    var s = r.exec(window.location.href);
    return s ? s[1] : null;
};


$(function () {

    //TODO: Replace with simple anchor link

    $('#read-more').click(function() {
        $('html, body').animate({ scrollTop: $("#description2").offset().top-50 }, 0);
        return false;
    });


    //Lazy load iframes
    if ('loading' in HTMLIFrameElement.prototype) {
        var iframes = document.querySelectorAll('iframe[loading="lazy"]');
        iframes.forEach(iframe => {iframe.src = iframe.dataset.src;
            //console.log(iframe.dataset.src);
            console.log('test');
        });
    } else {
        // Dynamically import the LazySizes library
        var script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/lazysizes/5.2.2/lazysizes.min.js';
        document.body.appendChild(script);
    }
});

function performSearch(e) {
    var keypressed = e.keyCode || e.which;
    if (e.type === 'keyup' && keypressed !== 13) {
        return false;
    }
    var searchterms = $("#small-searchterms");
    if (searchterms.val() === "") {
        alert('Please enter a search term.');
        searchterms.focus();
        return false;
    }
    window.location.href = '/search?q=' + encodeURIComponent(searchterms.val());
    return false;
}

function ytpopup(id) {
    $.magnificPopup.open({
        src: '<div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; max-width: 100%;"><iframe width="300" height="150" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;" src="https://www.youtube.com/embed/'+id+'?rel=0" frameborder="0" allowfullscreen="allowfullscreen"></iframe></div>',
        type: 'inline'
    });
    return false;
}

var SajariSearch = function (u) { "use strict"; var e, o = function (r) { var s = {}; return Object.keys(r.values).forEach(function (e) { var t, n = void 0 !== (t = r.values[e]).single ? t.single : t.repeated instanceof Object ? t.repeated.values : null; null !== n && (s[e] = n) }), { values: s, token: {}, score: parseFloat(r.score), indexScore: parseFloat(r.indexScore) } }, p = function (e, s) { void 0 === e && (e = {}), void 0 === s && (s = []); var n, t = (e.results || []).map(function (e, t) { var n = o(e), r = s[t]; return void 0 === r || (void 0 !== r.click ? n.token = { click: "https://www.sajari.com/token/" + r.click.token } : void 0 !== r.posNeg && (n.token = { pos: r.posNeg.pos, neg: r.posNeg.neg })), n }); return { reads: parseInt(e.reads, 10) || 0, totalResults: parseInt(e.totalResults, 10) || 0, time: parseFloat(e.time) || 0, aggregates: (n = e.aggregates, void 0 === n && (n = {}), Object.keys(n).reduce(function (e, t) { switch (t.split(".")[0]) { case "bucket": e[t] = n[t].buckets.buckets; break; case "count": e[t] = n[t].count.counts; break; case "date": e[t] = n[t].date.dates; break; case "metric": e[t] = n[t].metric.value } return e }, {})), results: t } }; (e = u.TransportError || (u.TransportError = {}))[e.None = 0] = "None", e[e.Connection = 1] = "Connection", e[e.ParseResponse = 2] = "ParseResponse"; var t, n = function () { function c(e, t) { this.client = e, this.name = t } return c.prototype.search = function (e, t, n) { var r = t.next(e), s = r[0], o = r[1]; if (o) { var i = new Error("could not get next tracking data: " + o); return i.name = "SessionError", void n(i) } var a = JSON.stringify({ metadata: { collection: [this.client.collection], project: [this.client.project], "user-agent": ["sdk-js-1.0.0"] }, request: { tracking: s, values: e, pipeline: { name: this.name } } }); !function (e, t, n) { if (!navigator.onLine) { var r = new Error("connection appears to be offline"); return r.transportErrorCode = u.TransportError.Connection, n(r) } var s = new XMLHttpRequest; s.open("POST", e, !0), s.setRequestHeader("Accept", "application/json"), s.setRequestHeader("Content-Type", "application/json"), s.onreadystatechange = function () { if (s.readyState === XMLHttpRequest.DONE) { var e; if (0 === s.status) return (t = new Error("connection error")).transportErrorCode = u.TransportError.Connection, void n(t); try { e = JSON.parse(s.responseText) } catch (e) { var t; return (t = new Error("error parsing response")).httpStatusCode = s.status, t.transportErrorCode = u.TransportError.ParseResponse, void n(t) } if (200 !== s.status) return (t = new Error(e.message)).httpStatusCode = s.status, void n(t); n(null, e) } }, s.send(t) }(this.client.endpoint + "/" + c.searchEndpoint, a, function (e, t) { e ? n(e) : n(null, p(t.searchResponse, t.tokens), t.values) }) }, c.searchEndpoint = "sajari.api.pipeline.v1.Query/Search", c }(), r = function () { function e(e, t, n) { void 0 === n && (n = []); var r = this; this.project = e, this.collection = t, this.endpoint = "https://jsonapi.sajari.net", n.forEach(function (e) { e(r) }) } return e.prototype.pipeline = function (e) { return new n(this, e) }, e }(); (t = u.TrackingType || (u.TrackingType = {})).None = "NONE", t.Click = "CLICK", t.PosNeg = "POS_NEG"; var s = function () { function e(e, t) { this.lastQuery = "", this.queryLabel = e, this.session = t } return e.prototype.next = function (e) { var t = e[this.queryLabel]; if (void 0 === t) return this.reset(), this.session.next(e); var n = this.lastQuery.substr(0, Math.min(t.length, 3)), r = !(t.substr(0, n.length) === n), s = 0 < this.lastQuery.length && 0 === t.length; return (r || s) && this.reset(), this.lastQuery = t, this.session.next(e) }, e.prototype.reset = function () { this.session.reset() }, e }(), i = function () { function e(e, t, n) { this.queryID = "", this.sequence = 0, this.trackingType = e, this.field = t, this.sessionData = n } return e.prototype.next = function (e) { return "" === this.queryID ? (this.queryID = a(), this.sequence = 0) : this.sequence++, [{ type: this.trackingType, query_id: this.queryID, sequence: this.sequence, field: this.field, data: this.sessionData }, null] }, e.prototype.reset = function () { this.queryID = "", this.sequence = 0 }, e }(), a = function () { for (var e = "", t = 0; t < 16; t++) e += "abcdefghijklmnopqrstuvwxyz0123456789".charAt(Math.floor(36 * Math.random())); return e }; return u.Client = r, u.withEndpoint = function (t) { return function (e) { e.endpoint = t } }, u.InteractiveSession = s, u.DefaultSession = i, u }({});