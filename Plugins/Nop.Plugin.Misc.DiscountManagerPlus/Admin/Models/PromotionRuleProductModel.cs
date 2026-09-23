using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleProductSearchModel : BaseNopModel
    {
        public PromotionRuleProductSearchModel()
        {
            PageIndex = 0;
            PageSize = 15;
        }

        public int PromotionRuleId { get; set; }
        public bool AllowRewardProduct { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public partial class PromotionRuleProductModel : BaseNopEntityModel
    {
        public PromotionRuleProductModel()
        {
            ProductName = string.Empty;
            SourceTypeName = string.Empty;
            SourceName = string.Empty;
            RewardAttributeSelectionTypeName = string.Empty;
            RewardAttributeValueIds = string.Empty;
            AttributeFilterSummary = string.Empty;
        }

        public int PromotionRuleId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId")]
        public int ProductId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductName")]
        public string ProductName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceType")]
        public string SourceTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceName")]
        public string SourceName { get; set; }

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
        public string RewardAttributeSelectionTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds")]
        public string RewardAttributeValueIds { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds")]
        public string AttributeFilterSummary { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId")]
        public int? CategoryId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId")]
        public int? ManufacturerId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId")]
        public int? VendorId { get; set; }
    }

    public partial class PromotionRuleProductAddModel : BaseNopModel
    {
        public PromotionRuleProductAddModel()
        {
            SourceTypeId = (int)RuleProductSourceType.Product;
            ProductName = string.Empty;
            MinQuantity = 1;
            RewardAttributeSelectionTypeId = (int)RewardAttributeSelectionType.Any;
            RewardAttributeValueIds = string.Empty;
            AvailableProducts = new List<SelectListItem>();
            AvailableSourceTypes = new List<SelectListItem>();
            AvailableCategories = new List<SelectListItem>();
            AvailableManufacturers = new List<SelectListItem>();
            AvailableVendors = new List<SelectListItem>();
            AvailableRewardAttributeSelectionTypes = new List<SelectListItem>();
        }

        public int Id { get; set; }

        public int PromotionRuleId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceType")]
        public int SourceTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId")]
        public int ProductId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductName")]
        public string ProductName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId")]
        public int? CategoryId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId")]
        public int? ManufacturerId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId")]
        public int? VendorId { get; set; }

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

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds")]
        public string RewardAttributeValueIds { get; set; }

        public IList<SelectListItem> AvailableProducts { get; set; }
        public IList<SelectListItem> AvailableSourceTypes { get; set; }
        public IList<SelectListItem> AvailableCategories { get; set; }
        public IList<SelectListItem> AvailableManufacturers { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
        public IList<SelectListItem> AvailableRewardAttributeSelectionTypes { get; set; }
    }

    public partial class AddProductsToPromotionRuleModel : BaseNopModel
    {
        public AddProductsToPromotionRuleModel()
        {
            SelectedProductIds = new List<int>();
        }

        public int PromotionRuleId { get; set; }

        public IList<int> SelectedProductIds { get; set; }
    }
}
