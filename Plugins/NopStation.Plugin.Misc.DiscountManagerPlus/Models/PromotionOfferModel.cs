using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Models;

public partial record PromotionOfferModel : BaseNopEntityModel
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string RuleTypeName { get; set; } = string.Empty;
    public string DiscountDisplay { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

public partial record PromotionOfferListModel : BaseNopModel
{
    public string Title { get; set; } = string.Empty;
    public string EmptyMessage { get; set; } = string.Empty;
    public IList<PromotionOfferModel> Offers { get; set; } = new List<PromotionOfferModel>();
}
