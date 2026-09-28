using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-04 00:00:03", "NopStation.Plugin.Misc.DiscountManagerPlus add cumulative with default discount flag")]
public class RuleCumulativeWithDefaultDiscountMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRule.IsCumulativeWithDefaultDiscounts)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.IsCumulativeWithDefaultDiscounts)).AsBoolean().NotNullable()
                .WithDefaultValue(false);
        }
    }
}
