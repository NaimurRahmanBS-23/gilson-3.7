using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleTierSearchModel : BaseSearchModel
{
    public int PromotionRuleId { get; set; }
}

public partial record PromotionRuleTierListModel : BasePagedListModel<PromotionRuleTierModel>
{
}

public partial record PromotionRuleTierModel : BaseNopEntityModel
{
    public int PromotionRuleId { get; set; }
    public int RuleTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MinQuantity")]
    public int MinQuantity { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity")]
    public int MaxQuantity { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardQuantity")]
    public int RewardQuantity { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType")]
    public int DiscountTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType")]
    public string DiscountTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountValue")]
    public decimal DiscountValue { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AutoAddReward")]
    public bool AutoAddReward { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct")]
    public int? RewardProductId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct")]
    public string RewardProductName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts")]
    public IList<int> SelectedRuleProductIds { get; set; } = new List<int>();

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts")]
    public string AppliesToRuleProductsSummary { get; set; } = string.Empty;

    public IList<SelectListItem> AvailableDiscountTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableProducts { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableRuleProducts { get; set; } = new List<SelectListItem>();
}
