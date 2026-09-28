using FluentMigrator;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-04-10 00:00:01", "NopStation.Plugin.Misc.DiscountManagerPlus add IsAllProducts to rule product")]
public class RuleProductAllProductsMigration : AutoReversingMigration
{
    #region Methods

    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleProduct));
        if (!Schema.Table(tableName).Column(nameof(PromotionRuleProduct.IsAllProducts)).Exists())
        {
            Alter.Table(tableName)
                .AddColumn(nameof(PromotionRuleProduct.IsAllProducts))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);
        }
    }

    #endregion
}
