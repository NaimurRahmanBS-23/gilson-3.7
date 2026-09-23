using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Models
{
    public class RewardProductAttributesModel
    {
        public RewardProductAttributesModel()
        {
            Product = new ProductDetailsModel();
        }

        public ProductDetailsModel Product { get; set; }
        public string FormId { get; set; }
    }
}
