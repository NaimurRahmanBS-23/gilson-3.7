using System.Threading;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class DiscountManagerPlusEventConsumer : IConsumer<GetShoppingCartItemUnitPriceEvent>
{
    private const string RequestCacheKey = "NopStation.DiscountManagerPlus.RequestDiscountMapTask";
    private const string RequestUnitDiscountCacheKey = "NopStation.DiscountManagerPlus.RequestUnitDiscountMapTask";
    private const string ReentryGuardKey = "NopStation.DiscountManagerPlus.PriceEventGuard";
    private static readonly AsyncLocal<bool> _priceEventGuard = new();

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISettingService _settingService;
    private readonly ICustomerService _customerService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;

    public DiscountManagerPlusEventConsumer(
        IHttpContextAccessor httpContextAccessor,
        ISettingService settingService,
        ICustomerService customerService,
        IShoppingCartService shoppingCartService,
        IDiscountManagerPlusService discountManagerPlusService)
    {
        _httpContextAccessor = httpContextAccessor;
        _settingService = settingService;
        _customerService = customerService;
        _shoppingCartService = shoppingCartService;
        _discountManagerPlusService = discountManagerPlusService;
    }

    public async Task HandleEventAsync(GetShoppingCartItemUnitPriceEvent eventMessage)
    {
        if (eventMessage?.ShoppingCartItem == null || !eventMessage.IncludeDiscounts)
            return;

        var cartItem = eventMessage.ShoppingCartItem;
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(cartItem.StoreId);
        if (!settings.IsEnabled)
            return;

        var context = _httpContextAccessor.HttpContext;
        if (_priceEventGuard.Value || (context?.Items.ContainsKey(ReentryGuardKey) ?? false))
            return;

        var discountMap = await GetOrCreateDiscountMapAsync(cartItem);
        if (!discountMap.TryGetValue(cartItem.Id, out var lineDiscountAmount) || lineDiscountAmount <= 0)
            return;

        // Calculate per-unit discount (nopCommerce limitation: same discount applied to all units)
        var perUnitDiscount = lineDiscountAmount / cartItem.Quantity;

        if (perUnitDiscount <= 0)
            return;

        decimal defaultUnitPrice;
        decimal defaultDiscountAmount;
        List<Nop.Core.Domain.Discounts.Discount> appliedDiscounts;
        try
        {
            _priceEventGuard.Value = true;
            if (context != null)
                context.Items[ReentryGuardKey] = true;

            (defaultUnitPrice, defaultDiscountAmount, appliedDiscounts) = await _shoppingCartService.GetUnitPriceAsync(cartItem, true);
        }
        finally
        {
            _priceEventGuard.Value = false;
            context?.Items.Remove(ReentryGuardKey);
        }

        if (defaultUnitPrice <= 0)
            return;

        if (perUnitDiscount > defaultUnitPrice)
            perUnitDiscount = defaultUnitPrice;

        appliedDiscounts ??= new List<Discount>();
        if (!appliedDiscounts.Any())
            appliedDiscounts.Add(CreateDiscountManagerPlusMarkerDiscount(perUnitDiscount));

        eventMessage.UnitPrice = defaultUnitPrice - perUnitDiscount;
        eventMessage.DiscountAmount = defaultDiscountAmount + perUnitDiscount;
        eventMessage.AppliedDiscounts = appliedDiscounts;
        eventMessage.StopProcessing = true;
    }

    private static Discount CreateDiscountManagerPlusMarkerDiscount(decimal perUnitDiscount)
    {
        return new Discount
        {
            Name = "DiscountManagerPlus",
            DiscountType = Nop.Core.Domain.Discounts.DiscountType.AssignedToSkus,
            DiscountAmount = perUnitDiscount,
            IsActive = true
        };
    }

    private async Task<Dictionary<int, decimal>> GetOrCreateDiscountMapAsync(ShoppingCartItem shoppingCartItem)
    {
        var map = new Dictionary<int, decimal>();
        if (shoppingCartItem.CustomerId <= 0)
            return map;

        var customer = await _customerService.GetCustomerByIdAsync(shoppingCartItem.CustomerId);
        if (customer == null || customer.Deleted)
            return map;

        await _discountManagerPlusService.SynchronizeAutoAddedRewardsAsync(customer, shoppingCartItem.StoreId);

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, shoppingCartItem.StoreId);
        if (!cart.Any())
            return map;

        var requestCacheKey = BuildRequestCacheKey(shoppingCartItem.StoreId, shoppingCartItem.CustomerId, cart);
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(requestCacheKey, out var existing) ?? false)
        {
            if (existing is Task<Dictionary<int, decimal>> existingTask)
                return await existingTask;

            if (existing is Dictionary<int, decimal> existingMap)
                return existingMap;
        }

        var mapTask = CreateDiscountMapAsync(cart, shoppingCartItem.StoreId);
        if (context != null)
            context.Items[requestCacheKey] = mapTask;

        map = await mapTask;
        if (context != null)
            context.Items[requestCacheKey] = map;

        return map;
    }

    /// <summary>
    /// FIXED: Get unit-level discounts for proper dual-offer coordination
    /// This returns a list of discount amounts per unit for each cart line
    /// </summary>
    private async Task<Dictionary<int, List<decimal>>> GetOrCreateUnitDiscountMapAsync(ShoppingCartItem shoppingCartItem)
    {
        var unitDiscountMap = new Dictionary<int, List<decimal>>();
        if (shoppingCartItem.CustomerId <= 0)
            return unitDiscountMap;

        var customer = await _customerService.GetCustomerByIdAsync(shoppingCartItem.CustomerId);
        if (customer == null || customer.Deleted)
            return unitDiscountMap;

        await _discountManagerPlusService.SynchronizeAutoAddedRewardsAsync(customer, shoppingCartItem.StoreId);

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, shoppingCartItem.StoreId);
        if (!cart.Any())
            return unitDiscountMap;

        var requestCacheKey = BuildRequestCacheKey(shoppingCartItem.StoreId, shoppingCartItem.CustomerId, cart);
        var unitDiscountCacheKey = $"{RequestUnitDiscountCacheKey}.{requestCacheKey}";
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.TryGetValue(unitDiscountCacheKey, out var existing) ?? false)
        {
            if (existing is Task<Dictionary<int, List<decimal>>> existingTask)
                return await existingTask;

            if (existing is Dictionary<int, List<decimal>> existingMap)
                return existingMap;
        }

        var mapTask = CreateUnitDiscountMapAsync(cart, shoppingCartItem.StoreId);
        if (context != null)
            context.Items[unitDiscountCacheKey] = mapTask;

        unitDiscountMap = await mapTask;
        if (context != null)
            context.Items[unitDiscountCacheKey] = unitDiscountMap;

        return unitDiscountMap;
    }

    private static string BuildRequestCacheKey(int storeId, int customerId, IList<ShoppingCartItem> cart)
    {
        var cartKey = string.Join(",", cart.OrderBy(x => x.Id).Select(x => $"{x.Id}-{x.ProductId}-{x.Quantity}"));
        return $"{RequestCacheKey}.{storeId}.{customerId}.{cartKey}";
    }

    private async Task<Dictionary<int, decimal>> CreateDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId)
    {
        var map = new Dictionary<int, decimal>();

        var lineDiscountMap = await _discountManagerPlusService.BuildLineDiscountMapAsync(cart, storeId);
        foreach (var lineDiscount in lineDiscountMap)
        {
            if (lineDiscount.Value > 0)
                map[lineDiscount.Key] = lineDiscount.Value;
        }

        return map;
    }

    /// <summary>
    /// FIXED: Create unit-level discount map for proper dual-offer coordination
    /// </summary>
    private async Task<Dictionary<int, List<decimal>>> CreateUnitDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId)
    {
        var unitDiscountMap = new Dictionary<int, List<decimal>>();

        System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Creating unit discount map for {cart.Count} cart items");

        // Get coordinated allocations from the discount manager service
        var coordinatedAllocations = await _discountManagerPlusService.GetCoordinatedAllocationsAsync(cart, storeId);

        System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Got {coordinatedAllocations.Count} coordinated allocations");

        foreach (var allocation in coordinatedAllocations)
        {
            var lineId = allocation.LineId;
            var discountAmount = allocation.DiscountAmount;

            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Allocation - Line {lineId}, Discount ${discountAmount}");

            if (discountAmount <= 0)
                continue;

            if (!unitDiscountMap.ContainsKey(lineId))
                unitDiscountMap[lineId] = new List<decimal>();

            // Each allocation represents 1 unit's discount
            unitDiscountMap[lineId].Add(discountAmount);
        }

        foreach (var kvp in unitDiscountMap)
        {
            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Final unit discounts for line {kvp.Key}: [{string.Join(", ", kvp.Value)}]");
        }

        return unitDiscountMap;
    }
}
