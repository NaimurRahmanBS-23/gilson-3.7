using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleUsageHistorySearchModel : BaseNopModel
    {
        public PromotionRuleUsageHistorySearchModel()
        {
            ParentDiscountName = string.Empty;
            PageIndex = 0;
            PageSize = 15;
        }

        public int PromotionRuleId { get; set; }
        public bool UseParentDiscountHistory { get; set; }
        public string ParentDiscountName { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
