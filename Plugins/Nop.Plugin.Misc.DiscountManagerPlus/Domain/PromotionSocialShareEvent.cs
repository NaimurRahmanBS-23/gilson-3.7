using System;
using Nop.Core;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class PromotionSocialShareEvent : BaseEntity
    {
        public int CustomerId { get; set; }
        public int StoreId { get; set; }
        public int RuleId { get; set; }
        public string TokenHash { get; set; }
        public string Channel { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public bool IsConsumed { get; set; }
    }
}
