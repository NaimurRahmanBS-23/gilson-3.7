using FluentValidation;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Validators
{
    public class PromotionRuleTierValidator : BaseNopValidator<PromotionRuleTierModel>
    {
        public PromotionRuleTierValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.MinQuantity)
                .GreaterThan(0)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MinQuantity.Required"));

            RuleFor(x => x.MaxQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity.Invalid"));

            RuleFor(x => x)
                .Must(x => x.MaxQuantity <= 0 || x.MaxQuantity >= x.MinQuantity)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity.Invalid"));

            RuleFor(x => x.DiscountTypeId)
                .GreaterThan(0)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType.Required"));

            RuleFor(x => x.DiscountValue)
                .GreaterThan(0)
                .When(x => x.DiscountTypeId != (int)DiscountType.FreeItem)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountValue.Required"));

            RuleFor(x => x.RewardQuantity)
                .GreaterThan(0)
                .When(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardQuantity.Required"));

            RuleFor(x => x)
                .Must(x => x.DiscountTypeId == (int)DiscountType.FreeItem || !x.AutoAddReward)
                .WithMessage(localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AutoAddReward.Invalid"));
        }
    }
}
