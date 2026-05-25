using System;
using FluentValidation;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Manual.Validators
{
    public class PaymentInfoValidator : BaseNopValidator<PaymentInfoModel>
    {
        public PaymentInfoValidator(ILocalizationService localizationService)
        {
            //useful links:
            //http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
            //http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

            //RuleFor(x => x.CardNumber).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardNumber.Required"));
            //RuleFor(x => x.CardCode).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardCode.Required"));

            RuleFor(x => x.CardholderName).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardholderName.Required"));
            RuleFor(x => x.CardNumber).IsCreditCard().WithMessage(localizationService.GetResource("Payment.CardNumber.Wrong"));
            RuleFor(x => x.CardCode).Matches(@"^[0-9]{3,4}$").WithMessage(localizationService.GetResource("Payment.CardCode.Wrong"));


            //RuleFor(x => x.ExpireMonth).NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireMonth.Required"));
            //RuleFor(x => x.ExpireYear).NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireYear.Required"));

            //Validating Manual Credit Card Expiry - Solution
            //https://www.nopcommerce.com/boards/t/30837/validating-manual-credit-card-expiry-solution.aspx

            RuleFor(p => p.ExpireYear)
                .NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireYear.Required"))
                .Must(x => Convert.ToInt32(x) >= DateTime.Now.Year).WithMessage(localizationService.GetResource("Payment.ExpirationDate.Wrong"));

            RuleFor(p => p.ExpireMonth)
                .NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireMonth.Required"))
                .Must(x => Convert.ToInt32(x) >= DateTime.Now.Month)
                .WithMessage(localizationService.GetResource("Payment.ExpirationDate.Wrong"))
                .When(f => Convert.ToInt32(f.ExpireYear) == DateTime.Now.Year);
        }
    }
}