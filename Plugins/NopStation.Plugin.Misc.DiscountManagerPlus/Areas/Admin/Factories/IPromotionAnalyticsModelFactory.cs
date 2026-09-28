using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;

public interface IPromotionAnalyticsModelFactory
{
    Task<PromotionRuleAnalyticsSearchModel> PreparePromotionRuleAnalyticsSearchModelAsync(PromotionRuleAnalyticsSearchModel searchModel);

    Task<PromotionRuleAnalyticsListModel> PreparePromotionRuleAnalyticsListModelAsync(PromotionRuleAnalyticsSearchModel searchModel);

    Task<PromotionRuleAnalyticsSummaryModel> PreparePromotionRuleAnalyticsSummaryModelAsync(PromotionRuleAnalyticsSearchModel searchModel);
}
