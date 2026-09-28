using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Models.Catalog;
using NopStation.Plugin.Misc.Core.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Components;

public class PromotionBadgeViewComponent : NopStationViewComponent
{
    private readonly ISettingService _settingService;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly ILocalizationService _localizationService;

    public PromotionBadgeViewComponent(
        ISettingService settingService,
        IWorkContext workContext,
        IStoreContext storeContext,
        IShoppingCartService shoppingCartService,
        IDiscountManagerPlusService discountManagerPlusService,
        ILocalizationService localizationService)
    {
        _settingService = settingService;
        _workContext = workContext;
        _storeContext = storeContext;
        _shoppingCartService = shoppingCartService;
        _discountManagerPlusService = discountManagerPlusService;
        _localizationService = localizationService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled || !settings.EnablePromotionBadge)
            return Content(string.Empty);

        if (additionalData is not ProductOverviewModel && additionalData is not ProductDetailsModel)
            return Content(string.Empty);

        var productId = additionalData is ProductOverviewModel productOverviewModel
            ? productOverviewModel.Id
            : ((ProductDetailsModel)additionalData).Id;

        if (productId <= 0)
            return Content(string.Empty);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var virtualCart = cart.ToList();

        if (!virtualCart.Any(x => x.ProductId == productId))
        {
            virtualCart.Add(new ShoppingCartItem
            {
                CustomerId = customer.Id,
                StoreId = store.Id,
                ShoppingCartTypeId = (int)ShoppingCartType.ShoppingCart,
                ProductId = productId,
                Quantity = 1,
                AttributesXml = string.Empty,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            });
        }

        var appliedPromotions = await _discountManagerPlusService.EvaluateCartAsync(virtualCart, store.Id);
        if (!appliedPromotions.Any(x => x.DiscountAmount > 0))
            return Content(string.Empty);

        var model = new PromotionBadgeModel
        {
            Text = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.PromotionBadge"),
            CssClass = "ns-discount-manager-plus-badge"
        };

        //return View(model);
        return Content(string.Empty);
    }
}
