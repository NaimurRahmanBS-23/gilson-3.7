using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-01 00:00:02", "NopStation.Plugin.Misc.DiscountManagerPlus add discount scope")]
public class DiscountScopeMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (!Schema.Table(tableName).Exists())
            return;

        if (!Schema.Table(tableName).Column(nameof(PromotionRule.DiscountScopeId)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRule.DiscountScopeId)).AsInt32().NotNullable()
                .WithDefaultValue((int)DiscountScope.MatchedItemsOnly);
        }
    }
}
