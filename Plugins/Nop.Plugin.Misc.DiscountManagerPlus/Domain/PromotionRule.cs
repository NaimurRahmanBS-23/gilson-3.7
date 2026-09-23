using System;
using Nop.Core;
using Nop.Core.Domain.Stores;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class PromotionRule : BaseEntity, IStoreMappingSupported
    {
        public int DiscountId { get; set; }
        public string Name { get; set; }
        public string SystemName { get; set; }
        public int RuleTypeId { get; set; }
        public int DiscountTypeId { get; set; }
        public int DiscountScopeId { get; set; }
        public decimal DiscountValue { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public bool IsExclusive { get; set; }
        public int? LinkedDiscountId { get; set; }
        public bool CarryDefaultDiscount { get; set; }
        public bool IsCumulativeWithDefaultDiscounts { get; set; }
        public bool StopFurtherRulesForMatchedLines { get; set; }
        public int UsageLimitTotal { get; set; }
        public int UsageLimitPerCustomer { get; set; }
        public DateTime? UsageWindowStartUtc { get; set; }
        public DateTime? UsageWindowEndUtc { get; set; }
        public bool IsFlashEnabled { get; set; }
        public DateTime? StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }
        public bool LimitedToStores { get; set; }
        public int LimitedToStore { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public int DiscountTargetTypeId { get; set; }
        public bool EnableAutoUpgrade { get; set; }
        public bool EnableStackedCumulativeMode { get; set; }

        public PromotionRuleType RuleType
        {
            get { return (PromotionRuleType)RuleTypeId; }
            set { RuleTypeId = (int)value; }
        }

        public DiscountType DiscountType
        {
            get { return (DiscountType)DiscountTypeId; }
            set { DiscountTypeId = (int)value; }
        }

        public DiscountScope DiscountScope
        {
            get { return (DiscountScope)DiscountScopeId; }
            set { DiscountScopeId = (int)value; }
        }

        public DiscountTargetType DiscountTargetType
        {
            get { return (DiscountTargetType)DiscountTargetTypeId; }
            set { DiscountTargetTypeId = (int)value; }
        }
    }
}
