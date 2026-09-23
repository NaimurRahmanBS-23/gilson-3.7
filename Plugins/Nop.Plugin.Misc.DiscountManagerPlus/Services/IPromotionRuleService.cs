using System;
using System.Collections.Generic;
using Nop.Core;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IPromotionRuleService
    {
        PromotionRule GetPromotionRuleById(int promotionRuleId);

        IPagedList<PromotionRule> GetAllPromotionRules(
            string name = null,
            int? ruleTypeId = null,
            bool? isActive = null,
            int discountId = 0,
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue);

        IList<PromotionRule> GetActiveRules(int storeId = 0);

        IList<PromotionRule> GetPromotionRulesByDiscountId(int discountId);

        IList<PromotionRule> GetSuppressingPromotionRulesByDiscountId(int discountId);

        IList<PromotionRule> GetPromotionRulesByLinkedDiscountId(int linkedDiscountId);

        IList<PromotionRule> GetSuppressingPromotionRulesByLinkedDiscountId(int linkedDiscountId);

        void InsertPromotionRule(PromotionRule promotionRule);

        void UpdatePromotionRule(PromotionRule promotionRule);

        void DeletePromotionRule(PromotionRule promotionRule);

        IList<PromotionRuleProduct> GetRuleProductsByRuleId(int promotionRuleId);

        PromotionRuleProduct GetRuleProductById(int ruleProductId);

        void InsertRuleProduct(PromotionRuleProduct ruleProduct);

        void UpdateRuleProduct(PromotionRuleProduct ruleProduct);

        void DeleteRuleProduct(PromotionRuleProduct ruleProduct);

        IList<PromotionRuleCondition> GetRuleConditionsByRuleId(int promotionRuleId);

        IList<PromotionRuleCondition> GetRuleConditionsByDiscountRequirementId(int discountRequirementId);

        PromotionRuleCondition GetRuleConditionById(int ruleConditionId);

        void InsertRuleCondition(PromotionRuleCondition ruleCondition);

        void UpdateRuleCondition(PromotionRuleCondition ruleCondition);

        void DeleteRuleCondition(PromotionRuleCondition ruleCondition);

        IList<PromotionRuleTier> GetRuleTiersByRuleId(int promotionRuleId);

        PromotionRuleTier GetRuleTierById(int ruleTierId);

        void InsertRuleTier(PromotionRuleTier ruleTier);

        void UpdateRuleTier(PromotionRuleTier ruleTier);

        void DeleteRuleTier(PromotionRuleTier ruleTier);

        IList<PromotionRuleTierProductMapping> GetRuleTierProductMappingsByTierId(int ruleTierId);

        IList<PromotionRuleTierProductMapping> GetRuleTierProductMappingsByRuleId(int promotionRuleId);

        IList<int> GetMappedRuleProductIdsByTierId(int ruleTierId);

        IList<int> GetMappedTierIdsByRuleProducts(int promotionRuleId, IList<int> ruleProductIds);

        void SaveRuleTierProductMappings(int promotionRuleId, int ruleTierId, IList<int> ruleProductIds);

        void InsertRuleUsage(PromotionRuleUsage usage);

        int GetRuleUsageCount(int promotionRuleId, DateTime? fromUtc = null, int? customerId = null);

        void InsertPromotionSocialShareEvent(PromotionSocialShareEvent socialShareEvent);

        PromotionSocialShareEvent GetValidPromotionSocialShareEvent(int customerId, int storeId, int ruleId, string tokenHash);

        void ConsumePromotionSocialShareEvent(PromotionSocialShareEvent socialShareEvent);

        IPagedList<PromotionRuleUsage> GetRuleUsages(
            int? promotionRuleId = null,
            int? orderId = null,
            int pageIndex = 0,
            int pageSize = int.MaxValue);

        IPagedList<PromotionRuleAnalyticsSummary> SearchPromotionRuleAnalytics(
            DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null,
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue);

        PromotionRuleAnalyticsTotals GetPromotionRuleAnalyticsTotals(
            DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null,
            int storeId = 0);
    }
}
