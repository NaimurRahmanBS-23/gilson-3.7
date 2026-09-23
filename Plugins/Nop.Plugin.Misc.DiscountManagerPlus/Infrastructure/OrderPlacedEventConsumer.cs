using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly ISettingService _settingService;
        private readonly IDiscountManagerPlusService _discountManagerPlusService;
        private readonly IDiscountService _discountService;
        private readonly IPromotionRuleService _promotionRuleService;

        public OrderPlacedEventConsumer(
            ISettingService settingService,
            IDiscountManagerPlusService discountManagerPlusService,
            IDiscountService discountService,
            IPromotionRuleService promotionRuleService)
        {
            _settingService = settingService;
            _discountManagerPlusService = discountManagerPlusService;
            _discountService = discountService;
            _promotionRuleService = promotionRuleService;
        }

        public void HandleEvent(OrderPlacedEvent eventMessage)
        {
            var order = eventMessage != null ? eventMessage.Order : null;
            if (order == null)
                return;

            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(order.StoreId);
            if (!settings.IsEnabled)
                return;

            var appliedPromotions = (_discountManagerPlusService.GetRequestAppliedPromotions(order.CustomerId, order.StoreId) ?? new List<AppliedPromotion>()).ToList();
            if (!appliedPromotions.Any())
            {
                var orderItems = order.OrderItems;
                if (orderItems == null || !orderItems.Any())
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

                appliedPromotions = (_discountManagerPlusService.EvaluateCart(cart, order.StoreId) ?? new List<AppliedPromotion>()).ToList();
            }

            var parentDiscountCache = new Dictionary<int, bool>();
            var parentDiscountIdsToEnsureHistory = new HashSet<int>();
            foreach (var appliedPromotion in appliedPromotions.Where(HasSuccessfulUsage))
            {
                var promotionRule = _promotionRuleService.GetPromotionRuleById(appliedPromotion.PromotionRuleId);
                if (promotionRule == null)
                    continue;

                bool hasParentDiscount;
                if (!parentDiscountCache.TryGetValue(appliedPromotion.PromotionRuleId, out hasParentDiscount))
                {
                    hasParentDiscount = promotionRule.DiscountId > 0 || promotionRule.LinkedDiscountId.GetValueOrDefault() > 0;
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

                var existingRuleUsage = _promotionRuleService.GetRuleUsages(
                    promotionRuleId: appliedPromotion.PromotionRuleId,
                    orderId: order.Id,
                    pageIndex: 0,
                    pageSize: 1);

                if (existingRuleUsage == null || !existingRuleUsage.Any())
                {
                    _promotionRuleService.InsertRuleUsage(new PromotionRuleUsage
                    {
                        PromotionRuleId = appliedPromotion.PromotionRuleId,
                        OrderId = order.Id,
                        CustomerId = order.CustomerId,
                        DiscountAmountApplied = GetAppliedDiscountAmount(appliedPromotion),
                        CreatedOnUtc = DateTime.UtcNow
                    });
                }

                if (!hasParentDiscount)
                    continue;
            }

            foreach (var parentDiscountId in parentDiscountIdsToEnsureHistory)
            {
                var existingUsageHistory = _discountService.GetAllDiscountUsageHistory(
                    discountId: parentDiscountId,
                    orderId: order.Id,
                    pageIndex: 0,
                    pageSize: 1);

                if (existingUsageHistory != null && existingUsageHistory.Any())
                    continue;

                _discountService.InsertDiscountUsageHistory(new DiscountUsageHistory
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
                   (appliedPromotion.LineDiscounts != null && appliedPromotion.LineDiscounts.Any()) ||
                   (appliedPromotion.RewardProductId.HasValue && appliedPromotion.RewardQuantity > 0) ||
                   appliedPromotion.AutoAddReward ||
                   appliedPromotion.RequiresRewardSelection;
        }

        private static decimal GetAppliedDiscountAmount(AppliedPromotion appliedPromotion)
        {
            if (appliedPromotion == null)
                return 0m;

            if (appliedPromotion.LineDiscounts != null && appliedPromotion.LineDiscounts.Any())
                return appliedPromotion.LineDiscounts.Sum(x => x.Value);

            return appliedPromotion.DiscountAmount;
        }
    }
}
