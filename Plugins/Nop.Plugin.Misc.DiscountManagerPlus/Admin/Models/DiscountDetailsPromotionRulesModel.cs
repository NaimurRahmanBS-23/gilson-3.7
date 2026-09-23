using System.Collections.Generic;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class DiscountDetailsPromotionRulesModel : BaseNopModel
    {
        public DiscountDetailsPromotionRulesModel()
        {
            DiscountName = string.Empty;
            ParentDiscountLimitationSummary = string.Empty;
            ParentDiscountMaximumDiscountAmountDisplay = string.Empty;
            ParentDiscountMaximumDiscountedQuantityDisplay = string.Empty;
            SearchModel = new PromotionRuleSearchModel();
            Rules = new List<PromotionRuleModel>();
        }

        public int DiscountId { get; set; }

        public string DiscountName { get; set; }
        public string ParentDiscountLimitationSummary { get; set; }
        public string ParentDiscountMaximumDiscountAmountDisplay { get; set; }
        public string ParentDiscountMaximumDiscountedQuantityDisplay { get; set; }
        public bool ParentDiscountUsesNativeCustomerLimit { get; set; }

        public bool IsSavedDiscount
        {
            get { return DiscountId > 0; }
        }

        public PromotionRuleSearchModel SearchModel { get; set; }

        public IList<PromotionRuleModel> Rules { get; set; }
    }
}
