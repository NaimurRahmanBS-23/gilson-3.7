using System;
using System.Linq;
using Nop.Core;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Helpers;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Factories
{
    public class PromotionOfferModelFactory : IPromotionOfferModelFactory
    {
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public PromotionOfferModelFactory(
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            IPriceFormatter priceFormatter,
            IPromotionRuleService promotionRuleService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _dateTimeHelper = dateTimeHelper;
            _localizationService = localizationService;
            _priceFormatter = priceFormatter;
            _promotionRuleService = promotionRuleService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        public PromotionOfferListModel PreparePromotionOfferListModel()
        {
            var store = _storeContext.CurrentStore;
            var rules = _promotionRuleService.GetActiveRules(store.Id);

            var model = new PromotionOfferListModel
            {
                Title = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Title"),
                EmptyMessage = _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Empty")
            };

            foreach (var rule in rules.OrderBy(x => x.Priority).ThenByDescending(x => x.UpdatedOnUtc))
            {
                model.Offers.Add(new PromotionOfferModel
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Summary = GetSummary(rule),
                    RuleTypeName = rule.RuleType.GetLocalizedEnum(_localizationService, _workContext),
                    DiscountDisplay = GetDiscountDisplay(rule),
                    StartDate = FormatDate(rule.StartDateUtc),
                    EndDate = FormatDate(rule.EndDateUtc)
                });
            }

            return model;
        }

        private string GetDiscountDisplay(PromotionRule rule)
        {
            if (rule.RuleType == PromotionRuleType.BuyXGetY)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Discount.TierBased");

            if (rule.DiscountType == DiscountType.Percentage)
                return string.Format("{0:0.##}%", rule.DiscountValue);

            if (rule.DiscountType == DiscountType.FixedAmount)
                return _priceFormatter.FormatPrice(rule.DiscountValue, true, false);

            if (rule.DiscountType == DiscountType.FixedBundlePrice)
            {
                var price = _priceFormatter.FormatPrice(rule.DiscountValue, true, false);
                return string.Format(_localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Discount.FixedBundlePrice"), price);
            }

            if (rule.DiscountType == DiscountType.FreeItem)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Discount.FreeItem");

            return string.Empty;
        }

        private string GetSummary(PromotionRule rule)
        {
            if (rule.RuleType == PromotionRuleType.ProductBased && rule.DiscountType == DiscountType.FreeItem)
                return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ProductBasedFreeItem");

            switch (rule.RuleType)
            {
                case PromotionRuleType.ProductBased:
                    return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ProductBased");
                case PromotionRuleType.ComboPricing:
                    return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ComboPricing");
                case PromotionRuleType.BuyXGetY:
                    return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.BuyXGetY");
                case PromotionRuleType.CartCondition:
                    return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.CartCondition");
                case PromotionRuleType.SubtotalBased:
                    return _localizationService.GetResource("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.SubtotalBased");
                default:
                    return string.Empty;
            }
        }

        private string FormatDate(DateTime? dateTimeUtc)
        {
            if (!dateTimeUtc.HasValue)
                return string.Empty;

            var userDate = _dateTimeHelper.ConvertToUserTime(dateTimeUtc.Value, DateTimeKind.Utc);
            return userDate.ToString("g");
        }
    }
}
