using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

/// <summary>
/// Default implementation of discount coordination service for handling multiple simultaneous discounts
/// with proper cheapest-item selection and double-discount prevention.
/// </summary>
public class DiscountCoordinationService : IDiscountCoordinationService
{
    private readonly IShoppingCartService _shoppingCartService;

    public DiscountCoordinationService(IShoppingCartService shoppingCartService)
    {
        _shoppingCartService = shoppingCartService;
    }

    public async Task<Dictionary<int, List<DiscountAllocation>>> CoordinateCheapestItemDiscountsAsync(
        IList<AppliedPromotion> appliedPromotions,
        IList<ShoppingCartItem> cart)
    {
        var result = new Dictionary<int, List<DiscountAllocation>>();
        if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
            return result;

        // FIXED: Include all BuyXGetY promotions for coordination, not just those with existing line discounts
        // The coordination service needs to process both promotions even if line discounts aren't pre-calculated
        var cheapestItemPromotions = appliedPromotions
            .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
            .ToList();

        if (!cheapestItemPromotions.Any())
            return result;

        // Sort by effective discount value (highest first) to prioritize 100% discounts over 50%
        // This ensures the best discounts get first pick at the cheapest items
        var sortedPromotions = cheapestItemPromotions
            .OrderByDescending(x => CalculateEffectiveDiscountValue(x))
            .ThenBy(x => x.RewardQuantity) // Then by reward quantity (smaller quantities first for efficiency)
            .ToList();

        // CRITICAL FIX: Track allocated QUANTITIES per cart item, not just allocated line IDs
        // This allows multiple units of the same product to receive different promotions
        var allocatedQuantities = new Dictionary<int, int>();

        foreach (var promotion in sortedPromotions)
        {
            var allocations = new List<DiscountAllocation>();

            // FIXED: Handle both cases - with and without pre-calculated LineDiscounts
            IList<int> eligibleLineIds;

            if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
            {
                // Use existing line discounts if available, but limit to RewardQuantity
                // Sort by discount amount descending to get the best discounts first
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
                // CRITICAL FIX: Only use cart items that are in the EligibleShoppingCartItemIds
                // This ensures we only discount items that actually meet the promotion conditions
                if (promotion.EligibleShoppingCartItemIds == null || !promotion.EligibleShoppingCartItemIds.Any())
                {
                    // If no eligible items specified, we can't apply this promotion
                    continue;
                }

                eligibleLineIds = promotion.EligibleShoppingCartItemIds
                    .Where(lineId => cart.Any(item => item.Id == lineId && item.Quantity > 0))
                    .ToList();
            }

            if (!eligibleLineIds.Any())
                continue;

            // CRITICAL FIX: Filter out items where ALL units are already allocated
            // An item is available if: total quantity > allocated quantity
            var availableLineIds = eligibleLineIds
                .Where(lineId => {
                    var cartItem = cart.FirstOrDefault(item => item.Id == lineId);
                    if (cartItem == null) return false;

                    var alreadyAllocated = allocatedQuantities.TryGetValue(lineId, out var allocated) ? allocated : 0;
                    return cartItem.Quantity > alreadyAllocated; // Available units remain
                })
                .ToList();

            if (!availableLineIds.Any())
                continue;

            // Get the actual cart items with available quantities
            var availableItems = cart
                .Where(item => {
                    if (!availableLineIds.Contains(item.Id) || item.Quantity <= 0) return false;

                    var alreadyAllocated = allocatedQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
                    return item.Quantity > alreadyAllocated; // Has available units
                })
                .ToList();

            if (!availableItems.Any())
                continue;

            // CRITICAL FIX: Calculate available quantity across all eligible items
            var requiredQuantity = promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1;

            // Calculate total available units across all eligible items
            var totalAvailableUnits = availableItems.Sum(item => {
                var alreadyAllocated = allocatedQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
                return item.Quantity - alreadyAllocated;
            });

            var itemsToAllocate = Math.Min(requiredQuantity, totalAvailableUnits);

            // Select the cheapest available items (considering available quantities)
            var cheapestItems = await GetLowestPricedItemsExcludingAllocatedAsync(
                availableItems,
                allocatedQuantities,
                itemsToAllocate);

            // Create allocations for the cheapest items (limited by RewardQuantity)
            foreach (var item in cheapestItems)
            {
                if (item == null || item.Quantity <= 0)
                    continue;

                var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(item, false);

                // CRITICAL FIX: Calculate discount amount properly based on discount type
                var discountAmount = 0m;
                if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any() &&
                    promotion.LineDiscounts.TryGetValue(item.Id, out var existingDiscount))
                {
                    // Use the existing line discount amount if available
                    discountAmount = existingDiscount;
                }
                else
                {
                    // Calculate discount based on discount type
                    if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                    {
                        discountAmount = unitPrice; // 100% discount
                    }
                    else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                    {
                        // Use DiscountValue for percentage calculations (e.g., 50 for 50%)
                        var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue : promotion.DiscountAmount;
                        discountAmount = unitPrice * (percentage / 100m);
                    }
                    else if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                    {
                        discountAmount = Math.Min(promotion.DiscountValue, unitPrice);
                    }
                }

                if (discountAmount <= 0)
                    continue;

                // CRITICAL FIX: Track allocated quantities to allow multiple promotions
                // on different units of the same cart item
                allocations.Add(new DiscountAllocation
                {
                    LineId = item.Id,
                    DiscountAmount = discountAmount,
                    PromotionRuleId = promotion.PromotionRuleId,
                    UnitPrice = unitPrice,
                    Quantity = 1, // Always allocate 1 unit per allocation
                    ProductId = item.ProductId,
                    IsFreeItem = promotion.DiscountTypeId == (int)DiscountType.FreeItem ||
                                discountAmount >= unitPrice
                });

                // CRITICAL FIX: Track allocated quantity for this cart item
                // This allows different units of the same product to receive different promotions
                if (!allocatedQuantities.ContainsKey(item.Id))
                    allocatedQuantities[item.Id] = 0;
                allocatedQuantities[item.Id]++;

                // CRITICAL FIX: Stop allocating once we reach the RewardQuantity limit
                // This prevents allocating more items than the promotion allows
                if (allocations.Count >= requiredQuantity)
                    break;
            }

            if (allocations.Any())
            {
                result[promotion.PromotionRuleId] = allocations;
            }
        }

        return result;
    }

    public async Task<IList<ShoppingCartItem>> GetLowestPricedItemsExcludingAllocatedAsync(
        IList<ShoppingCartItem> items,
        IDictionary<int, int> allocatedQuantities,
        int requiredQuantity = 1)
    {
        if (items == null || !items.Any())
            return Array.Empty<ShoppingCartItem>();

        // CRITICAL FIX: Filter out items with no available quantity
        // An item has available quantity if: total quantity > allocated quantity
        var availableItems = items
            .Where(x => {
                if (x == null || x.Quantity <= 0) return false;

                var alreadyAllocated = allocatedQuantities.TryGetValue(x.Id, out var allocated) ? allocated : 0;
                return x.Quantity > alreadyAllocated; // Has available units
            })
            .ToList();

        if (!availableItems.Any())
            return Array.Empty<ShoppingCartItem>();

        // Get unit prices for all available items
        var pricedItems = new List<(ShoppingCartItem Item, decimal UnitPrice, int AvailableQuantity)>();
        foreach (var item in availableItems)
        {
            var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(item, false);
            var alreadyAllocated = allocatedQuantities.TryGetValue(item.Id, out var allocated) ? allocated : 0;
            var availableQuantity = item.Quantity - alreadyAllocated;

            pricedItems.Add((item, unitPrice < 0 ? 0 : unitPrice, availableQuantity));
        }

        if (!pricedItems.Any())
            return Array.Empty<ShoppingCartItem>();

        // Sort by unit price (ascending) to get cheapest items first
        var sortedByPrice = pricedItems
            .OrderBy(x => x.UnitPrice)
            .ToList();

        // CRITICAL FIX: Select the required quantity of cheapest items, considering available quantities
        var result = new List<ShoppingCartItem>();
        int quantityNeeded = requiredQuantity;

        foreach (var (item, price, availableQuantity) in sortedByPrice)
        {
            if (quantityNeeded <= 0)
                break;

            // Add this item as many times as we need it, up to its available quantity
            // For example, if we need 2 items and this item has 2 available units, add it twice
            var timesToAdd = Math.Min(quantityNeeded, availableQuantity);
            for (int i = 0; i < timesToAdd; i++)
            {
                result.Add(item);
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

        // Free items get highest priority (100% discount)
        if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            return 100m;

        // FIXED: For percentage discounts, use DiscountValue (the actual percentage)
        // not DiscountAmount (the calculated discount amount)
        if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
        {
            // Use DiscountValue which contains the actual percentage (e.g., 50 for 50%)
            // If DiscountValue is 0 or negative, fall back to DiscountAmount
            return promotion.DiscountValue > 0 ? promotion.DiscountValue : promotion.DiscountAmount;
        }

        // For fixed amounts, we need to estimate the effective percentage
        // This is tricky without knowing the item price, so we'll use a lower priority
        if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
        {
            // Fixed discounts get lower priority than percentage discounts
            // Use the discount amount as a proxy, but cap at reasonable percentage
            return Math.Min(promotion.DiscountAmount, 50m);
        }

        return promotion.DiscountAmount;
    }
}