using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Builders;

public class PromotionRuleConditionBuilder : NopEntityBuilder<PromotionRuleCondition>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(PromotionRuleCondition.PromotionRuleId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleCondition.DiscountRequirementId)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRuleCondition.ConditionGroup)).AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn(nameof(PromotionRuleCondition.ParentConditionId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.LogicalOperatorId)).AsInt32().NotNullable().WithDefaultValue((int)ConditionLogicalOperator.And)
            .WithColumn(nameof(PromotionRuleCondition.ConditionOperatorId)).AsInt32().NotNullable()
            .WithColumn(nameof(PromotionRuleCondition.MinValue)).AsDecimal(18, 4).NotNullable()
            .WithColumn(nameof(PromotionRuleCondition.MaxValue)).AsDecimal(18, 4).NotNullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredProductId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.ExcludedProductId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredCategoryId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredVendorId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredCustomerRoleId)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.IsFirstOrderOnly)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRuleCondition.IsNewCustomerOnly)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRuleCondition.ConditionRestrictionTypeId)).AsInt32().NotNullable().WithDefaultValue((int)ConditionRestrictionType.Include)
            .WithColumn(nameof(PromotionRuleCondition.ConditionSourceTypeId)).AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn(nameof(PromotionRuleCondition.ConditionSourceData)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(PromotionRuleCondition.QuantityMin)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.QuantityMax)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredCountryCodesCsv)).AsString(2000).Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredPaymentMethodsCsv)).AsString(2000).Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredCouponCodesCsv)).AsString(2000).Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredOrderCountMin)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequiredOrderCountMax)).AsInt32().Nullable()
            .WithColumn(nameof(PromotionRuleCondition.RequireSameLineMatch)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(PromotionRuleCondition.AttributeMatchModeId)).AsInt32().NotNullable().WithDefaultValue((int)AttributeMatchMode.Any);
    }

    #endregion
}
