using Microsoft.AspNetCore.Mvc;
using Nop.Web.Areas.Admin.Models.Discounts;
using NopStation.Plugin.Misc.Core.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Components;

public class DiscountManagerPlusDiscountDetailsViewComponent : NopStationViewComponent
{
    private readonly DiscountManagerPlusSettings _settings;
    private readonly IPromotionRuleModelFactory _promotionRuleModelFactory;

    public DiscountManagerPlusDiscountDetailsViewComponent(
        DiscountManagerPlusSettings settings,
        IPromotionRuleModelFactory promotionRuleModelFactory)
    {
        _settings = settings;
        _promotionRuleModelFactory = promotionRuleModelFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (!_settings.IsEnabled)
            return Content(string.Empty);

        if (additionalData is not DiscountModel discountModel)
            return Content(string.Empty);

        var model = await _promotionRuleModelFactory.PrepareDiscountDetailsPromotionRulesModelAsync(discountModel.Id);
        if (model.IsSavedDiscount)
            model.DiscountName = string.IsNullOrWhiteSpace(discountModel.Name) ? model.DiscountName : discountModel.Name;

        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/Shared/Components/DiscountManagerPlusDiscountDetails/Default.cshtml", model);
    }
}
