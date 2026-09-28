using Nop.Web.Models.Catalog;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Models;

public class RewardProductAttributesModel
{
    public ProductDetailsModel Product { get; set; } = new();

    public string FormId { get; set; } = string.Empty;
}
