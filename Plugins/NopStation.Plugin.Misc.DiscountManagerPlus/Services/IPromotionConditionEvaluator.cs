using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IPromotionConditionEvaluator
{
    Task<bool> EvaluateRuleConditionsAsync(
        PromotionRuleType ruleType,
        IList<PromotionRuleCondition> conditions,
        IList<ShoppingCartItem> cart,
        bool defaultWhenNoConditions,
        int storeId,
        PromotionEvaluationContext context = null);

    Task<ConditionMatchResult> EvaluateRuleConditionsWithQuantityAsync(
        PromotionRuleType ruleType,
        IList<PromotionRuleCondition> conditions,
        IList<ShoppingCartItem> cart,
        bool defaultWhenNoConditions,
        int storeId,
        PromotionEvaluationContext context = null);

    Task<IList<ShoppingCartItem>> GetMatchedCartItemsByRuleProductsAsync(
        IList<ShoppingCartItem> cart,
        IList<PromotionRuleProduct> ruleProducts);

    Task<IList<PromotionRuleProduct>> GetQualifiedProductBasedRuleProductsAsync(
        IList<ShoppingCartItem> cart,
        IList<PromotionRuleProduct> ruleProducts);

    Task<(int SetCount, decimal MatchedSubtotal)> GetComboSetCountAndSubtotalAsync(
        IList<ShoppingCartItem> cart,
        IList<PromotionRuleProduct> buyProducts);

    Task<ComboSetMatchSummary> GetComboSetMatchSummaryAsync(
        IList<ShoppingCartItem> cart,
        IList<PromotionRuleProduct> buyProducts);

    bool HasSpecificCartConditionQuantityCriteria(IList<PromotionRuleCondition> conditions);

    PromotionRuleTier GetMatchingTierByMetric(IList<PromotionRuleTier> tiers, decimal metric);

    IList<PromotionRuleTier> FilterTiersByMatchedRuleProducts(
        IList<PromotionRuleTier> tiers,
        IList<PromotionRuleTierProductMapping> tierMappings,
        IList<int> matchedRuleProductIds);

    IList<PromotionRuleProduct> FilterRuleProductsByTierMapping(
        IList<PromotionRuleProduct> ruleProducts,
        PromotionRuleTier tier,
        IList<PromotionRuleTierProductMapping> tierMappings);
}
