using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-04-21 10:10:00", "NopStation.Plugin.Misc.DiscountManagerPlus add stacked cumulative mode field")]
public class StackedCumulativeModeMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRule.EnableStackedCumulativeMode)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.EnableStackedCumulativeMode)).AsBoolean().NotNullable()
                .WithDefaultValue(false);
        }
    }
}
