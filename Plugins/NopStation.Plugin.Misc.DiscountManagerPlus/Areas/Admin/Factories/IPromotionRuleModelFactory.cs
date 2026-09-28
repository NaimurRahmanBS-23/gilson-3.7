using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;

public interface IPromotionRuleModelFactory
{
    Task<PromotionRuleSearchModel> PreparePromotionRuleSearchModelAsync(PromotionRuleSearchModel searchModel);

    Task<DiscountDetailsPromotionRulesModel> PrepareDiscountDetailsPromotionRulesModelAsync(int discountId);

    Task<PromotionRuleListModel> PreparePromotionRuleListModelAsync(PromotionRuleSearchModel searchModel);

    Task<PromotionRuleModel> PreparePromotionRuleModelAsync(
        PromotionRuleModel model,
        PromotionRule promotionRule,
        bool excludeProperties = false);

    Task<PromotionRuleProductListModel> PrepareRuleProductListModelAsync(PromotionRuleProductSearchModel searchModel);

    Task<PromotionRuleTierListModel> PrepareRuleTierListModelAsync(PromotionRuleTierSearchModel searchModel);

    Task<PromotionRuleTierModel> PrepareRuleTierModelAsync(PromotionRuleTierModel model, PromotionRuleTier tier, bool excludeProperties = false);

    Task<PromotionRuleConditionListModel> PrepareRuleConditionListModelAsync(PromotionRuleConditionSearchModel searchModel);

    Task<PromotionRuleConditionModel> PrepareRuleConditionModelAsync(PromotionRuleConditionModel model, PromotionRuleCondition condition, bool excludeProperties = false);

    Task<PromotionRuleConditionRequirementListModel> PrepareRuleConditionRequirementListModelAsync(int promotionRuleId);

    Task<PromotionRuleConditionRequirementListModel> PrepareDiscountRequirementConditionListModelAsync(int discountRequirementId);

    Task<PromotionRuleUsageHistoryListModel> PrepareRuleUsageHistoryListModelAsync(PromotionRuleUsageHistorySearchModel searchModel);

    Task<PromotionRuleRewardAttributePopupModel> PrepareRewardAttributePopupModelAsync(int productId, string selectedValueIds);

    Task<PromotionRuleExcludedProductListModel> PrepareExcludedProductListModelAsync(PromotionRuleExcludedProductSearchModel searchModel);
}
