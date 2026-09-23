using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleSearchModel : BaseNopModel
    {
        public PromotionRuleSearchModel()
        {
            AvailableRuleTypes = new List<SelectListItem>();
            SearchName = string.Empty;
            PageIndex = 0;
            PageSize = 15;
        }

        public int DiscountId { get; set; }

        public bool HideFilters { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.Name")]
        [AllowHtml]
        public string SearchName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.RuleType")]
        public int SearchRuleTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.IsActive")]
        public bool? SearchIsActive { get; set; }

        public IList<SelectListItem> AvailableRuleTypes { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }
    }
}
