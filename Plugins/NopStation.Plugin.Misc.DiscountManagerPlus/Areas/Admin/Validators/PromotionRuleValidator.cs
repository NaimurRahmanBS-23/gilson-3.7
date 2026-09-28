using FluentValidation;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Validators;

public class PromotionRuleValidator : BaseNopValidator<PromotionRuleModel>
{
    private static bool UsesStandaloneUsageControls(PromotionRuleModel model)
    {
        return model != null && model.DiscountId <= 0 && !model.IsDiscountBound;
    }

    private static bool IsDiscountTypeAllowedForRuleType(int ruleTypeId, int discountTypeId)
    {
        if (discountTypeId <= 0)
            return false;

        var ruleType = (PromotionRuleType)ruleTypeId;
        var discountType = (DiscountType)discountTypeId;

        return ruleType switch
        {
            PromotionRuleType.ProductBased => discountType is DiscountType.Percentage or DiscountType.FixedAmount or DiscountType.FreeItem,
            PromotionRuleType.ComboPricing => discountType is DiscountType.Percentage or DiscountType.FixedAmount or DiscountType.FixedBundlePrice,
            PromotionRuleType.CartCondition => discountType is DiscountType.Percentage or DiscountType.FixedAmount,
            PromotionRuleType.SubtotalBased => discountType is DiscountType.Percentage or DiscountType.FixedAmount,
            _ => false
        };
    }

    public PromotionRuleValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.Required"))
            .MaximumLength(400)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.MaxLength"));

        RuleFor(x => x.SystemName)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.Required"))
            .MaximumLength(400)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.MaxLength"));

        RuleFor(x => x.RuleTypeId)
            .GreaterThan(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType.Required"));

        RuleFor(x => x.DiscountTypeId)
            .GreaterThan(0)
            .When(x => x.RuleTypeId != (int)PromotionRuleType.BuyXGetY)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.Required"));

        RuleFor(x => x.DiscountScopeId)
            .Must(x => Enum.IsDefined(typeof(DiscountScope), x))
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope.Required"));

        RuleFor(x => x)
            .Must(x => IsDiscountTypeAllowedForRuleType(x.RuleTypeId, x.DiscountTypeId))
            .When(x => x.RuleTypeId != (int)PromotionRuleType.BuyXGetY)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.InvalidForRuleType"));

        RuleFor(x => x.DiscountValue)
            .GreaterThanOrEqualTo(0)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountValue.Invalid"));

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EndDate.InvalidRange"));

        RuleFor(x => x.UsageLimitTotal)
            .GreaterThanOrEqualTo(0)
            .When(UsesStandaloneUsageControls)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitTotal.Invalid"));

        RuleFor(x => x.UsageLimitPerCustomer)
            .GreaterThanOrEqualTo(0)
            .When(UsesStandaloneUsageControls)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.Invalid"));

        RuleFor(x => x)
            .Must(x => !x.UsageWindowStartUtc.HasValue || !x.UsageWindowEndUtc.HasValue || x.UsageWindowEndUtc.Value >= x.UsageWindowStartUtc.Value)
            .When(UsesStandaloneUsageControls)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowEndUtc.InvalidRange"));

        RuleFor(x => x)
            .Must(x => x.UsageLimitTotal <= 0 || x.UsageLimitPerCustomer <= 0 || x.UsageLimitPerCustomer <= x.UsageLimitTotal)
            .When(UsesStandaloneUsageControls)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.InvalidRange"));
    }
}
