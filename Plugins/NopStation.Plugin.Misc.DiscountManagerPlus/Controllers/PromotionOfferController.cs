using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Controllers;
using NopStation.Plugin.Misc.DiscountManagerPlus.Factories;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Controllers;

public class PromotionOfferController : BasePluginController
{
    private readonly IPromotionOfferModelFactory _promotionOfferModelFactory;

    public PromotionOfferController(IPromotionOfferModelFactory promotionOfferModelFactory)
    {
        _promotionOfferModelFactory = promotionOfferModelFactory;
    }

    public async Task<IActionResult> List()
    {
        var model = await _promotionOfferModelFactory.PreparePromotionOfferListModelAsync();
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Views/PromotionOffer/List.cshtml", model);
    }
}
