using System.Collections.Generic;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Models
{
    public partial class PromotionOfferModel : BaseNopEntityModel
    {
        public string Name { get; set; }
        public string Summary { get; set; }
        public string RuleTypeName { get; set; }
        public string DiscountDisplay { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }

    public partial class PromotionOfferListModel : BaseNopModel
    {
        public PromotionOfferListModel()
        {
            Offers = new List<PromotionOfferModel>();
        }

        public string Title { get; set; }
        public string EmptyMessage { get; set; }
        public IList<PromotionOfferModel> Offers { get; set; }
    }
}
