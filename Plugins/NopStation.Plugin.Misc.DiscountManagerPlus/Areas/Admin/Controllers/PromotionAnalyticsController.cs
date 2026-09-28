using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Filters;
using NopStation.Plugin.Misc.Core.Controllers;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Controllers;

public class PromotionAnalyticsController : NopStationAdminController
{
    private readonly IPromotionAnalyticsModelFactory _promotionAnalyticsModelFactory;

    public PromotionAnalyticsController(IPromotionAnalyticsModelFactory promotionAnalyticsModelFactory)
    {
        _promotionAnalyticsModelFactory = promotionAnalyticsModelFactory;
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> List()
    {
        var searchModel = await _promotionAnalyticsModelFactory.PreparePromotionRuleAnalyticsSearchModelAsync(new PromotionRuleAnalyticsSearchModel());
        return View(searchModel);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> List(PromotionRuleAnalyticsSearchModel searchModel)
    {
        var model = await _promotionAnalyticsModelFactory.PreparePromotionRuleAnalyticsListModelAsync(searchModel);
        return Json(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> Summary(PromotionRuleAnalyticsSearchModel searchModel)
    {
        var model = await _promotionAnalyticsModelFactory.PreparePromotionRuleAnalyticsSummaryModelAsync(searchModel);
        return Json(model);
    }
}
