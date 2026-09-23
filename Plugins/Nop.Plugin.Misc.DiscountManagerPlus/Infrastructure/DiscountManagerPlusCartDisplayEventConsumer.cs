using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Tax;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class DiscountManagerPlusCartDisplayEventConsumer : ActionFilterAttribute
    {
        private const string RequestCacheKey = "NopStation.DiscountManagerPlus.CartDisplayDiscountMap";
        private const string PendingRewardWarningRequestCacheKey = "NopStation.DiscountManagerPlus.PendingRewardWarning";

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext == null || filterContext.Result == null)
                return;

            var viewResult = filterContext.Result as ViewResultBase;
            if (viewResult == null || viewResult.Model == null)
                return;

            var model = viewResult.Model;
            var modelName = model.GetType().Name;
            if (string.Equals(modelName, "ShoppingCartModel", StringComparison.OrdinalIgnoreCase))
            {
                ApplyLineDiscounts(model);
                AppendPendingRewardWarning(GetWarnings(model));
                return;
            }

            if (string.Equals(modelName, "CheckoutConfirmModel", StringComparison.OrdinalIgnoreCase))
                AppendPendingRewardWarning(GetWarnings(model));
        }

        private void ApplyLineDiscounts(object shoppingCartModel)
        {
            var itemsProperty = shoppingCartModel.GetType().GetProperty("Items");
            if (itemsProperty == null)
                return;

            var items = itemsProperty.GetValue(shoppingCartModel, null) as IEnumerable;
            if (items == null)
                return;

            var lineDiscountMap = GetOrCreateDiscountMap();
            if (!lineDiscountMap.Any())
                return;

            var productService = EngineContext.Current.Resolve<IProductService>();
            var taxService = EngineContext.Current.Resolve<ITaxService>();
            var currencyService = EngineContext.Current.Resolve<ICurrencyService>();
            var priceFormatter = EngineContext.Current.Resolve<IPriceFormatter>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var currentCurrency = workContext.WorkingCurrency;

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                var itemType = item.GetType();
                var idProperty = itemType.GetProperty("Id");
                var productIdProperty = itemType.GetProperty("ProductId");
                var discountProperty = itemType.GetProperty("Discount");
                if (idProperty == null || productIdProperty == null || discountProperty == null)
                    continue;

                var itemId = Convert.ToInt32(idProperty.GetValue(item, null));
                decimal lineDiscountBase;
                if (!lineDiscountMap.TryGetValue(itemId, out lineDiscountBase) || lineDiscountBase <= 0)
                    continue;

                var productId = Convert.ToInt32(productIdProperty.GetValue(item, null));
                var product = productService.GetProductById(productId);
                if (product == null)
                    continue;

                decimal taxRate;
                var lineDiscountWithTaxBase = taxService.GetProductPrice(product, lineDiscountBase, out taxRate);
                if (lineDiscountWithTaxBase <= 0)
                    continue;

                var lineDiscount = currencyService.ConvertFromPrimaryStoreCurrency(lineDiscountWithTaxBase, currentCurrency);
                if (lineDiscount <= 0)
                    continue;

                discountProperty.SetValue(item, priceFormatter.FormatPrice(lineDiscount), null);
            }
        }

        private Dictionary<int, decimal> GetOrCreateDiscountMap()
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var discountManagerPlusService = EngineContext.Current.Resolve<IDiscountManagerPlusService>();

            var store = storeContext.CurrentStore;
            var settings = settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled)
                return new Dictionary<int, decimal>();

            var customer = workContext.CurrentCustomer;
            if (customer == null || customer.Deleted)
                return new Dictionary<int, decimal>();

            discountManagerPlusService.SynchronizeAutoAddedRewards(customer, store.Id);

            var cart = GetCustomerCart(customer, store.Id);
            if (!cart.Any())
                return new Dictionary<int, decimal>();

            var requestCacheKey = BuildRequestCacheKey(store.Id, customer.Id, cart);
            var context = HttpContext.Current;
            if (context != null && context.Items.Contains(requestCacheKey))
            {
                var existingMap = context.Items[requestCacheKey] as Dictionary<int, decimal>;
                if (existingMap != null)
                    return existingMap;
            }

            var map = discountManagerPlusService.BuildLineDiscountMap(cart, store.Id)
                .Where(x => x.Value > 0)
                .ToDictionary(x => x.Key, x => x.Value);

            if (context != null)
                context.Items[requestCacheKey] = map;

            return map;
        }

        private static string BuildRequestCacheKey(int storeId, int customerId, IList<ShoppingCartItem> cart)
        {
            var cartKey = string.Join(",", cart.OrderBy(x => x.Id).Select(x => string.Format("{0}-{1}-{2}", x.Id, x.ProductId, x.Quantity)));
            return string.Format("{0}.{1}.{2}.{3}", RequestCacheKey, storeId, customerId, cartKey);
        }

        private static IList<ShoppingCartItem> GetCustomerCart(Nop.Core.Domain.Customers.Customer customer, int storeId)
        {
            if (customer == null || customer.ShoppingCartItems == null)
                return new List<ShoppingCartItem>();

            return customer.ShoppingCartItems
                .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart && x.StoreId == storeId)
                .ToList();
        }

        private void AppendPendingRewardWarning(IList<string> warnings)
        {
            if (warnings == null)
                return;

            var warning = GetOrCreatePendingRewardWarning();
            if (string.IsNullOrWhiteSpace(warning))
                return;

            if (!warnings.Any(x => x.Equals(warning, StringComparison.OrdinalIgnoreCase)))
                warnings.Add(warning);
        }

        private string GetOrCreatePendingRewardWarning()
        {
            var context = HttpContext.Current;
            if (context != null && context.Items.Contains(PendingRewardWarningRequestCacheKey))
            {
                var existingWarning = context.Items[PendingRewardWarningRequestCacheKey] as string;
                if (existingWarning != null)
                    return existingWarning;
            }

            var warning = CreatePendingRewardWarning();
            if (context != null)
                context.Items[PendingRewardWarningRequestCacheKey] = warning;

            return warning;
        }

        private string CreatePendingRewardWarning()
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var discountManagerPlusService = EngineContext.Current.Resolve<IDiscountManagerPlusService>();
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            var store = storeContext.CurrentStore;
            var settings = settingService.LoadSetting<DiscountManagerPlusSettings>(store.Id);
            if (!settings.IsEnabled)
                return string.Empty;

            var customer = workContext.CurrentCustomer;
            if (customer == null || customer.Deleted)
                return string.Empty;

            discountManagerPlusService.SynchronizeAutoAddedRewards(customer, store.Id);

            var cart = GetCustomerCart(customer, store.Id);
            if (!cart.Any())
                return string.Empty;

            var pendingPromotions = (discountManagerPlusService.EvaluateCart(cart, store.Id) ?? new List<AppliedPromotion>())
                .Where(x => x.RequiresRewardSelection)
                .ToList();
            if (!pendingPromotions.Any())
                return string.Empty;

            var pendingRuleNames = pendingPromotions
                .Select(x => x.RuleName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Take(3)
                .ToList();

            var warningTemplate = localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout");
            var fallbackWarning = localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout.Default");
            if (string.IsNullOrWhiteSpace(warningTemplate) || !pendingRuleNames.Any())
                return fallbackWarning;

            return string.Format(warningTemplate, string.Join(", ", pendingRuleNames));
        }

        private static IList<string> GetWarnings(object model)
        {
            if (model == null)
                return null;

            var warningsProperty = model.GetType().GetProperty("Warnings", BindingFlags.Public | BindingFlags.Instance);
            if (warningsProperty == null)
                return null;

            return warningsProperty.GetValue(model, null) as IList<string>;
        }
    }
}
