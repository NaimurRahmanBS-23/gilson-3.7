using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-15 18:40:00", "DiscountManagerPlus rule store mapping support")]
public class RuleStoreMappingMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Column(nameof(PromotionRule.LimitedToStores)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.LimitedToStores)).AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
