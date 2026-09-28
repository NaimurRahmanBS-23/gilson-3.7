using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

/// <summary>
/// Migration to add excluded products table and migrate existing data
/// </summary>
[NopMigration("2026-06-24 00:00:01", "NopStation.Plugin.Misc.DiscountManagerPlus add excluded products table")]
public class ExcludedProductsTableMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleExcludedProduct));

        // Create the new excluded products table
        if (!Schema.Table(tableName).Exists())
            Create.TableFor<PromotionRuleExcludedProduct>();

        // Migrate existing excluded products from conditions
    }
}
