using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleConditionProductSelectorModel : BaseNopModel
    {
        public int AssociatedToProductId { get; set; }
    }

    public partial class PromotionRuleSelectProductSearchModel : BaseNopModel
    {
        public PromotionRuleSelectProductSearchModel()
        {
            AddProductModel = new PromotionRuleConditionProductSelectorModel();
            SearchProductName = string.Empty;
            AvailableCategories = new List<SelectListItem>();
            AvailableManufacturers = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
            AvailableVendors = new List<SelectListItem>();
            AvailableProductTypes = new List<SelectListItem>();
            PageIndex = 0;
            PageSize = 15;
        }

        public PromotionRuleConditionProductSelectorModel AddProductModel { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
        [AllowHtml]
        public string SearchProductName { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
        public int SearchCategoryId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchManufacturer")]
        public int SearchManufacturerId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
        public int SearchStoreId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
        public int SearchVendorId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
        public int SearchProductTypeId { get; set; }

        public IList<SelectListItem> AvailableCategories { get; set; }
        public IList<SelectListItem> AvailableManufacturers { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
        public IList<SelectListItem> AvailableProductTypes { get; set; }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public partial class PromotionRuleSelectProductModel : BaseNopEntityModel
    {
        public PromotionRuleSelectProductModel()
        {
            Name = string.Empty;
        }

        public string Name { get; set; }
        public bool Published { get; set; }
    }
}
