using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using PluginDiscountType = Nop.Plugin.Misc.DiscountManagerPlus.Domain.DiscountType;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionDiscountAllocator
    {
        decimal GetCartSubtotal(IList<ShoppingCartItem> cart);

        decimal GetItemsSubtotal(IList<ShoppingCartItem> items);

        decimal GetItemsSubtotal(IList<ShoppingCartItem> items, IDictionary<int, int> quantitiesByLineId);

        decimal GetApplicableSubtotal(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionRuleTier tier = null);

        AppliedPromotion BuildAppliedPromotion(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionRuleTier tier = null,
            decimal? overrideDiscountAmount = null);

        AppliedPromotion ApplyLinkedMaximumDiscountedQuantity(
            PromotionRule rule,
            AppliedPromotion appliedPromotion,
            IList<ShoppingCartItem> cart);

        RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity);

        RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId);

        RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            PluginDiscountType discountType,
            decimal discountValue);

        RewardDiscountResult CalculateRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId,
            PluginDiscountType discountType,
            decimal discountValue);

        decimal MergeLineDiscountsWithCap(
            IList<ShoppingCartItem> cart,
            IDictionary<int, decimal> aggregateLineDiscounts,
            IDictionary<int, decimal> candidateLineDiscounts,
            IDictionary<int, decimal> lineSubtotalCache);

        DiscountMapsResult BuildDiscountMaps(
            PromotionEvaluationContext context,
            Func<PromotionRule, PromotionEvaluationContext, bool> isRuleRuntimeEligible,
            Func<PromotionRule, IList<ShoppingCartItem>, PromotionEvaluationContext, AppliedPromotion> evaluateRule);

        IList<ShoppingCartItem> GetEligibleCartItemsForAppliedPromotion(
            PromotionRule rule,
            AppliedPromotion appliedPromotion,
            IList<ShoppingCartItem> cart);

        Dictionary<int, decimal> AllocateDiscountAcrossItems(
            decimal discountAmount,
            IList<ShoppingCartItem> eligibleItems,
            IDictionary<int, int> discountedQuantitiesByLineId = null);

        Dictionary<int, decimal> AllocateDiscountCheapestFirst(
            decimal discountAmount,
            IList<ShoppingCartItem> eligibleItems,
            IDictionary<int, int> discountedQuantitiesByLineId = null);

        IList<PromotionAttentionMessage> GenerateAttentionMessages(
            PromotionEvaluationContext context,
            IDictionary<int, decimal> finalLineDiscountMap,
            IDictionary<int, decimal> finalRuleDiscountMap);

        RewardDiscountQuantizedResult CalculateRewardDiscountsQuantized(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId,
            PluginDiscountType discountType,
            decimal discountValue);
    }
}
