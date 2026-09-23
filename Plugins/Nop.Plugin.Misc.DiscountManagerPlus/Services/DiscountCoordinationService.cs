using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using PluginDiscountType = Nop.Plugin.Misc.DiscountManagerPlus.Domain.DiscountType;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class DiscountCoordinationService : IDiscountCoordinationService
    {
        private readonly IPriceCalculationService _priceCalculationService;

        public DiscountCoordinationService(IPriceCalculationService priceCalculationService)
        {
            _priceCalculationService = priceCalculationService;
        }

        public Dictionary<int, List<DiscountAllocation>> CoordinateCheapestItemDiscounts(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart)
        {
            var result = new Dictionary<int, List<DiscountAllocation>>();
            if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
                return result;

            var cheapestItemPromotions = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                .ToList();

            if (!cheapestItemPromotions.Any())
                return result;

            var sortedPromotions = cheapestItemPromotions
                .OrderByDescending(x => CalculateEffectiveDiscountValue(x))
                .ThenBy(x => x.RewardQuantity)
                .ToList();

            var allocatedQuantities = new Dictionary<int, int>();

            foreach (var promotion in sortedPromotions)
            {
                var allocations = new List<DiscountAllocation>();

                IList<int> eligibleLineIds;

                if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
                {
                    var sortedLineDiscounts = promotion.LineDiscounts
                        .Where(x => x.Value > 0)
                        .OrderByDescending(x => x.Value)
                        .Take(promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1)
                        .ToList();

                    eligibleLineIds = sortedLineDiscounts
                        .Select(x => x.Key)
                        .ToList();
                }
                else
                {
                    if (promotion.EligibleShoppingCartItemIds == null || !promotion.EligibleShoppingCartItemIds.Any())
                        continue;

                    eligibleLineIds = promotion.EligibleShoppingCartItemIds
                        .Where(lineId => cart.Any(item => item.Id == lineId && item.Quantity > 0))
                        .ToList();
                }

                if (!eligibleLineIds.Any())
                    continue;

                var availableLineIds = eligibleLineIds
                    .Where(lineId =>
                    {
                        var cartItem = cart.FirstOrDefault(item => item.Id == lineId);
                        if (cartItem == null)
                            return false;

                        var allocated = 0;
                        allocatedQuantities.TryGetValue(lineId, out allocated);
                        return cartItem.Quantity > allocated;
                    })
                    .ToList();

                if (!availableLineIds.Any())
                    continue;

                var availableItems = cart
                    .Where(item =>
                    {
                        if (!availableLineIds.Contains(item.Id) || item.Quantity <= 0)
                            return false;

                        var allocated = 0;
                        allocatedQuantities.TryGetValue(item.Id, out allocated);
                        return item.Quantity > allocated;
                    })
                    .ToList();

                if (!availableItems.Any())
                    continue;

                var requiredQuantity = promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1;

                var totalAvailableUnits = availableItems.Sum(item =>
                {
                    var allocated = 0;
                    allocatedQuantities.TryGetValue(item.Id, out allocated);
                    return item.Quantity - allocated;
                });

                var itemsToAllocate = Math.Min(requiredQuantity, totalAvailableUnits);

                var cheapestItems = GetLowestPricedItemsExcludingAllocated(
                    availableItems,
                    allocatedQuantities,
                    itemsToAllocate);

                foreach (var item in cheapestItems)
                {
                    if (item == null || item.Quantity <= 0)
                        continue;

                    var unitPrice = _priceCalculationService.GetUnitPrice(item, false);

                    var discountAmount = 0m;
                    var existingDiscount = 0m;
                    if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any() &&
                        promotion.LineDiscounts.TryGetValue(item.Id, out existingDiscount))
                    {
                        discountAmount = existingDiscount;
                    }
                    else
                    {
                        if (promotion.DiscountTypeId == (int)PluginDiscountType.FreeItem)
                        {
                            discountAmount = unitPrice;
                        }
                        else if (promotion.DiscountTypeId == (int)PluginDiscountType.Percentage)
                        {
                            var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue : promotion.DiscountAmount;
                            discountAmount = unitPrice * (percentage / 100m);
                        }
                        else if (promotion.DiscountTypeId == (int)PluginDiscountType.FixedAmount)
                        {
                            discountAmount = Math.Min(promotion.DiscountValue, unitPrice);
                        }
                    }

                    if (discountAmount <= 0)
                        continue;

                    allocations.Add(new DiscountAllocation
                    {
                        LineId = item.Id,
                        DiscountAmount = discountAmount,
                        PromotionRuleId = promotion.PromotionRuleId,
                        UnitPrice = unitPrice,
                        Quantity = 1,
                        ProductId = item.ProductId,
                        IsFreeItem = promotion.DiscountTypeId == (int)PluginDiscountType.FreeItem ||
                                    discountAmount >= unitPrice
                    });

                    if (!allocatedQuantities.ContainsKey(item.Id))
                        allocatedQuantities[item.Id] = 0;
                    allocatedQuantities[item.Id]++;

                    if (allocations.Count >= requiredQuantity)
                        break;
                }

                if (allocations.Any())
                    result[promotion.PromotionRuleId] = allocations;
            }

            return result;
        }

        public IList<ShoppingCartItem> GetLowestPricedItemsExcludingAllocated(
            IList<ShoppingCartItem> items,
            IDictionary<int, int> allocatedQuantities,
            int requiredQuantity = 1)
        {
            if (items == null || !items.Any())
                return new ShoppingCartItem[0];

            var availableItems = items
                .Where(x =>
                {
                    if (x == null || x.Quantity <= 0)
                        return false;

                    var allocated = 0;
                    allocatedQuantities.TryGetValue(x.Id, out allocated);
                    return x.Quantity > allocated;
                })
                .ToList();

            if (!availableItems.Any())
                return new ShoppingCartItem[0];

            var pricedItems = new List<PricedCartItem>();
            foreach (var item in availableItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                var allocated = 0;
                allocatedQuantities.TryGetValue(item.Id, out allocated);
                var availableQuantity = item.Quantity - allocated;

                pricedItems.Add(new PricedCartItem
                {
                    Item = item,
                    UnitPrice = unitPrice < 0 ? 0 : unitPrice,
                    AvailableQuantity = availableQuantity
                });
            }

            if (!pricedItems.Any())
                return new ShoppingCartItem[0];

            var sortedByPrice = pricedItems
                .OrderBy(x => x.UnitPrice)
                .ToList();

            var result = new List<ShoppingCartItem>();
            var quantityNeeded = requiredQuantity;

            foreach (var priced in sortedByPrice)
            {
                if (quantityNeeded <= 0)
                    break;

                var timesToAdd = Math.Min(quantityNeeded, priced.AvailableQuantity);
                for (var i = 0; i < timesToAdd; i++)
                {
                    result.Add(priced.Item);
                    quantityNeeded--;
                }

                if (quantityNeeded <= 0)
                    break;
            }

            return result;
        }

        public decimal CalculateEffectiveDiscountValue(AppliedPromotion promotion)
        {
            if (promotion == null)
                return 0m;

            if (promotion.DiscountTypeId == (int)PluginDiscountType.FreeItem)
                return 100m;

            if (promotion.DiscountTypeId == (int)PluginDiscountType.Percentage)
                return promotion.DiscountValue > 0 ? promotion.DiscountValue : promotion.DiscountAmount;

            if (promotion.DiscountTypeId == (int)PluginDiscountType.FixedAmount)
                return Math.Min(promotion.DiscountAmount, 50m);

            return promotion.DiscountAmount;
        }

        private class PricedCartItem
        {
            public ShoppingCartItem Item { get; set; }
            public decimal UnitPrice { get; set; }
            public int AvailableQuantity { get; set; }
        }
    }
}
