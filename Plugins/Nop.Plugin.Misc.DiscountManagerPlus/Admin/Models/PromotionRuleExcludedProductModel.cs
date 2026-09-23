using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleExcludedProductSearchModel : BaseNopModel
    {
        public PromotionRuleExcludedProductSearchModel()
        {
            PageIndex = 0;
            PageSize = 15;
        }

        public int PromotionRuleId { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public partial class PromotionRuleExcludedProductModel : BaseNopEntityModel
    {
        public PromotionRuleExcludedProductModel()
        {
            ProductName = string.Empty;
            Sku = string.Empty;
            ProductPictureUrl = string.Empty;
        }

        public int PromotionRuleId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductId")]
        public int ProductId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductName")]
        public string ProductName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Sku")]
        public string Sku { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Price")]
        public decimal Price { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductPictureUrl")]
        public string ProductPictureUrl { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.CreatedOn")]
        public DateTime CreatedOnUtc { get; set; }
    }

    public partial class AddExcludedProductsToPromotionRuleModel : BaseNopModel
    {
        public AddExcludedProductsToPromotionRuleModel()
        {
            SelectedProductIds = new List<int>();
            AvailableProducts = new List<SelectListItem>();
        }

        public int PromotionRuleId { get; set; }

        public IList<int> SelectedProductIds { get; set; }

        public IList<SelectListItem> AvailableProducts { get; set; }
    }
}
