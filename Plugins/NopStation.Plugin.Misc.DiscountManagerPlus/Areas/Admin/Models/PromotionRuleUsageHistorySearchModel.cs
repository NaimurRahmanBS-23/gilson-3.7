using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleUsageHistorySearchModel : BaseSearchModel
{
    public int PromotionRuleId { get; set; }
    public bool UseParentDiscountHistory { get; set; }
    public string ParentDiscountName { get; set; } = string.Empty;
}
