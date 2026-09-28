using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Infrastructure;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class RouteProvider : BaseRouteProvider, IRouteProvider
{
    #region Methods

    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var pattern = GetLanguageRoutePattern();

        endpointRouteBuilder.MapControllerRoute(
            DiscountManagerPlusDefaults.OffersRouteName,
            $"{pattern}/promotions/offers",
            new { controller = "PromotionOffer", action = "List" });
    }

    #endregion

    #region Properties

    public int Priority => 0;

    #endregion
}
