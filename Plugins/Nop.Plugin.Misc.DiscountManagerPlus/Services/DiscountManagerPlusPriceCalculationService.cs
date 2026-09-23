using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class DiscountManagerPlusPriceCalculationService : PriceCalculationService
    {
        private const string RequestCacheKey = "NopStation.DiscountManagerPlus.RequestDiscountMapTask";
        private const string ReentryGuardKey = "NopStation.DiscountManagerPlus.PriceEventGuard";

        private readonly IWorkContext _workContext;
        private readonly IDiscountManagerPlusService _discountManagerPlusService;
        private readonly ISettingService _settingService;
        private readonly ICustomerService _customerService;

        public DiscountManagerPlusPriceCalculationService(
            IWorkContext workContext,
            IStoreContext storeContext,
            IDiscountService discountService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductAttributeParser productAttributeParser,
            IProductService productService,
            ICacheManager cacheManager,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            IDiscountManagerPlusService discountManagerPlusService,
            ISettingService settingService,
            ICustomerService customerService)
            : base(
                workContext,
                storeContext,
                discountService,
                categoryService,
                manufacturerService,
                productAttributeParser,
                productService,
                cacheManager,
                shoppingCartSettings,
                catalogSettings)
        {
            _workContext = workContext;
            _discountManagerPlusService = discountManagerPlusService;
            _settingService = settingService;
            _customerService = customerService;
        }

        public override decimal GetUnitPrice(
            ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException("shoppingCartItem");

            GetCurrentCouponCode();

            if (!includeDiscounts || IsReentry())
            {
                return base.GetUnitPrice(shoppingCartItem, includeDiscounts, out discountAmount, out appliedDiscount);
            }

            EnterReentry();
            try
            {
                var unitPrice = base.GetUnitPrice(shoppingCartItem, includeDiscounts, out discountAmount, out appliedDiscount);
                return ApplyDiscountManagerPlus(shoppingCartItem, includeDiscounts, true, ref unitPrice, ref discountAmount, ref appliedDiscount);
            }
            finally
            {
                ExitReentry();
            }
        }

        public override decimal GetSubTotal(
            ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException("shoppingCartItem");

            GetCurrentCouponCode();

            if (!includeDiscounts || IsReentry())
            {
                return base.GetSubTotal(shoppingCartItem, includeDiscounts, out discountAmount, out appliedDiscount);
            }

            EnterReentry();
            try
            {
                var subTotal = base.GetSubTotal(shoppingCartItem, includeDiscounts, out discountAmount, out appliedDiscount);
                return ApplyDiscountManagerPlus(shoppingCartItem, includeDiscounts, false, ref subTotal, ref discountAmount, ref appliedDiscount);
            }
            finally
            {
                ExitReentry();
            }
        }

        private decimal ApplyDiscountManagerPlus(
            ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            bool perUnit,
            ref decimal price,
            ref decimal discountAmount,
            ref Discount appliedDiscount)
        {
            if (!includeDiscounts || shoppingCartItem == null || price <= 0)
                return price;

            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(shoppingCartItem.StoreId);
            if (!settings.IsEnabled)
                return price;

            var discountMap = GetOrCreateDiscountMap(shoppingCartItem);
            decimal lineDiscountAmount;
            if (!discountMap.TryGetValue(shoppingCartItem.Id, out lineDiscountAmount) || lineDiscountAmount <= 0)
                return price;

            var appliedAmount = perUnit
                ? (shoppingCartItem.Quantity > 0 ? lineDiscountAmount / shoppingCartItem.Quantity : 0)
                : lineDiscountAmount;

            if (appliedAmount <= 0)
                return price;

            if (appliedAmount > price)
                appliedAmount = price;

            if (appliedDiscount == null)
                appliedDiscount = CreateDiscountManagerPlusMarkerDiscount(appliedAmount);

            discountAmount += appliedAmount;
            price = price - appliedAmount;
            if (price < 0)
                price = 0;

            return price;
        }

        private static Discount CreateDiscountManagerPlusMarkerDiscount(decimal amount)
        {
            return new Discount
            {
                Name = "DiscountManagerPlus",
                DiscountType = DiscountType.AssignedToSkus,
                DiscountAmount = amount
            };
        }

        private Dictionary<int, decimal> GetOrCreateDiscountMap(ShoppingCartItem shoppingCartItem)
        {
            var map = new Dictionary<int, decimal>();
            if (shoppingCartItem == null || shoppingCartItem.CustomerId <= 0)
                return map;

            var customer = shoppingCartItem.Customer ?? _customerService.GetCustomerById(shoppingCartItem.CustomerId);
            if (customer == null || customer.Deleted)
                return map;

            _discountManagerPlusService.SynchronizeAutoAddedRewards(customer, shoppingCartItem.StoreId);

            var cart = GetCustomerCart(customer, shoppingCartItem.StoreId);
            if (!cart.Any())
                return map;

            var requestCacheKey = BuildRequestCacheKey(shoppingCartItem.StoreId, shoppingCartItem.CustomerId, cart);
            var items = GetHttpItems();
            if (items != null && items.Contains(requestCacheKey))
            {
                var existingMap = items[requestCacheKey] as Dictionary<int, decimal>;
                if (existingMap != null)
                    return existingMap;
            }

            map = CreateDiscountMap(cart, shoppingCartItem.StoreId);
            if (items != null)
                items[requestCacheKey] = map;

            return map;
        }

        private Dictionary<int, decimal> CreateDiscountMap(IList<ShoppingCartItem> cart, int storeId)
        {
            var map = new Dictionary<int, decimal>();

            var coordinatedAllocations = _discountManagerPlusService.GetCoordinatedAllocations(cart, storeId);
            if (coordinatedAllocations != null && coordinatedAllocations.Any())
            {
                foreach (var allocation in coordinatedAllocations)
                {
                    if (allocation == null || allocation.DiscountAmount <= 0)
                        continue;

                    decimal current;
                    if (!map.TryGetValue(allocation.LineId, out current))
                        current = 0;
                    map[allocation.LineId] = current + allocation.DiscountAmount;
                }

                if (map.Any())
                    return map;
            }

            var lineDiscountMap = _discountManagerPlusService.BuildLineDiscountMap(cart, storeId);
            foreach (var lineDiscount in lineDiscountMap)
            {
                if (lineDiscount.Value > 0)
                    map[lineDiscount.Key] = lineDiscount.Value;
            }

            return map;
        }

        private static string BuildRequestCacheKey(int storeId, int customerId, IList<ShoppingCartItem> cart)
        {
            var cartKey = string.Join(",", cart.OrderBy(x => x.Id).Select(x => string.Format("{0}-{1}-{2}", x.Id, x.ProductId, x.Quantity)));
            return string.Format("{0}.{1}.{2}.{3}", RequestCacheKey, storeId, customerId, cartKey);
        }

        private static IList<ShoppingCartItem> GetCustomerCart(Customer customer, int storeId)
        {
            if (customer == null || customer.ShoppingCartItems == null)
                return new List<ShoppingCartItem>();

            return customer.ShoppingCartItems
                .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart && x.StoreId == storeId)
                .ToList();
        }

        private string GetCurrentCouponCode()
        {
            var customer = _workContext.CurrentCustomer;
            if (customer == null)
                return null;

            return customer.GetAttribute<string>(SystemCustomerAttributeNames.DiscountCouponCode);
        }

        private static System.Collections.IDictionary GetHttpItems()
        {
            var context = HttpContext.Current;
            return context != null ? context.Items : null;
        }

        private static bool IsReentry()
        {
            var items = GetHttpItems();
            return items != null && items.Contains(ReentryGuardKey);
        }

        private static void EnterReentry()
        {
            var items = GetHttpItems();
            if (items != null)
                items[ReentryGuardKey] = true;
        }

        private static void ExitReentry()
        {
            var items = GetHttpItems();
            if (items != null)
                items.Remove(ReentryGuardKey);
        }
    }
}
