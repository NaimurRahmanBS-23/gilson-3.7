using Nop.Core;
using Nop.Core.Domain.Stores;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionRule : BaseEntity, IStoreMappingSupported
{
    public int DiscountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
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

    /// <summary>
    /// Where should discount be applied?
    /// 0 = SameProduct (default), 1 = CheapestInCategory, 2 = CheapestInCart
    /// </summary>
    public int DiscountTargetTypeId { get; set; } = 0;

    /// <summary>
    /// Enable auto-upgrade logic for this rule (compares with other rules)
    /// </summary>
    public bool EnableAutoUpgrade { get; set; } = true;

    public bool EnableStackedCumulativeMode { get; set; }

    public PromotionRuleType RuleType
    {
        get => (PromotionRuleType)RuleTypeId;
        set => RuleTypeId = (int)value;
    }

    public DiscountType DiscountType
    {
        get => (DiscountType)DiscountTypeId;
        set => DiscountTypeId = (int)value;
    }

    public DiscountScope DiscountScope
    {
        get => (DiscountScope)DiscountScopeId;
        set => DiscountScopeId = (int)value;
    }

    public DiscountTargetType DiscountTargetType
    {
        get => (DiscountTargetType)DiscountTargetTypeId;
        set => DiscountTargetTypeId = (int)value;
    }
}
