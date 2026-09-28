using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using NopStation.Plugin.Misc.Core.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Components;

public class OffersLinkViewComponent : NopStationViewComponent
{
    private readonly ILocalizationService _localizationService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;

    public OffersLinkViewComponent(
        ILocalizationService localizationService,
        IPromotionRuleService promotionRuleService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _localizationService = localizationService;
        _promotionRuleService = promotionRuleService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled)
            return Content(string.Empty);

        var activeRules = await _promotionRuleService.GetActiveRulesAsync(store.Id);
        if (!activeRules.Any())
            return Content(string.Empty);

        var model = new OfferPageLinkModel
        {
            Text = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.LinkText"),
            Url = Url.RouteUrl(DiscountManagerPlusDefaults.OffersRouteName) ?? "/promotions/offers"
        };

        return View(model);
    }
}
