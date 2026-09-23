using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework.UI;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class CheckoutPendingRewardSelectionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsCheckoutConfirmationPost(filterContext))
                return;

            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var discountManagerPlusService = EngineContext.Current.Resolve<IDiscountManagerPlusService>();
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            var store = storeContext.CurrentStore;
            var settings = settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled)
                return;

            var customer = workContext.CurrentCustomer;
            if (customer == null || customer.Deleted)
                return;

            discountManagerPlusService.SynchronizeAutoAddedRewards(customer, store.Id);

            var cart = customer.ShoppingCartItems == null
                ? new List<ShoppingCartItem>()
                : customer.ShoppingCartItems
                    .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart && x.StoreId == store.Id)
                    .ToList();
            if (!cart.Any())
                return;

            var pendingPromotions = (discountManagerPlusService.EvaluateCart(cart, store.Id) ?? new List<AppliedPromotion>())
                .Where(x => x.RequiresRewardSelection)
                .ToList();
            if (!pendingPromotions.Any())
                return;

            var pendingRuleNames = pendingPromotions
                .Select(x => x.RuleName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var warningTemplate = localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout");
            var fallbackWarning = localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout.Default");
            var warning = string.IsNullOrWhiteSpace(warningTemplate) || !pendingRuleNames.Any()
                ? fallbackWarning
                : string.Format(warningTemplate, string.Join(", ", pendingRuleNames.Take(3)));

            if (IsOpcConfirmRequest(filterContext))
            {
                filterContext.Result = new JsonResult
                {
                    Data = new { error = 1, message = warning },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return;
            }

            var dataKey = string.Format("nop.notifications.{0}", NotifyType.Error);
            var notifications = filterContext.Controller.TempData[dataKey] as IList<string>;
            if (notifications == null)
            {
                notifications = new List<string>();
                filterContext.Controller.TempData[dataKey] = notifications;
            }
            notifications.Add(warning);
            filterContext.Result = new RedirectResult("~/cart");
        }

        private static bool IsCheckoutConfirmationPost(ActionExecutingContext context)
        {
            if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                return false;

            if (!string.Equals(context.HttpContext.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                return false;

            var controller = GetRouteValue(context, "controller");
            if (!string.Equals(controller, "Checkout", StringComparison.OrdinalIgnoreCase))
                return false;

            var action = GetRouteValue(context, "action");
            return string.Equals(action, "Confirm", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(action, "OpcConfirmOrder", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOpcConfirmRequest(ActionExecutingContext context)
        {
            var action = GetRouteValue(context, "action");
            if (string.Equals(action, "OpcConfirmOrder", StringComparison.OrdinalIgnoreCase))
                return true;

            var requestedWith = context.HttpContext.Request.Headers["X-Requested-With"];
            return string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRouteValue(ActionExecutingContext context, string key)
        {
            if (context.RouteData == null || context.RouteData.Values == null || !context.RouteData.Values.ContainsKey(key))
                return string.Empty;

            var value = context.RouteData.Values[key];
            return value != null ? value.ToString() : string.Empty;
        }
    }
}
