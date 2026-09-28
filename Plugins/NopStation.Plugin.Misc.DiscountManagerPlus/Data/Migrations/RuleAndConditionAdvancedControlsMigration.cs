using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using NopStation.Plugin.Misc.Core.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Data.Migrations;

[NopMigration("2026-03-05 00:00:01", "NopStation.Plugin.Misc.DiscountManagerPlus add advanced rule and condition controls")]
public class RuleAndConditionAdvancedControlsMigration : AutoReversingMigration
{
    public override void Up()
    {
        var ruleTableName = NameCompatibilityManager.GetTableName(typeof(PromotionRule));
        if (Schema.Table(ruleTableName).Exists())
        {
            if (!Schema.Table(ruleTableName).Column(nameof(PromotionRule.StopFurtherRulesForMatchedLines)).Exists())
            {
                Alter.Table(ruleTableName)
                    .AddColumn(nameof(PromotionRule.StopFurtherRulesForMatchedLines)).AsBoolean().NotNullable()
                    .WithDefaultValue(false);
            }

            if (!Schema.Table(ruleTableName).Column(nameof(PromotionRule.UsageLimitTotal)).Exists())
            {
                Alter.Table(ruleTableName)
                    .AddColumn(nameof(PromotionRule.UsageLimitTotal)).AsInt32().NotNullable()
                    .WithDefaultValue(0);
            }

            if (!Schema.Table(ruleTableName).Column(nameof(PromotionRule.UsageLimitPerCustomer)).Exists())
            {
                Alter.Table(ruleTableName)
                    .AddColumn(nameof(PromotionRule.UsageLimitPerCustomer)).AsInt32().NotNullable()
                    .WithDefaultValue(0);
            }

            if (!Schema.Table(ruleTableName).Column(nameof(PromotionRule.UsageWindowStartUtc)).Exists())
            {
                Alter.Table(ruleTableName)
                    .AddColumn(nameof(PromotionRule.UsageWindowStartUtc)).AsDateTime2().Nullable();
            }

            if (!Schema.Table(ruleTableName).Column(nameof(PromotionRule.UsageWindowEndUtc)).Exists())
            {
                Alter.Table(ruleTableName)
                    .AddColumn(nameof(PromotionRule.UsageWindowEndUtc)).AsDateTime2().Nullable();
            }

            if (!Schema.Table(ruleTableName).Column(nameof(PromotionRule.IsFlashEnabled)).Exists())
            {
                Alter.Table(ruleTableName)
                    .AddColumn(nameof(PromotionRule.IsFlashEnabled)).AsBoolean().NotNullable()
                    .WithDefaultValue(false);
            }
        }

        var conditionTableName = NameCompatibilityManager.GetTableName(typeof(PromotionRuleCondition));
        if (Schema.Table(conditionTableName).Exists())
        {
            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.RequiredCountryCodesCsv)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.RequiredCountryCodesCsv)).AsString(2000).Nullable();
            }

            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.RequiredPaymentMethodsCsv)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.RequiredPaymentMethodsCsv)).AsString(2000).Nullable();
            }

            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.RequiredCouponCodesCsv)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.RequiredCouponCodesCsv)).AsString(2000).Nullable();
            }

            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.RequiredOrderCountMin)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.RequiredOrderCountMin)).AsInt32().Nullable();
            }

            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.RequiredOrderCountMax)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.RequiredOrderCountMax)).AsInt32().Nullable();
            }

            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.RequireSameLineMatch)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.RequireSameLineMatch)).AsBoolean().NotNullable()
                    .WithDefaultValue(false);
            }

            if (!Schema.Table(conditionTableName).Column(nameof(PromotionRuleCondition.AttributeMatchModeId)).Exists())
            {
                Alter.Table(conditionTableName)
                    .AddColumn(nameof(PromotionRuleCondition.AttributeMatchModeId)).AsInt32().NotNullable()
                    .WithDefaultValue((int)AttributeMatchMode.Any);
            }
        }

        if (!Schema.Table<PromotionSocialShareEvent>().Exists())
            Create.TableFor<PromotionSocialShareEvent>();
    }
}
