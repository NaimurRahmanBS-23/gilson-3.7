using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public class PromotionRuleCondition : BaseEntity
{
    public int PromotionRuleId { get; set; }
    public int DiscountRequirementId { get; set; }
    public int ConditionGroup { get; set; }
    public int? ParentConditionId { get; set; }
    public int LogicalOperatorId { get; set; }
    public int ConditionOperatorId { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int? RequiredProductId { get; set; }
    public int? ExcludedProductId { get; set; }
    public int? RequiredCategoryId { get; set; }
    public int? RequiredVendorId { get; set; }
    public int? RequiredCustomerRoleId { get; set; }
    public bool IsFirstOrderOnly { get; set; }
    public bool IsNewCustomerOnly { get; set; }
    public int ConditionRestrictionTypeId { get; set; }
    public int ConditionSourceTypeId { get; set; }
    public string ConditionSourceData { get; set; } = string.Empty;
    public int? QuantityMin { get; set; }
    public int? QuantityMax { get; set; }
    public string RequiredCountryCodesCsv { get; set; } = string.Empty;
    public string RequiredPaymentMethodsCsv { get; set; } = string.Empty;
    public string RequiredCouponCodesCsv { get; set; } = string.Empty;
    public int? RequiredOrderCountMin { get; set; }
    public int? RequiredOrderCountMax { get; set; }
    public bool RequireSameLineMatch { get; set; }
    public int AttributeMatchModeId { get; set; }

    public ConditionOperator ConditionOperator
    {
        get => (ConditionOperator)ConditionOperatorId;
        set => ConditionOperatorId = (int)value;
    }

    public ConditionLogicalOperator LogicalOperator
    {
        get => (ConditionLogicalOperator)LogicalOperatorId;
        set => LogicalOperatorId = (int)value;
    }

    public ConditionRestrictionType ConditionRestrictionType
    {
        get => (ConditionRestrictionType)ConditionRestrictionTypeId;
        set => ConditionRestrictionTypeId = (int)value;
    }

    public ConditionSourceType ConditionSourceType
    {
        get => (ConditionSourceType)ConditionSourceTypeId;
        set => ConditionSourceTypeId = (int)value;
    }

    public AttributeMatchMode AttributeMatchMode
    {
        get => (AttributeMatchMode)AttributeMatchModeId;
        set => AttributeMatchModeId = (int)value;
    }
}
