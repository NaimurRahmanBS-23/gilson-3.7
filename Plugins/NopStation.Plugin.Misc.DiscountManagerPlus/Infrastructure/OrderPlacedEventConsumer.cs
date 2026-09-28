using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ISettingService _settingService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly IDiscountService _discountService;
    private readonly IPromotionRuleService _promotionRuleService;

    public OrderPlacedEventConsumer(
        IOrderService orderService,
        ISettingService settingService,
        IDiscountManagerPlusService discountManagerPlusService,
        IDiscountService discountService,
        IPromotionRuleService promotionRuleService)
    {
        _orderService = orderService;
        _settingService = settingService;
        _discountManagerPlusService = discountManagerPlusService;
        _discountService = discountService;
        _promotionRuleService = promotionRuleService;
    }

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage?.Order;
        if (order == null)
            return;

        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(order.StoreId);
        if (!settings.IsEnabled)
            return;

        var appliedPromotions = (await _discountManagerPlusService.GetRequestAppliedPromotionsAsync(order.CustomerId, order.StoreId)).ToList();
        if (!appliedPromotions.Any())
        {
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
            if (!orderItems.Any())
                return;

            var cart = orderItems.Select(orderItem => new ShoppingCartItem
            {
                StoreId = order.StoreId,
                CustomerId = order.CustomerId,
                ShoppingCartTypeId = (int)ShoppingCartType.ShoppingCart,
                ProductId = orderItem.ProductId,
                AttributesXml = orderItem.AttributesXml,
                Quantity = orderItem.Quantity,
                RentalStartDateUtc = orderItem.RentalStartDateUtc,
                RentalEndDateUtc = orderItem.RentalEndDateUtc,
                CreatedOnUtc = order.CreatedOnUtc,
                UpdatedOnUtc = order.CreatedOnUtc
            }).ToList();

            appliedPromotions = (await _discountManagerPlusService.EvaluateCartAsync(cart, order.StoreId)).ToList();
        }
        var parentDiscountCache = new Dictionary<int, bool>();
        var parentDiscountIdsToEnsureHistory = new HashSet<int>();
        foreach (var appliedPromotion in appliedPromotions.Where(HasSuccessfulUsage))
        {
            var promotionRule = await _promotionRuleService.GetPromotionRuleByIdAsync(appliedPromotion.PromotionRuleId);
            if (promotionRule == null)
                continue;

            if (!parentDiscountCache.TryGetValue(appliedPromotion.PromotionRuleId, out var hasParentDiscount))
            {
                hasParentDiscount = promotionRule != null && (promotionRule.DiscountId > 0 || promotionRule.LinkedDiscountId.GetValueOrDefault() > 0);
                parentDiscountCache[appliedPromotion.PromotionRuleId] = hasParentDiscount;

                if (hasParentDiscount)
                {
                    var parentDiscountId = promotionRule.DiscountId > 0
                        ? promotionRule.DiscountId
                        : promotionRule.LinkedDiscountId.GetValueOrDefault();
                    if (parentDiscountId > 0)
                        parentDiscountIdsToEnsureHistory.Add(parentDiscountId);
                }
            }

            var existingRuleUsage = await _promotionRuleService.GetRuleUsagesAsync(
                promotionRuleId: appliedPromotion.PromotionRuleId,
                orderId: order.Id,
                pageIndex: 0,
                pageSize: 1);

            if (!existingRuleUsage.Any())
            {
                await _promotionRuleService.InsertRuleUsageAsync(new PromotionRuleUsage
                {
                    PromotionRuleId = appliedPromotion.PromotionRuleId,
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    DiscountAmountApplied = GetAppliedDiscountAmount(appliedPromotion)
                });
            }

            if (!hasParentDiscount)
                continue;
        }

        foreach (var parentDiscountId in parentDiscountIdsToEnsureHistory)
        {
            var existingUsageHistory = await _discountService.GetAllDiscountUsageHistoryAsync(
                discountId: parentDiscountId,
                orderId: order.Id,
                includeCancelledOrders: true,
                pageIndex: 0,
                pageSize: 1);

            if (existingUsageHistory.Any())
                continue;

            await _discountService.InsertDiscountUsageHistoryAsync(new DiscountUsageHistory
            {
                DiscountId = parentDiscountId,
                OrderId = order.Id,
                CreatedOnUtc = DateTime.UtcNow
            });
        }
    }

    private static bool HasSuccessfulUsage(AppliedPromotion appliedPromotion)
    {
        if (appliedPromotion == null)
            return false;

        return appliedPromotion.DiscountAmount > 0 ||
               (appliedPromotion.LineDiscounts?.Any() ?? false) ||
               (appliedPromotion.RewardProductId.HasValue && appliedPromotion.RewardQuantity > 0) ||
               appliedPromotion.AutoAddReward ||
               appliedPromotion.RequiresRewardSelection;
    }

    private static decimal GetAppliedDiscountAmount(AppliedPromotion appliedPromotion)
    {
        if (appliedPromotion == null)
            return 0m;

        if (appliedPromotion.LineDiscounts?.Any() ?? false)
            return appliedPromotion.LineDiscounts.Sum(x => x.Value);

        return appliedPromotion.DiscountAmount;
    }
}
