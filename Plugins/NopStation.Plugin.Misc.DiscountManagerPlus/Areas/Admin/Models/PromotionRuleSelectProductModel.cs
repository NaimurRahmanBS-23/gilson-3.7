using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleConditionProductSelectorModel : BaseNopModel
{
    public int AssociatedToProductId { get; set; }
}

public partial record PromotionRuleSelectProductSearchModel : BaseSearchModel
{
    public PromotionRuleSelectProductSearchModel()
    {
        AddProductModel = new PromotionRuleConditionProductSelectorModel();
    }

    public PromotionRuleConditionProductSelectorModel AddProductModel { get; set; } = new();

    [NopResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
    public string SearchProductName { get; set; } = string.Empty;
}

public partial record PromotionRuleSelectProductListModel : BasePagedListModel<PromotionRuleSelectProductModel>
{
}

public partial record PromotionRuleSelectProductModel : BaseNopEntityModel
{
    public string Name { get; set; } = string.Empty;
    public bool Published { get; set; }
}
