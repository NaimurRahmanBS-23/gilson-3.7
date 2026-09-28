using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleSearchModel : BaseSearchModel
{
    public int DiscountId { get; set; }

    public bool HideFilters { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.Name")]
    public string SearchName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.RuleType")]
    public int SearchRuleTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.IsActive")]
    public bool? SearchIsActive { get; set; }

    public IList<SelectListItem> AvailableRuleTypes { get; set; } = new List<SelectListItem>();
}
