using FluentValidation;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Validators
{
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

            if (ruleType == PromotionRuleType.ProductBased)
                return discountType == DiscountType.Percentage || discountType == DiscountType.FixedAmount || discountType == DiscountType.FreeItem;
            if (ruleType == PromotionRuleType.ComboPricing)
                return discountType == DiscountType.Percentage || discountType == DiscountType.FixedAmount || discountType == DiscountType.FixedBundlePrice;
            if (ruleType == PromotionRuleType.CartCondition)
                return discountType == DiscountType.Percentage || discountType == DiscountType.FixedAmount;
            if (ruleType == PromotionRuleType.SubtotalBased)
                return discountType == DiscountType.Percentage || discountType == DiscountType.FixedAmount;

            return false;
        }

        public PromotionRuleValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.Required"))
                .Length(0, 400)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.MaxLength"));

            RuleFor(x => x.SystemName)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.Required"))
                .Length(0, 400)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.MaxLength"));

            RuleFor(x => x.RuleTypeId)
                .GreaterThan(0)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType.Required"));

            RuleFor(x => x.DiscountTypeId)
                .GreaterThan(0)
                .When(x => x.RuleTypeId != (int)PromotionRuleType.BuyXGetY)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.Required"));

            RuleFor(x => x.DiscountScopeId)
                .Must(x => System.Enum.IsDefined(typeof(DiscountScope), x))
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope.Required"));

            RuleFor(x => x)
                .Must(x => IsDiscountTypeAllowedForRuleType(x.RuleTypeId, x.DiscountTypeId))
                .When(x => x.RuleTypeId != (int)PromotionRuleType.BuyXGetY)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.InvalidForRuleType"));

            RuleFor(x => x.DiscountValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountValue.Invalid"));

            RuleFor(x => x)
                .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate.Value >= x.StartDate.Value)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EndDate.InvalidRange"));

            RuleFor(x => x.UsageLimitTotal)
                .GreaterThanOrEqualTo(0)
                .When(UsesStandaloneUsageControls)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitTotal.Invalid"));

            RuleFor(x => x.UsageLimitPerCustomer)
                .GreaterThanOrEqualTo(0)
                .When(UsesStandaloneUsageControls)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.Invalid"));

            RuleFor(x => x)
                .Must(x => !x.UsageWindowStartUtc.HasValue || !x.UsageWindowEndUtc.HasValue || x.UsageWindowEndUtc.Value >= x.UsageWindowStartUtc.Value)
                .When(UsesStandaloneUsageControls)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowEndUtc.InvalidRange"));

            RuleFor(x => x)
                .Must(x => x.UsageLimitTotal <= 0 || x.UsageLimitPerCustomer <= 0 || x.UsageLimitPerCustomer <= x.UsageLimitTotal)
                .When(UsesStandaloneUsageControls)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.InvalidRange"));
        }
    }
}
