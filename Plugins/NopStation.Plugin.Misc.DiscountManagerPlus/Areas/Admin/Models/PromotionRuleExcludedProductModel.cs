using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

/// <summary>
/// Represents model for excluded products list
/// </summary>
public partial record PromotionRuleExcludedProductListModel : BasePagedListModel<PromotionRuleExcludedProductModel>
{
}

/// <summary>
/// Represents model for searching excluded products
/// </summary>
public partial record PromotionRuleExcludedProductSearchModel : BaseSearchModel
{
    public int PromotionRuleId { get; set; }
}

/// <summary>
/// Represents model for a single excluded product
/// </summary>
public partial record PromotionRuleExcludedProductModel : BaseNopEntityModel
{
    public int PromotionRuleId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductId")]
    public int ProductId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductName")]
    public string ProductName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Sku")]
    public string Sku { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Price")]
    public decimal Price { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductPictureUrl")]
    public string ProductPictureUrl { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.CreatedOn")]
    public DateTime CreatedOnUtc { get; set; }
}

/// <summary>
/// Represents model for adding excluded products to a promotion rule
/// </summary>
public partial record AddExcludedProductsToPromotionRuleModel : BaseNopModel
{
    public AddExcludedProductsToPromotionRuleModel()
    {
        SelectedProductIds = new List<int>();
    }

    public int PromotionRuleId { get; set; }

    public IList<int> SelectedProductIds { get; set; }

    public IList<SelectListItem> AvailableProducts { get; set; } = new List<SelectListItem>();
}
