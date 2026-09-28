using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleProductSearchModel : BaseSearchModel
{
    public int PromotionRuleId { get; set; }
    public bool AllowRewardProduct { get; set; }
}

public partial record PromotionRuleProductListModel : BasePagedListModel<PromotionRuleProductModel>
{
}

public partial record PromotionRuleProductModel : BaseNopEntityModel
{
    public int PromotionRuleId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId")]
    public int ProductId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductName")]
    public string ProductName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceType")]
    public string SourceTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceName")]
    public string SourceName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MinQuantity")]
    public int MinQuantity { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity")]
    public int MaxQuantity { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsAllProducts")]
    public bool IsAllProducts { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsRewardProduct")]
    public bool IsRewardProduct { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeSelectionType")]
    public int RewardAttributeSelectionTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeSelectionType")]
    public string RewardAttributeSelectionTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds")]
    public string RewardAttributeValueIds { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds")]
    public string AttributeFilterSummary { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId")]
    public int? CategoryId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId")]
    public int? ManufacturerId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId")]
    public int? VendorId { get; set; }
}

public partial record PromotionRuleProductAddModel : BaseNopModel
{
    public int Id { get; set; }

    public int PromotionRuleId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceType")]
    public int SourceTypeId { get; set; } = (int)RuleProductSourceType.Product;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId")]
    public int ProductId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductName")]
    public string ProductName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId")]
    public int? CategoryId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId")]
    public int? ManufacturerId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId")]
    public int? VendorId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MinQuantity")]
    public int MinQuantity { get; set; } = 1;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity")]
    public int MaxQuantity { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsAllProducts")]
    public bool IsAllProducts { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsRewardProduct")]
    public bool IsRewardProduct { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeSelectionType")]
    public int RewardAttributeSelectionTypeId { get; set; } = (int)RewardAttributeSelectionType.Any;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds")]
    public string RewardAttributeValueIds { get; set; } = string.Empty;

    public IList<SelectListItem> AvailableProducts { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableSourceTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableManufacturers { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableRewardAttributeSelectionTypes { get; set; } = new List<SelectListItem>();
}

public partial record AddProductsToPromotionRuleModel : BaseNopModel
{
    public AddProductsToPromotionRuleModel()
    {
        SelectedProductIds = new List<int>();
    }

    public int PromotionRuleId { get; set; }

    public IList<int> SelectedProductIds { get; set; }
}
