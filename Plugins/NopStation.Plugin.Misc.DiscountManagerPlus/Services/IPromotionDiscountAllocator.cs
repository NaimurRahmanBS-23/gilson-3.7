using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IPromotionDiscountAllocator
{
    Task<decimal> GetCartSubtotalAsync(IList<ShoppingCartItem> cart);

    Task<decimal> GetItemsSubtotalAsync(IList<ShoppingCartItem> items);

    Task<decimal> GetItemsSubtotalAsync(IList<ShoppingCartItem> items, IDictionary<int, int> quantitiesByLineId);

    Task<decimal> GetApplicableSubtotalAsync(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionRuleTier tier = null);

    Task<AppliedPromotion> BuildAppliedPromotionAsync(
        PromotionRule rule,
        IList<ShoppingCartItem> cart,
        PromotionRuleTier tier = null,
        decimal? overrideDiscountAmount = null);

    Task<AppliedPromotion> ApplyLinkedMaximumDiscountedQuantityAsync(
        PromotionRule rule,
        AppliedPromotion appliedPromotion,
        IList<ShoppingCartItem> cart);

    Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts)> CalculateRewardDiscountsAsync(
        IList<ShoppingCartItem> rewardItems,
        int rewardQuantity);

    Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts)> CalculateRewardDiscountsAsync(
        IList<ShoppingCartItem> rewardItems,
        int rewardQuantity,
        IDictionary<int, int> availableQuantitiesByLineId);

    Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts)> CalculateRewardDiscountsAsync(
        IList<ShoppingCartItem> rewardItems,
        int rewardQuantity,
        DiscountType discountType,
        decimal discountValue);

    Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts)> CalculateRewardDiscountsAsync(
        IList<ShoppingCartItem> rewardItems,
        int rewardQuantity,
        IDictionary<int, int> availableQuantitiesByLineId,
        DiscountType discountType,
        decimal discountValue);

    Task<decimal> MergeLineDiscountsWithCapAsync(
        IList<ShoppingCartItem> cart,
        IDictionary<int, decimal> aggregateLineDiscounts,
        IDictionary<int, decimal> candidateLineDiscounts,
        IDictionary<int, decimal> lineSubtotalCache);

    Task<(Dictionary<int, decimal> LineDiscountMap, Dictionary<int, decimal> RuleDiscountMap)> BuildDiscountMapsAsync(
        PromotionEvaluationContext context,
        Func<PromotionRule, PromotionEvaluationContext, Task<bool>> isRuleRuntimeEligibleAsync,
        Func<PromotionRule, IList<ShoppingCartItem>, PromotionEvaluationContext, Task<AppliedPromotion>> evaluateRuleAsync);

    Task<IList<ShoppingCartItem>> GetEligibleCartItemsForAppliedPromotionAsync(
        PromotionRule rule,
        AppliedPromotion appliedPromotion,
        IList<ShoppingCartItem> cart);

    Task<Dictionary<int, decimal>> AllocateDiscountAcrossItemsAsync(
        decimal discountAmount,
        IList<ShoppingCartItem> eligibleItems,
        IDictionary<int, int> discountedQuantitiesByLineId = null);

    /// <summary>
    /// Allocates discount across items using cheapest-first strategy
    /// </summary>
    Task<Dictionary<int, decimal>> AllocateDiscountCheapestFirstAsync(
        decimal discountAmount,
        IList<ShoppingCartItem> eligibleItems,
        IDictionary<int, int> discountedQuantitiesByLineId = null);

    /// <summary>
    /// Generates attention/boost messages when better discounts are available
    /// </summary>
    Task<IList<PromotionAttentionMessage>> GenerateAttentionMessagesAsync(
        PromotionEvaluationContext context,
        IDictionary<int, decimal> finalLineDiscountMap,
        IDictionary<int, decimal> finalRuleDiscountMap);

    /// <summary>
    /// Calculate reward discounts with quantity tracking for dual-offer scenarios
    /// Returns discount amounts, line discounts, and which specific quantities were discounted
    /// </summary>
    Task<(decimal DiscountAmount, Dictionary<int, decimal> LineDiscounts, Dictionary<int, int> DiscountedQuantities)> CalculateRewardDiscountsQuantizedAsync(
        IList<ShoppingCartItem> rewardItems,
        int rewardQuantity,
        IDictionary<int, int> availableQuantitiesByLineId,
        DiscountType discountType,
        decimal discountValue);
}
