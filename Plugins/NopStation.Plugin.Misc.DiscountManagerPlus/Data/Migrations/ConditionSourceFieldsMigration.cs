using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-03 00:00:01", "NopStation.Plugin.Misc.DiscountManagerPlus add condition source fields")]
public class ConditionSourceFieldsMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleCondition));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.ConditionRestrictionTypeId)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.ConditionRestrictionTypeId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue((int)ConditionRestrictionType.Include);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.ConditionSourceTypeId)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.ConditionSourceTypeId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.ConditionSourceData)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.ConditionSourceData))
                .AsString(int.MaxValue)
                .Nullable();

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.QuantityMin)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.QuantityMin))
                .AsInt32()
                .Nullable();

        if (!Schema.Table(tableName).Column(nameof(PromotionRuleCondition.QuantityMax)).Exists())
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleCondition.QuantityMax))
                .AsInt32()
                .Nullable();
    }
}
