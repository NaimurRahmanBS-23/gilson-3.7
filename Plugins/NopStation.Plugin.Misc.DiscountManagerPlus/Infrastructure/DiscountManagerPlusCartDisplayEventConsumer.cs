using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.ShoppingCart;
using NopStation.Plugin.Misc.DiscountManagerPlus;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class DiscountManagerPlusCartDisplayEventConsumer : IConsumer<ModelPreparedEvent<BaseNopModel>>
{
    private const string RequestCacheKey = "NopStation.DiscountManagerPlus.CartDisplayDiscountMap";
    private const string PendingRewardWarningRequestCacheKey = "NopStation.DiscountManagerPlus.PendingRewardWarning";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly IProductService _productService;
    private readonly ITaxService _taxService;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ILocalizationService _localizationService;

    public DiscountManagerPlusCartDisplayEventConsumer(
        IHttpContextAccessor httpContextAccessor,
        ISettingService settingService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        IDiscountManagerPlusService discountManagerPlusService,
        IProductService productService,
        ITaxService taxService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        ILocalizationService localizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _settingService = settingService;
        _storeContext = storeContext;
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _discountManagerPlusService = discountManagerPlusService;
        _productService = productService;
        _taxService = taxService;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _localizationService = localizationService;
    }

    public async Task HandleEventAsync(ModelPreparedEvent<BaseNopModel> eventMessage)
    {
        if (eventMessage?.Model == null)
            return;

        if (eventMessage.Model is ShoppingCartModel shoppingCartModel)
        {
            if (shoppingCartModel.Items != null && shoppingCartModel.Items.Any())
            {
                var lineDiscountMap = await GetOrCreateDiscountMapAsync();
                if (lineDiscountMap.Any())
                {
                    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
                    foreach (var item in shoppingCartModel.Items)
                    {
                        if (!lineDiscountMap.TryGetValue(item.Id, out var lineDiscountBase) || lineDiscountBase <= 0)
                            continue;

                        var product = await _productService.GetProductByIdAsync(item.ProductId);
                        if (product == null)
                            continue;

                        var (lineDiscountWithTaxBase, _) = await _taxService.GetProductPriceAsync(product, lineDiscountBase);
                        if (lineDiscountWithTaxBase <= 0)
                            continue;

                        var lineDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(lineDiscountWithTaxBase, currentCurrency);
                        if (lineDiscount <= item.DiscountValue)
                            continue;

                        item.DiscountValue = lineDiscount;
                        item.Discount = await _priceFormatter.FormatPriceAsync(lineDiscount);
                    }
                }
            }

            await AppendPendingRewardWarningAsync(shoppingCartModel.Warnings);
            return;
        }

        if (eventMessage.Model is CheckoutConfirmModel checkoutConfirmModel)
            await AppendPendingRewardWarningAsync(checkoutConfirmModel.Warnings);
    }

    private async Task<Dictionary<int, decimal>> GetOrCreateDiscountMapAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled)
            return new Dictionary<int, decimal>();

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || customer.Deleted)
            return new Dictionary<int, decimal>();

        await _discountManagerPlusService.SynchronizeAutoAddedRewardsAsync(customer, store.Id);

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return new Dictionary<int, decimal>();

        var requestCacheKey = BuildRequestCacheKey(store.Id, customer.Id, cart);
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(requestCacheKey, out var existing) ?? false)
        {
            if (existing is Task<Dictionary<int, decimal>> existingTask)
                return await existingTask;

            if (existing is Dictionary<int, decimal> existingMap)
                return existingMap;
        }

        var mapTask = CreateDiscountMapAsync(cart, store.Id);
        if (context != null)
            context.Items[requestCacheKey] = mapTask;

        var map = await mapTask;
        if (context != null)
            context.Items[requestCacheKey] = map;

        return map;
    }

    private static string BuildRequestCacheKey(int storeId, int customerId, IList<ShoppingCartItem> cart)
    {
        var cartKey = string.Join(",", cart.OrderBy(x => x.Id).Select(x => $"{x.Id}-{x.ProductId}-{x.Quantity}"));
        return $"{RequestCacheKey}.{storeId}.{customerId}.{cartKey}";
    }

    private async Task<Dictionary<int, decimal>> CreateDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId)
    {
        return (await _discountManagerPlusService.BuildLineDiscountMapAsync(cart, storeId))
            .Where(x => x.Value > 0)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private async Task AppendPendingRewardWarningAsync(IList<string> warnings)
    {
        if (warnings == null)
            return;

        var warning = await GetOrCreatePendingRewardWarningAsync();
        if (string.IsNullOrWhiteSpace(warning))
            return;

        if (!warnings.Any(x => x.Equals(warning, StringComparison.OrdinalIgnoreCase)))
            warnings.Add(warning);
    }

    private async Task<string> GetOrCreatePendingRewardWarningAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(PendingRewardWarningRequestCacheKey, out var existing) ?? false)
        {
            if (existing is Task<string> existingTask)
                return await existingTask;

            if (existing is string existingWarning)
                return existingWarning;
        }

        var warningTask = CreatePendingRewardWarningAsync();
        if (context != null)
            context.Items[PendingRewardWarningRequestCacheKey] = warningTask;

        var warning = await warningTask;
        if (context != null)
            context.Items[PendingRewardWarningRequestCacheKey] = warning;

        return warning;
    }

    private async Task<string> CreatePendingRewardWarningAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled)
            return string.Empty;

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || customer.Deleted)
            return string.Empty;

        await _discountManagerPlusService.SynchronizeAutoAddedRewardsAsync(customer, store.Id);

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return string.Empty;

        var pendingPromotions = (await _discountManagerPlusService.EvaluateCartAsync(cart, store.Id))
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

        var warningTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout");
        var fallbackWarning = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout.Default");
        if (string.IsNullOrWhiteSpace(warningTemplate) || !pendingRuleNames.Any())
            return fallbackWarning;

        return string.Format(warningTemplate, string.Join(", ", pendingRuleNames));
    }
}
