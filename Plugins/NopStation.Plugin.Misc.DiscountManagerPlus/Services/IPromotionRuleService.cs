using Nop.Core;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IPromotionRuleService
{
    Task<PromotionRule> GetPromotionRuleByIdAsync(int promotionRuleId);

    Task<IPagedList<PromotionRule>> GetAllPromotionRulesAsync(
        string name = null,
        int? ruleTypeId = null,
        bool? isActive = null,
        int discountId = 0,
        int storeId = 0,
        int pageIndex = 0,
        int pageSize = int.MaxValue);

    Task<IList<PromotionRule>> GetActiveRulesAsync(int storeId = 0);

    Task<IList<PromotionRule>> GetPromotionRulesByDiscountIdAsync(int discountId);

    Task<IList<PromotionRule>> GetSuppressingPromotionRulesByDiscountIdAsync(int discountId);

    Task<IList<PromotionRule>> GetPromotionRulesByLinkedDiscountIdAsync(int linkedDiscountId);

    Task<IList<PromotionRule>> GetSuppressingPromotionRulesByLinkedDiscountIdAsync(int linkedDiscountId);

    Task InsertPromotionRuleAsync(PromotionRule promotionRule);

    Task UpdatePromotionRuleAsync(PromotionRule promotionRule);

    Task DeletePromotionRuleAsync(PromotionRule promotionRule);

    Task<IList<PromotionRuleProduct>> GetRuleProductsByRuleIdAsync(int promotionRuleId);

    Task<PromotionRuleProduct> GetRuleProductByIdAsync(int ruleProductId);

    Task InsertRuleProductAsync(PromotionRuleProduct ruleProduct);

    Task UpdateRuleProductAsync(PromotionRuleProduct ruleProduct);

    Task DeleteRuleProductAsync(PromotionRuleProduct ruleProduct);

    Task<IList<PromotionRuleCondition>> GetRuleConditionsByRuleIdAsync(int promotionRuleId);

    Task<IList<PromotionRuleCondition>> GetRuleConditionsByDiscountRequirementIdAsync(int discountRequirementId);

    Task<PromotionRuleCondition> GetRuleConditionByIdAsync(int ruleConditionId);

    Task InsertRuleConditionAsync(PromotionRuleCondition ruleCondition);

    Task UpdateRuleConditionAsync(PromotionRuleCondition ruleCondition);

    Task DeleteRuleConditionAsync(PromotionRuleCondition ruleCondition);

    Task<IList<PromotionRuleTier>> GetRuleTiersByRuleIdAsync(int promotionRuleId);

    Task<PromotionRuleTier> GetRuleTierByIdAsync(int ruleTierId);

    Task InsertRuleTierAsync(PromotionRuleTier ruleTier);

    Task UpdateRuleTierAsync(PromotionRuleTier ruleTier);

    Task DeleteRuleTierAsync(PromotionRuleTier ruleTier);

    Task<IList<PromotionRuleTierProductMapping>> GetRuleTierProductMappingsByTierIdAsync(int ruleTierId);

    Task<IList<PromotionRuleTierProductMapping>> GetRuleTierProductMappingsByRuleIdAsync(int promotionRuleId);

    Task<IList<int>> GetMappedRuleProductIdsByTierIdAsync(int ruleTierId);

    Task<IList<int>> GetMappedTierIdsByRuleProductsAsync(int promotionRuleId, IList<int> ruleProductIds);

    Task SaveRuleTierProductMappingsAsync(int promotionRuleId, int ruleTierId, IList<int> ruleProductIds);

    Task InsertRuleUsageAsync(PromotionRuleUsage usage);

    Task<int> GetRuleUsageCountAsync(int promotionRuleId, DateTime? fromUtc = null, int? customerId = null);

    Task InsertPromotionSocialShareEventAsync(PromotionSocialShareEvent socialShareEvent);

    Task<PromotionSocialShareEvent> GetValidPromotionSocialShareEventAsync(int customerId, int storeId, int ruleId, string tokenHash);

    Task ConsumePromotionSocialShareEventAsync(PromotionSocialShareEvent socialShareEvent);

    Task<IPagedList<PromotionRuleUsage>> GetRuleUsagesAsync(
        int? promotionRuleId = null,
        int? orderId = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue);

    Task<IPagedList<PromotionRuleAnalyticsSummary>> SearchPromotionRuleAnalyticsAsync(
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        int storeId = 0,
        int pageIndex = 0,
        int pageSize = int.MaxValue);

    Task<PromotionRuleAnalyticsTotals> GetPromotionRuleAnalyticsTotalsAsync(
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        int storeId = 0);
}
