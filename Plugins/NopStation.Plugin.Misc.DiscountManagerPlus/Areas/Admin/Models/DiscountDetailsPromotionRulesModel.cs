using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record DiscountDetailsPromotionRulesModel : BaseNopModel
{
    public int DiscountId { get; set; }

    public string DiscountName { get; set; } = string.Empty;
    public string ParentDiscountLimitationSummary { get; set; } = string.Empty;
    public string ParentDiscountMaximumDiscountAmountDisplay { get; set; } = string.Empty;
    public string ParentDiscountMaximumDiscountedQuantityDisplay { get; set; } = string.Empty;
    public bool ParentDiscountUsesNativeCustomerLimit { get; set; }

    public bool IsSavedDiscount => DiscountId > 0;

    public PromotionRuleSearchModel SearchModel { get; set; } = new();

    public IList<PromotionRuleModel> Rules { get; set; } = new List<PromotionRuleModel>();
}
