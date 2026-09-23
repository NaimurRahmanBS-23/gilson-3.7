using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionConditionEvaluator
    {
        bool EvaluateRuleConditions(
            PromotionRuleType ruleType,
            IList<PromotionRuleCondition> conditions,
            IList<ShoppingCartItem> cart,
            bool defaultWhenNoConditions,
            int storeId,
            PromotionEvaluationContext context = null);

        ConditionMatchResult EvaluateRuleConditionsWithQuantity(
            PromotionRuleType ruleType,
            IList<PromotionRuleCondition> conditions,
            IList<ShoppingCartItem> cart,
            bool defaultWhenNoConditions,
            int storeId,
            PromotionEvaluationContext context = null);

        IList<ShoppingCartItem> GetMatchedCartItemsByRuleProducts(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts);

        IList<PromotionRuleProduct> GetQualifiedProductBasedRuleProducts(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts);

        ComboSetCountResult GetComboSetCountAndSubtotal(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> buyProducts);

        ComboSetMatchSummary GetComboSetMatchSummary(
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
}
