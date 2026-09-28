using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-04-16 14:30:00", "NopStation.Plugin.Misc.DiscountManagerPlus add auto-upgrade and discount target type fields")]
public class AutoUpgradeDiscountTargetTypeMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Exists())
            return;

        // Add DiscountTargetTypeId column (for Feature 2: Cheapest-in-Category support)
        if (!Schema.Table(tableName).Column(nameof(PromotionRule.DiscountTargetTypeId)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.DiscountTargetTypeId)).AsInt32().NotNullable()
                .WithDefaultValue(0); // SameProduct default
        }

        // Add EnableAutoUpgrade column (for Feature 1: Auto-upgrade logic)
        if (!Schema.Table(tableName).Column(nameof(PromotionRule.EnableAutoUpgrade)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.EnableAutoUpgrade)).AsBoolean().NotNullable()
                .WithDefaultValue(true); // Enabled by default
        }
    }
}
