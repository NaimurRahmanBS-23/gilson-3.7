using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

/// <summary>
/// Service for generating contextual attention messages that explain discount allocations
/// and provide actionable savings maximization tips to customers.
/// </summary>
public interface IPromotionAttentionMessageService
{
    /// <summary>
    /// Generates attention messages for dual-offer scenarios, explaining why specific
    /// items were chosen for discounts and providing actionable suggestions.
    /// </summary>
    /// <param name="appliedPromotions">List of promotions that were applied to the cart</param>
    /// <param name="cart">Shopping cart items</param>
    /// <param name="coordinatedAllocations">Discount allocations from coordination service</param>
    /// <returns>List of attention messages explaining the discount choices</returns>
    Task<IList<PromotionAttentionMessage>> GenerateDualOfferMessagesAsync(
        IList<AppliedPromotion> appliedPromotions,
        IList<ShoppingCartItem> cart,
        IDictionary<int, List<DiscountAllocation>> coordinatedAllocations);

    /// <summary>
    /// Generates a comprehensive explanation of how dual-offer discounts were applied,
    /// including reasoning for item selection and total savings calculation.
    /// </summary>
    /// <param name="appliedPromotions">List of promotions that were applied to the cart</param>
    /// <param name="coordinatedAllocations">Discount allocations from coordination service</param>
    /// <param name="cart">Shopping cart items</param>
    /// <returns>Detailed explanation of discount allocation decisions</returns>
    Task<DualOfferExplanation> GenerateDualOfferExplanationAsync(
        IList<AppliedPromotion> appliedPromotions,
        IDictionary<int, List<DiscountAllocation>> coordinatedAllocations,
        IList<ShoppingCartItem> cart);

    /// <summary>
    /// Generates actionable tips for maximizing savings by adding additional items
    /// to the cart or adjusting quantities.
    /// </summary>
    /// <param name="appliedPromotions">List of promotions currently applied to the cart</param>
    /// <param name="cart">Current shopping cart items</param>
    /// <param name="eligibleProductIds">Product IDs eligible for promotions</param>
    /// <returns>List of maximization tips with potential savings calculations</returns>
    Task<IList<SavingsMaximizationTip>> GenerateMaximizationTipsAsync(
        IList<AppliedPromotion> appliedPromotions,
        IList<ShoppingCartItem> cart,
        IList<int> eligibleProductIds);

    /// <summary>
    /// Detects the scenario type based on cart composition to enable context-aware messaging.
    /// </summary>
    /// <param name="cart">Shopping cart items</param>
    /// <param name="allocations">Discount allocations</param>
    /// <returns>The detected scenario type for contextual messaging</returns>
    ScenarioType DetectScenarioType(
        IList<ShoppingCartItem> cart,
        IDictionary<int, List<DiscountAllocation>> allocations);
}