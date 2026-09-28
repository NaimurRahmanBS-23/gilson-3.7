using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

/// <summary>
/// Service for coordinating multiple discount allocations to ensure proper cheapest-item selection
/// and prevent double-discounting across multiple promotion rules.
/// </summary>
public interface IDiscountCoordinationService
{
    /// <summary>
    /// Coordinates discount allocations across multiple promotions to ensure cheapest items are selected
    /// for each discount type without double-discounting the same product units.
    /// </summary>
    /// <param name="appliedPromotions">List of promotions to be applied</param>
    /// <param name="cart">Shopping cart items</param>
    /// <returns>Dictionary mapping promotion rule IDs to their coordinated discount allocations</returns>
    Task<Dictionary<int, List<DiscountAllocation>>> CoordinateCheapestItemDiscountsAsync(
        IList<AppliedPromotion> appliedPromotions,
        IList<ShoppingCartItem> cart);

    /// <summary>
    /// Gets the cheapest items from a collection, excluding already-allocated quantities.
    /// </summary>
    /// <param name="items">Available items to select from</param>
    /// <param name="allocatedQuantities">Dictionary tracking how many units of each item are already allocated</param>
    /// <param name="requiredQuantity">Number of items to select</param>
    /// <returns>List of cheapest available items</returns>
    Task<IList<ShoppingCartItem>> GetLowestPricedItemsExcludingAllocatedAsync(
        IList<ShoppingCartItem> items,
        IDictionary<int, int> allocatedQuantities,
        int requiredQuantity = 1);

    /// <summary>
    /// Calculates the effective discount value for sorting purposes.
    /// Higher values should be processed first (100% > 50% > 20%).
    /// </summary>
    decimal CalculateEffectiveDiscountValue(AppliedPromotion promotion);
}

/// <summary>
/// Represents a single discount allocation to a specific cart item.
/// </summary>
public class DiscountAllocation
{
    /// <summary>
    /// Shopping cart line ID receiving the discount
    /// </summary>
    public int LineId { get; set; }

    /// <summary>
    /// Amount of discount being applied
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Promotion rule ID providing this discount
    /// </summary>
    public int PromotionRuleId { get; set; }

    /// <summary>
    /// Unit price of the item before discount
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Quantity of items being discounted
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Product ID for reference
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Whether this is a free item (100% discount)
    /// </summary>
    public bool IsFreeItem { get; set; }
}