using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleAnalyticsSearchModel : BaseSearchModel
{
    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Search.CreatedFrom")]
    [UIHint("DateTimeNullable")]
    public DateTime SearchCreatedFrom { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Search.CreatedTo")]
    [UIHint("DateTimeNullable")]
    public DateTime SearchCreatedTo { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Search.Store")]
    public int SearchStoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();
}
