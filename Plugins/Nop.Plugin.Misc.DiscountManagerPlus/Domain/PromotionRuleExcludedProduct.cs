using System;
using Nop.Core;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class PromotionRuleExcludedProduct : BaseEntity
    {
        public int PromotionRuleId { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
