using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleAnalyticsSearchModel : BaseNopModel
    {
        public PromotionRuleAnalyticsSearchModel()
        {
            AvailableStores = new List<SelectListItem>();
            PageIndex = 0;
            PageSize = 15;
        }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Search.CreatedFrom")]
        [UIHint("DateTimeNullable")]
        public DateTime SearchCreatedFrom { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Search.CreatedTo")]
        [UIHint("DateTimeNullable")]
        public DateTime SearchCreatedTo { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Analytics.Search.Store")]
        public int SearchStoreId { get; set; }

        public IList<SelectListItem> AvailableStores { get; set; }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
