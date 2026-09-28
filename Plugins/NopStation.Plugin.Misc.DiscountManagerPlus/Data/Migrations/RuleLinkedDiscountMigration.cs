using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-13 15:10:00", "NopStation.Plugin.Misc.DiscountManagerPlus add linked discount fields")]
public class RuleLinkedDiscountMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRule.LinkedDiscountId)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.LinkedDiscountId)).AsInt32().Nullable();
        }

        if (!Schema.Table(tableName).Column(nameof(PromotionRule.CarryDefaultDiscount)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.CarryDefaultDiscount)).AsBoolean().NotNullable()
                .WithDefaultValue(false);
        }
    }
}
