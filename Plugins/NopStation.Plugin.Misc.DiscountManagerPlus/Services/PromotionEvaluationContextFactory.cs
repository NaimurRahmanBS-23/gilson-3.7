using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class PromotionEvaluationContextFactory : IPromotionEvaluationContextFactory
{
    private readonly ICountryService _countryService;
    private readonly ICustomerService _customerService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IGeoLookupService _geoLookupService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHelper _webHelper;

    public PromotionEvaluationContextFactory(
        ICountryService countryService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        IGeoLookupService geoLookupService,
        IHttpContextAccessor httpContextAccessor,
        IWebHelper webHelper)
    {
        _countryService = countryService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _geoLookupService = geoLookupService;
        _httpContextAccessor = httpContextAccessor;
        _webHelper = webHelper;
    }

    public async Task<PromotionEvaluationContext> BuildDefaultEvaluationContextAsync(IList<ShoppingCartItem> cart, int storeId = 0)
    {
        var result = new PromotionEvaluationContext
        {
            StoreId = storeId > 0 ? storeId : cart?.FirstOrDefault()?.StoreId ?? 0,
            Cart = cart ?? new List<ShoppingCartItem>(),
            CurrentUtc = DateTime.UtcNow
        };

        var customerId = cart?.FirstOrDefault()?.CustomerId ?? 0;
        result.CustomerId = customerId;

        if (customerId <= 0)
            return result;

        var customer = await _customerService.GetCustomerByIdAsync(customerId);
        if (customer == null || customer.Deleted)
            return result;

        var attributeStoreId = result.StoreId;
        var couponCodes = await _customerService.ParseAppliedDiscountCouponCodesAsync(customer);
        foreach (var code in couponCodes ?? Array.Empty<string>())
        {
            var normalized = NormalizeCouponCode(code);
            if (!string.IsNullOrWhiteSpace(normalized) &&
                !result.CouponCodes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                result.CouponCodes.Add(normalized);
            }
        }

        result.SelectedPaymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(
            customer,
            NopCustomerDefaults.SelectedPaymentMethodAttribute,
            attributeStoreId);

        result.CountryIso2 = await ResolveCountryIso2Async(customer);

        if (string.IsNullOrWhiteSpace(result.SocialShareProofToken))
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var token = request?.Query["promotion_social_token"].ToString();
            if (string.IsNullOrWhiteSpace(token))
                token = request?.Headers["X-Promotion-Social-Token"].ToString();

            result.SocialShareProofToken = token;
        }

        return result;
    }

    public int ResolveEvaluationStoreId(IList<ShoppingCartItem> cart, PromotionEvaluationContext context)
    {
        return context?.StoreId > 0
            ? context.StoreId
            : cart?.FirstOrDefault()?.StoreId ?? 0;
    }

    private async Task<string> ResolveCountryIso2Async(Customer customer)
    {
        var requestCacheKey = $"NopStation.DiscountManagerPlus.CountryIso2.{customer?.Id ?? 0}";
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(requestCacheKey, out var cachedCountryIso2) == true &&
            cachedCountryIso2 is string cachedIso2)
        {
            return cachedIso2;
        }

        var countryIso2 = await ResolveCountryIso2FromIpAsync();
        if (string.IsNullOrWhiteSpace(countryIso2) && customer?.ShippingAddressId.HasValue == true)
        {
            var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(customer);
            if (shippingAddress?.CountryId is > 0)
            {
                var country = await _countryService.GetCountryByAddressAsync(shippingAddress);
                if (country != null && country.Published)
                    countryIso2 = country.TwoLetterIsoCode;
            }
        }

        if (string.IsNullOrWhiteSpace(countryIso2) && customer?.BillingAddressId.HasValue == true)
        {
            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
            if (billingAddress?.CountryId is > 0)
            {
                var country = await _countryService.GetCountryByAddressAsync(billingAddress);
                if (country != null && country.Published)
                    countryIso2 = country.TwoLetterIsoCode;
            }
        }

        if (httpContext != null)
            httpContext.Items[requestCacheKey] = countryIso2 ?? string.Empty;

        return countryIso2 ?? string.Empty;
    }

    private async Task<string> ResolveCountryIso2FromIpAsync()
    {
        var ipAddress = _webHelper.GetCurrentIpAddress();
        if (string.IsNullOrWhiteSpace(ipAddress))
            return string.Empty;

        var countryIso2 = _geoLookupService.LookupCountryIsoCode(ipAddress)?.Trim();
        if (string.IsNullOrWhiteSpace(countryIso2))
            return string.Empty;

        var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(countryIso2);
        if (country == null || !country.Published)
            return string.Empty;

        return country.TwoLetterIsoCode;
    }

    private static string NormalizeCouponCode(string couponCode)
    {
        return string.IsNullOrWhiteSpace(couponCode) ? string.Empty : couponCode.Trim().ToUpperInvariant();
    }
}
