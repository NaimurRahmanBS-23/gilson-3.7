using System;
using Nop.Core;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class PromotionRuleUsage : BaseEntity
    {
        public int PromotionRuleId { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal DiscountAmountApplied { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
