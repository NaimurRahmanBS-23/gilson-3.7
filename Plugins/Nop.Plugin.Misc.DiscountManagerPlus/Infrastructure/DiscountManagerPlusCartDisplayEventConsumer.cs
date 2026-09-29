using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Tax;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    /// <summary>
    /// Injects Discount Manager Plus savings into cart / order-total models on every
    /// request (full page load and postbacks), not only after AJAX apply flows.
    /// Also keeps the applied coupon message visible even when eligibility rules fail.
    /// </summary>
    public class DiscountManagerPlusCartDisplayEventConsumer : ActionFilterAttribute
    {
        private const string RequestCacheKey = "NopStation.DiscountManagerPlus.CartDisplayDiscountMap";
        private const string PendingRewardWarningRequestCacheKey = "NopStation.DiscountManagerPlus.PendingRewardWarning";
        private const string OrderTotalsProcessedKey = "NopStation.DiscountManagerPlus.OrderTotalsProcessed";
        private const string CouponPersistedKey = "NopStation.DiscountManagerPlus.CouponPersisted";

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            PersistAppliedCouponEvenIfInvalid(filterContext);
            ApplyToResult(filterContext != null ? filterContext.Result : null);
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // Run again immediately before the view renders so child actions
            // (OrderTotals) and full cart page loads both get DMP values.
            ApplyToResult(filterContext != null ? filterContext.Result : null);
        }

        private void ApplyToResult(ActionResult result)
        {
            if (result == null)
                return;

            var viewResult = result as ViewResultBase;
            if (viewResult == null || viewResult.Model == null)
                return;

            var model = viewResult.Model;
            var modelName = model.GetType().Name;

            if (string.Equals(modelName, "ShoppingCartModel", StringComparison.OrdinalIgnoreCase))
            {
                ApplyLineDiscounts(model);
                EnsureDiscountBoxShowsAppliedCoupon(model);
                AppendPendingRewardWarning(GetWarnings(model));
                return;
            }

            if (string.Equals(modelName, "OrderTotalsModel", StringComparison.OrdinalIgnoreCase))
            {
                ApplyOrderTotalsDiscounts(model);
                return;
            }

            if (string.Equals(modelName, "CheckoutConfirmModel", StringComparison.OrdinalIgnoreCase))
                AppendPendingRewardWarning(GetWarnings(model));
        }

        /// <summary>
        /// Core nopCommerce only saves DiscountCouponCode when ValidateDiscount.IsValid.
        /// Customers should keep a code they entered even if quantity/category rules
        /// are not currently met, so the applied UI stays visible and the code is ready
        /// when the cart becomes eligible.
        /// </summary>
        private void PersistAppliedCouponEvenIfInvalid(ActionExecutedContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null || filterContext.HttpContext.Request == null)
                return;

            var httpContext = filterContext.HttpContext;
            if (httpContext.Items.Contains(CouponPersistedKey))
                return;

            var request = httpContext.Request;
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                return;

            if (request.Form == null || request.Form["applydiscountcouponcode"] == null)
                return;

            var couponCode = request.Form["discountcouponcode"];
            if (couponCode != null)
                couponCode = couponCode.Trim();
            if (string.IsNullOrWhiteSpace(couponCode))
                return;

            try
            {
                var settingService = EngineContext.Current.Resolve<ISettingService>();
                var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                var settings = settingService.LoadSetting<DiscountManagerPlusSettings>(storeContext.CurrentStore.Id);
                if (!settings.IsEnabled)
                    return;

                var discountService = EngineContext.Current.Resolve<IDiscountService>();
                var discount = discountService.GetDiscountByCouponCode(couponCode, true);
                if (discount == null || !discount.RequiresCouponCode)
                    return;

                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var customer = workContext.CurrentCustomer;
                if (customer == null || customer.Deleted)
                    return;

                var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
                genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.DiscountCouponCode, discount.CouponCode);
                httpContext.Items[CouponPersistedKey] = true;
            }
            catch
            {
                // Display-only persistence; never break cart apply.
            }
        }

        /// <summary>
        /// Always surface the entered coupon on DiscountBox, even when requirement
        /// validation currently fails (e.g. under the 5+ category quantity threshold).
        /// </summary>
        private void EnsureDiscountBoxShowsAppliedCoupon(object shoppingCartModel)
        {
            if (shoppingCartModel == null)
                return;

            try
            {
                var settingService = EngineContext.Current.Resolve<ISettingService>();
                var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                var settings = settingService.LoadSetting<DiscountManagerPlusSettings>(storeContext.CurrentStore.Id);
                if (!settings.IsEnabled)
                    return;

                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var customer = workContext.CurrentCustomer;
                if (customer == null || customer.Deleted)
                    return;

                var couponCode = customer.GetAttribute<string>(SystemCustomerAttributeNames.DiscountCouponCode);
                if (string.IsNullOrWhiteSpace(couponCode))
                    return;

                var discountService = EngineContext.Current.Resolve<IDiscountService>();
                var discount = discountService.GetDiscountByCouponCode(couponCode, true);
                var displayCode = discount != null && !string.IsNullOrWhiteSpace(discount.CouponCode)
                    ? discount.CouponCode
                    : couponCode;

                var discountBoxProperty = shoppingCartModel.GetType().GetProperty("DiscountBox");
                if (discountBoxProperty == null)
                    return;

                var discountBox = discountBoxProperty.GetValue(shoppingCartModel, null);
                if (discountBox == null)
                    return;

                var discountBoxType = discountBox.GetType();
                var currentCodeProperty = discountBoxType.GetProperty("CurrentCode");
                var isAppliedProperty = discountBoxType.GetProperty("IsApplied");
                var messageProperty = discountBoxType.GetProperty("Message");
                var displayProperty = discountBoxType.GetProperty("Display");

                if (displayProperty != null && displayProperty.CanWrite)
                    displayProperty.SetValue(discountBox, true, null);

                if (currentCodeProperty != null && currentCodeProperty.CanWrite)
                    currentCodeProperty.SetValue(discountBox, displayCode, null);

                if (isAppliedProperty != null && isAppliedProperty.CanWrite)
                    isAppliedProperty.SetValue(discountBox, true, null);

                // Always show the applied success message when a coupon is retained,
                // even if core validation failed on quantity/category rules.
                if (messageProperty != null && messageProperty.CanWrite)
                {
                    var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
                    messageProperty.SetValue(discountBox,
                        localizationService.GetResource("ShoppingCart.DiscountCouponCode.Applied"), null);
                }
            }
            catch
            {
                // Display-only; never break cart rendering.
            }
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

                // Always surface DMP savings on the line, including full page load.
                discountProperty.SetValue(item, priceFormatter.FormatPrice(lineDiscount), null);
            }
        }

        private void ApplyOrderTotalsDiscounts(object orderTotalsModel)
        {
            if (orderTotalsModel == null)
                return;

            // Child action can be rendered once; avoid double-inflating SubTotal.
            var httpContext = HttpContext.Current;
            if (httpContext != null && httpContext.Items.Contains(OrderTotalsProcessedKey))
                return;

            var subTotalDiscountProperty = orderTotalsModel.GetType().GetProperty("SubTotalDiscount");
            var subTotalProperty = orderTotalsModel.GetType().GetProperty("SubTotal");
            if (subTotalDiscountProperty == null || subTotalProperty == null)
                return;

            var existingSubTotalDiscount = Convert.ToString(subTotalDiscountProperty.GetValue(orderTotalsModel, null));
            // Native order/subtotal coupon already has a discount row — leave it alone.
            if (!string.IsNullOrWhiteSpace(existingSubTotalDiscount))
                return;

            var lineDiscountMap = GetOrCreateDiscountMap();
            if (!lineDiscountMap.Any())
                return;

            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var priceFormatter = EngineContext.Current.Resolve<IPriceFormatter>();
            var taxSettings = EngineContext.Current.Resolve<TaxSettings>();
            var currentCurrency = workContext.WorkingCurrency;
            var subTotalIncludingTax = workContext.TaxDisplayType == TaxDisplayType.IncludingTax
                && !taxSettings.ForceTaxExclusionFromOrderSubtotal;

            var dmpSavingsDisplay = ConvertLineDiscountsToDisplayCurrency(lineDiscountMap, subTotalIncludingTax);
            if (dmpSavingsDisplay <= 0)
                return;

            var existingSubTotalText = Convert.ToString(subTotalProperty.GetValue(orderTotalsModel, null));
            if (string.IsNullOrWhiteSpace(existingSubTotalText))
                return;

            // SubTotal already has DMP baked in via GetSubTotal. Inflate it back for
            // display and show a dedicated discount row so reload matches apply UX.
            var displaySubTotal = GetCurrentCartSubTotalDisplay(subTotalIncludingTax) + dmpSavingsDisplay;

            subTotalProperty.SetValue(orderTotalsModel,
                priceFormatter.FormatPrice(displaySubTotal, true, currentCurrency, workContext.WorkingLanguage, subTotalIncludingTax),
                null);
            subTotalDiscountProperty.SetValue(orderTotalsModel,
                priceFormatter.FormatPrice(-dmpSavingsDisplay, true, currentCurrency, workContext.WorkingLanguage, subTotalIncludingTax),
                null);

            if (httpContext != null)
                httpContext.Items[OrderTotalsProcessedKey] = true;
        }

        private decimal GetCurrentCartSubTotalDisplay(bool includingTax)
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var priceCalculationService = EngineContext.Current.Resolve<IPriceCalculationService>();
            var taxService = EngineContext.Current.Resolve<ITaxService>();
            var currencyService = EngineContext.Current.Resolve<ICurrencyService>();

            var cart = GetCustomerCart(workContext.CurrentCustomer, storeContext.CurrentStore.Id);
            decimal subTotalBase = 0;
            foreach (var sci in cart)
            {
                if (sci.Product == null)
                    continue;

                var sciSubTotal = priceCalculationService.GetSubTotal(sci, true);
                decimal taxRate;
                subTotalBase += taxService.GetProductPrice(sci.Product, sciSubTotal, includingTax, workContext.CurrentCustomer, out taxRate);
            }

            return currencyService.ConvertFromPrimaryStoreCurrency(subTotalBase, workContext.WorkingCurrency);
        }

        private decimal ConvertLineDiscountsToDisplayCurrency(IDictionary<int, decimal> lineDiscountMap, bool includingTax)
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var taxService = EngineContext.Current.Resolve<ITaxService>();
            var currencyService = EngineContext.Current.Resolve<ICurrencyService>();
            var productService = EngineContext.Current.Resolve<IProductService>();

            var cart = GetCustomerCart(workContext.CurrentCustomer, storeContext.CurrentStore.Id);
            var cartById = cart.ToDictionary(x => x.Id, x => x);
            decimal totalBase = 0;

            foreach (var pair in lineDiscountMap)
            {
                if (pair.Value <= 0)
                    continue;

                ShoppingCartItem sci;
                if (!cartById.TryGetValue(pair.Key, out sci) || sci == null)
                    continue;

                var product = sci.Product ?? productService.GetProductById(sci.ProductId);
                if (product == null)
                {
                    totalBase += pair.Value;
                    continue;
                }

                decimal taxRate;
                totalBase += taxService.GetProductPrice(product, pair.Value, includingTax, workContext.CurrentCustomer, out taxRate);
            }

            return currencyService.ConvertFromPrimaryStoreCurrency(totalBase, workContext.WorkingCurrency);
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
