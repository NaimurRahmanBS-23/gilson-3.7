using System.Collections.Generic;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories
{
    public interface IPromotionRuleModelFactory
    {
        PromotionRuleSearchModel PreparePromotionRuleSearchModel(PromotionRuleSearchModel searchModel);

        DiscountDetailsPromotionRulesModel PrepareDiscountDetailsPromotionRulesModel(int discountId);

        IList<PromotionRuleModel> PreparePromotionRuleList(PromotionRuleSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        PromotionRuleModel PreparePromotionRuleModel(
            PromotionRuleModel model,
            PromotionRule promotionRule,
            bool excludeProperties = false);

        IList<PromotionRuleProductModel> PrepareRuleProductList(PromotionRuleProductSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        IList<PromotionRuleTierModel> PrepareRuleTierList(PromotionRuleTierSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        PromotionRuleTierModel PrepareRuleTierModel(PromotionRuleTierModel model, PromotionRuleTier tier, bool excludeProperties = false);

        IList<PromotionRuleConditionModel> PrepareRuleConditionList(PromotionRuleConditionSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        PromotionRuleConditionModel PrepareRuleConditionModel(PromotionRuleConditionModel model, PromotionRuleCondition condition, bool excludeProperties = false);

        PromotionRuleConditionRequirementListModel PrepareRuleConditionRequirementListModel(int promotionRuleId);

        PromotionRuleConditionRequirementListModel PrepareDiscountRequirementConditionListModel(int discountRequirementId);

        IList<PromotionRuleUsageHistoryModel> PrepareRuleUsageHistoryList(PromotionRuleUsageHistorySearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        PromotionRuleRewardAttributePopupModel PrepareRewardAttributePopupModel(int productId, string selectedValueIds);

        IList<PromotionRuleExcludedProductModel> PrepareExcludedProductList(PromotionRuleExcludedProductSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        PromotionRuleSelectProductSearchModel PrepareSelectProductSearchModel(PromotionRuleSelectProductSearchModel searchModel);

        IList<PromotionRuleSelectProductModel> PrepareSelectProductList(PromotionRuleSelectProductSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);
    }
}
