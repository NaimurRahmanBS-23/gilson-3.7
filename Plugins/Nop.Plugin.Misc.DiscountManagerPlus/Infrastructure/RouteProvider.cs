using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            if (!ViewEngines.Engines.OfType<DiscountManagerPlusViewEngine>().Any())
                ViewEngines.Engines.Insert(0, new DiscountManagerPlusViewEngine());

            var adminNamespace = new[] { "Nop.Plugin.Misc.DiscountManagerPlus.Admin.Controllers" };
            var publicNamespace = new[] { "Nop.Plugin.Misc.DiscountManagerPlus.Controllers" };

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.Configure",
                "Plugins/DiscountManagerPlus/Configure",
                new { controller = "DiscountManagerPlus", action = "Configure" },
                adminNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.DiscountDetails",
                "Plugins/DiscountManagerPlus/DiscountDetails",
                new { controller = "DiscountManagerPlus", action = "DiscountDetails" },
                adminNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.Admin.DiscountManagerPlus",
                "Plugins/DiscountManagerPlus/{action}/{id}",
                new { controller = "DiscountManagerPlus", id = UrlParameter.Optional },
                adminNamespace);

            routes.MapRoute("Plugin.DiscountRules.DiscountManagerPlus.Configure",
                "Plugins/DiscountManagerPlusRequirement/Configure",
                new { controller = "DiscountManagerPlusRequirement", action = "Configure" },
                adminNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.Admin.Requirement",
                "Plugins/DiscountManagerPlusRequirement/{action}/{id}",
                new { controller = "DiscountManagerPlusRequirement", id = UrlParameter.Optional },
                adminNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.Admin.PromotionRule",
                "Plugins/PromotionRule/{action}/{id}",
                new { controller = "PromotionRule", id = UrlParameter.Optional },
                adminNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.Admin.PromotionAnalytics",
                "Plugins/PromotionAnalytics/{action}/{id}",
                new { controller = "PromotionAnalytics", id = UrlParameter.Optional },
                adminNamespace);

            routes.MapRoute(DiscountManagerPlusDefaults.OffersRouteName,
                "offers",
                new { controller = "PromotionOffer", action = "List" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.PromotionOffers",
                "promotion-offers",
                new { controller = "PromotionOffer", action = "List" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.PromotionsOffers",
                "promotions/offers",
                new { controller = "PromotionOffer", action = "List" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.AddRewardToCart",
                "Plugins/DiscountManagerPlusPublic/AddRewardToCart",
                new { controller = "DiscountManagerPlusPublic", action = "AddRewardToCart" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.CartSavings",
                "Plugins/DiscountManagerPlusPublic/CartSavings",
                new { controller = "DiscountManagerPlusPublic", action = "CartSavings" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.PromotionBadge",
                "Plugins/DiscountManagerPlusPublic/PromotionBadge",
                new { controller = "DiscountManagerPlusPublic", action = "PromotionBadge" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.OffersLink",
                "Plugins/DiscountManagerPlusPublic/OffersLink",
                new { controller = "DiscountManagerPlusPublic", action = "OffersLink" },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.Public",
                "Plugins/DiscountManagerPlusPublic/{action}/{id}",
                new { controller = "DiscountManagerPlusPublic", id = UrlParameter.Optional },
                publicNamespace);

            routes.MapRoute("Plugin.Misc.DiscountManagerPlus.PromotionOffer",
                "Plugins/PromotionOffer/{action}/{id}",
                new { controller = "PromotionOffer", id = UrlParameter.Optional },
                publicNamespace);

            if (!GlobalFilters.Filters.Any(x => x.Instance is CheckoutPendingRewardSelectionFilter))
                GlobalFilters.Filters.Add(new CheckoutPendingRewardSelectionFilter());

            if (!GlobalFilters.Filters.Any(x => x.Instance is DiscountManagerPlusCartDisplayEventConsumer))
                GlobalFilters.Filters.Add(new DiscountManagerPlusCartDisplayEventConsumer());
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
