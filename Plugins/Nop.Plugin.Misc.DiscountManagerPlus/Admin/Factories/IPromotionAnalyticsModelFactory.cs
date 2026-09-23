using System.Collections.Generic;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories
{
    public interface IPromotionAnalyticsModelFactory
    {
        PromotionRuleAnalyticsSearchModel PreparePromotionRuleAnalyticsSearchModel(PromotionRuleAnalyticsSearchModel searchModel);

        IList<PromotionRuleAnalyticsModel> PreparePromotionRuleAnalyticsList(PromotionRuleAnalyticsSearchModel searchModel, int pageIndex, int pageSize, out int totalCount);

        PromotionRuleAnalyticsSummaryModel PreparePromotionRuleAnalyticsSummaryModel(PromotionRuleAnalyticsSearchModel searchModel);
    }
}
