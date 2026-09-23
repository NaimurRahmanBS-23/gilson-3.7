using System.Web.Mvc;
using Nop.Plugin.Misc.DiscountManagerPlus.Factories;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Controllers
{
    public class PromotionOfferController : BasePluginController
    {
        private readonly IPromotionOfferModelFactory _promotionOfferModelFactory;

        public PromotionOfferController(IPromotionOfferModelFactory promotionOfferModelFactory)
        {
            _promotionOfferModelFactory = promotionOfferModelFactory;
        }

        public ActionResult List()
        {
            var model = _promotionOfferModelFactory.PreparePromotionOfferListModel();
            return View("~/Plugins/Misc.DiscountManagerPlus/Views/PromotionOffer/List.cshtml", model);
        }
    }
}
