using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class PromotionEvaluationContextFactory : IPromotionEvaluationContextFactory
    {
        private readonly ICountryService _countryService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGeoLookupService _geoLookupService;
        private readonly IWebHelper _webHelper;

        public PromotionEvaluationContextFactory(
            ICountryService countryService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IGeoLookupService geoLookupService,
            IWebHelper webHelper)
        {
            _countryService = countryService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _geoLookupService = geoLookupService;
            _webHelper = webHelper;
        }

        public PromotionEvaluationContext BuildDefaultEvaluationContext(IList<ShoppingCartItem> cart, int storeId = 0)
        {
            var firstItem = cart != null ? cart.FirstOrDefault() : null;
            var result = new PromotionEvaluationContext
            {
                StoreId = storeId > 0 ? storeId : (firstItem != null ? firstItem.StoreId : 0),
                Cart = cart ?? new List<ShoppingCartItem>(),
                CurrentUtc = DateTime.UtcNow
            };

            var customerId = firstItem != null ? firstItem.CustomerId : 0;
            result.CustomerId = customerId;

            if (customerId <= 0)
                return result;

            var customer = _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted)
                return result;

            var attributeStoreId = result.StoreId;
            var couponCode = customer.GetAttribute<string>(SystemCustomerAttributeNames.DiscountCouponCode, _genericAttributeService);
            var normalized = NormalizeCouponCode(couponCode);
            if (!string.IsNullOrWhiteSpace(normalized) &&
                !result.CouponCodes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                result.CouponCodes.Add(normalized);
            }

            result.SelectedPaymentMethodSystemName = customer.GetAttribute<string>(
                SystemCustomerAttributeNames.SelectedPaymentMethod,
                _genericAttributeService,
                attributeStoreId);

            result.CountryIso2 = ResolveCountryIso2(customer);

            if (string.IsNullOrWhiteSpace(result.SocialShareProofToken))
            {
                var request = HttpContext.Current != null ? HttpContext.Current.Request : null;
                string token = null;
                if (request != null)
                {
                    token = request.QueryString["promotion_social_token"];
                    if (string.IsNullOrWhiteSpace(token))
                        token = request.Headers["X-Promotion-Social-Token"];
                }

                result.SocialShareProofToken = token;
            }

            return result;
        }

        public int ResolveEvaluationStoreId(IList<ShoppingCartItem> cart, PromotionEvaluationContext context)
        {
            if (context != null && context.StoreId > 0)
                return context.StoreId;

            var firstItem = cart != null ? cart.FirstOrDefault() : null;
            return firstItem != null ? firstItem.StoreId : 0;
        }

        private string ResolveCountryIso2(Customer customer)
        {
            var requestCacheKey = string.Format("Nop.DiscountManagerPlus.CountryIso2.{0}", customer != null ? customer.Id : 0);
            var httpContext = HttpContext.Current;
            if (httpContext != null && httpContext.Items.Contains(requestCacheKey))
            {
                var cachedIso2 = httpContext.Items[requestCacheKey] as string;
                if (cachedIso2 != null)
                    return cachedIso2;
            }

            var countryIso2 = ResolveCountryIso2FromIp();
            if (string.IsNullOrWhiteSpace(countryIso2) && customer != null && customer.ShippingAddress != null)
            {
                var shippingAddress = customer.ShippingAddress;
                if (shippingAddress.CountryId.HasValue && shippingAddress.CountryId.Value > 0)
                {
                    var country = _countryService.GetCountryById(shippingAddress.CountryId.Value);
                    if (country != null && country.Published)
                        countryIso2 = country.TwoLetterIsoCode;
                }
            }

            if (string.IsNullOrWhiteSpace(countryIso2) && customer != null && customer.BillingAddress != null)
            {
                var billingAddress = customer.BillingAddress;
                if (billingAddress.CountryId.HasValue && billingAddress.CountryId.Value > 0)
                {
                    var country = _countryService.GetCountryById(billingAddress.CountryId.Value);
                    if (country != null && country.Published)
                        countryIso2 = country.TwoLetterIsoCode;
                }
            }

            if (httpContext != null)
                httpContext.Items[requestCacheKey] = countryIso2 ?? string.Empty;

            return countryIso2 ?? string.Empty;
        }

        private string ResolveCountryIso2FromIp()
        {
            var ipAddress = _webHelper.GetCurrentIpAddress();
            if (string.IsNullOrWhiteSpace(ipAddress))
                return string.Empty;

            var countryIso2 = _geoLookupService.LookupCountryIsoCode(ipAddress);
            if (!string.IsNullOrWhiteSpace(countryIso2))
                countryIso2 = countryIso2.Trim();
            if (string.IsNullOrWhiteSpace(countryIso2))
                return string.Empty;

            var country = _countryService.GetCountryByTwoLetterIsoCode(countryIso2);
            if (country == null || !country.Published)
                return string.Empty;

            return country.TwoLetterIsoCode;
        }

        private static string NormalizeCouponCode(string couponCode)
        {
            return string.IsNullOrWhiteSpace(couponCode) ? string.Empty : couponCode.Trim().ToUpperInvariant();
        }
    }
}
