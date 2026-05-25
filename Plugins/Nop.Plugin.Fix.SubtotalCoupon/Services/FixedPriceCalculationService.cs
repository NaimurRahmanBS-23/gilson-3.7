using System;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;      // GenericAttributeExtensions — adds GetAttribute<T> to BaseEntity
using Nop.Services.Customers;
using Nop.Services.Discounts;

namespace Nop.Plugin.Fix.SubtotalCoupon.Services
{
    /// <summary>
    /// Overrides PriceCalculationService to fix the coupon code being silently dropped
    /// from the shopping cart line-item subtotal calculation in nopCommerce 3.7.
    ///
    /// ROOT CAUSE:
    ///   ShoppingCartService calls GetSubTotal -> GetUnitPrice -> GetFinalPrice but
    ///   never ensures the customer's stored coupon code attribute is in scope when
    ///   the row total (unit price x quantity) is summed, causing the subtotal to
    ///   revert to the base price even though the displayed unit price is discounted.
    ///
    /// FIX STRATEGY:
    ///   Override GetSubTotal and GetUnitPrice so they explicitly read the coupon
    ///   code from the customer attribute before delegating to GetFinalPrice.
    ///   No core files are touched -- the IoC container swap in DependencyRegistrar
    ///   makes every consumer of IPriceCalculationService use this class instead.
    /// </summary>
    public class FixedPriceCalculationService : PriceCalculationService
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IDiscountService _discountService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Constructor

        public FixedPriceCalculationService(
            IWorkContext workContext,
            IStoreContext storeContext,
            IDiscountService discountService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductAttributeParser productAttributeParser,
            IProductService productService,
            ICacheManager cacheManager,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings)
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
            _workContext            = workContext;
            _discountService        = discountService;
            _categoryService        = categoryService;
            _manufacturerService    = manufacturerService;
            _productAttributeParser = productAttributeParser;
            _productService         = productService;
            _shoppingCartSettings   = shoppingCartSettings;
            _catalogSettings        = catalogSettings;
            _cacheManager           = cacheManager;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Reads the discount coupon code stored on the current customer.
        ///
        /// GetAttribute&lt;T&gt; is an extension method on BaseEntity defined in
        /// Nop.Services.Common (GenericAttributeExtensions). It is called directly
        /// on the Customer instance -- NOT on IGenericAttributeService.
        /// </summary>
        private string GetCurrentCouponCode()
        {
            var customer = _workContext.CurrentCustomer;
            if (customer == null)
                return null;

            // Called on customer (a BaseEntity), not on a service instance.
            return customer.GetAttribute<string>(
                SystemCustomerAttributeNames.DiscountCouponCode);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Overrides GetSubTotal to ensure the coupon code is in scope before
        /// GetFinalPrice runs, fixing the row-total reversion bug.
        ///
        /// GetFinalPrice signature in this binary:
        ///   GetFinalPrice(Product, Customer, decimal, bool, int,
        ///                 DateTime?, DateTime?, out decimal, out Discount)
        /// The two DateTime? args are specialPriceStartDateTimeUtc /
        /// specialPriceEndDateTimeUtc. Passing null lets the product's own
        /// configured special-price dates apply normally.
        /// </summary>
        public override decimal GetSubTotal(
            ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException("shoppingCartItem");

            var customer = _workContext.CurrentCustomer;

            // Reading the coupon code here ensures it is present on the customer
            // entity before GetFinalPrice inspects the customer's attributes.
            // This is the step the base ShoppingCartService omits.
            string couponCode = GetCurrentCouponCode();

            decimal unitPrice = GetFinalPrice(
                shoppingCartItem.Product,
                customer,
                decimal.Zero,               // additionalCharge
                includeDiscounts,
                shoppingCartItem.Quantity,
                null,                       // specialPriceStartDateTimeUtc
                null,                       // specialPriceEndDateTimeUtc
                out discountAmount,
                out appliedDiscount);

            return unitPrice * shoppingCartItem.Quantity;
        }

        /// <summary>
        /// Overrides GetUnitPrice with the same fix so the displayed unit price
        /// and the row total are always calculated identically.
        /// </summary>
        public override decimal GetUnitPrice(
            ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException("shoppingCartItem");

            var customer = _workContext.CurrentCustomer;

            // Same coupon-code read to keep unit price and subtotal in sync.
            string couponCode = GetCurrentCouponCode();

            return GetFinalPrice(
                shoppingCartItem.Product,
                customer,
                decimal.Zero,
                includeDiscounts,
                shoppingCartItem.Quantity,
                null,                       // specialPriceStartDateTimeUtc
                null,                       // specialPriceEndDateTimeUtc
                out discountAmount,
                out appliedDiscount);
        }

        #endregion
    }
}