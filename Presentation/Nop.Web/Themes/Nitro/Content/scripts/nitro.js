(function () {
    $(document).ready(function () {
        var responsiveAppSettings = {
            isEnabled: true,
            themeBreakpoint: 980,
            isSearchBoxDetachable: true,
            isHeaderLinksWrapperDetachable: true,
            doesDesktopHeaderMenuStick: true,
            doesScrollAfterFiltration: false,
            doesSublistHasIndent: true,
            displayGoToTop: true,
            hasStickyNav: true,
            selectors: {
                menuTitle: ".menu-title",
                headerMenu: ".header-menu",
                closeMenu: ".close-menu",
                movedElements: ".admin-header-links, .header, .responsive-nav-wrapper, .master-wrapper-content, .footer, .slider-wrapper",
                sublist: ".header-menu .sublist",
                overlayOffCanvas: ".overlayOffCanvas",
                withSubcategories: ".with-subcategories",
                filtersContainer: ".nopAjaxFilters7Spikes",
                filtersOpener: ".filters-button span",
                searchBoxOpener: ".search-wrap > span",
                searchBox: ".search-box.store-search-box",
                //searchBoxBefore: ".page-title span, .page-title h1",
                searchBoxBefore: "#header-contact-us",
                navWrapper: ".responsive-nav-wrapper",
                navWrapperParent: ".responsive-nav-wrapper-parent",
                headerLinksOpener: "#header-links-opener",
                headerLinksWrapper: ".header-links-wrapper",
                headerLinksWrapperMobileInsertAfter: ".header",
                headerLinksWrapperDesktopPrependTo: ".header-centering",
                shoppingCartLink: ".shopping-cart-link",
                overlayEffectDelay: 300
            }
        };

        sevenSpikes.initResponsiveTheme(responsiveAppSettings);

        if ($("#gilson-lhm").length > 0) {$.ajax({ url: "/gilsonlhm", dataType: "html", success: function (lhm) { $("#gilson-lhm").html(lhm); } });}
    });
})();