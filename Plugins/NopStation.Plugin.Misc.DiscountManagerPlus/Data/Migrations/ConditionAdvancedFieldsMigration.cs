using FluentMigrator;
using Nop.Data.Migrations;
using Nop.Data.Mapping;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-02-28 00:00:01", "NopStation.Plugin.Misc.DiscountManagerPlus add advanced condition fields")]
public class ConditionAdvancedFieldsMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleCondition));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.ConditionGroup)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.ConditionGroup)).AsInt32().NotNullable().WithDefaultValue(1);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.LogicalOperatorId)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.LogicalOperatorId)).AsInt32().NotNullable().WithDefaultValue((int)ConditionLogicalOperator.And);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.RequiredVendorId)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.RequiredVendorId)).AsInt32().Nullable();

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.RequiredCustomerRoleId)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.RequiredCustomerRoleId)).AsInt32().Nullable();

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.IsFirstOrderOnly)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.IsFirstOrderOnly)).AsBoolean().NotNullable().WithDefaultValue(false);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.IsNewCustomerOnly)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.IsNewCustomerOnly)).AsBoolean().NotNullable().WithDefaultValue(false);
    }
}
