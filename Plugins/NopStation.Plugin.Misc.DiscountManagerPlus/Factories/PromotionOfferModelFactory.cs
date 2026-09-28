using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Factories;

public class PromotionOfferModelFactory : IPromotionOfferModelFactory
{
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ILocalizationService _localizationService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IStoreContext _storeContext;

    public PromotionOfferModelFactory(
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        IPriceFormatter priceFormatter,
        IPromotionRuleService promotionRuleService,
        IStoreContext storeContext)
    {
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _priceFormatter = priceFormatter;
        _promotionRuleService = promotionRuleService;
        _storeContext = storeContext;
    }

    private async Task<string> GetDiscountDisplayAsync(PromotionRule rule)
    {
        if (rule.RuleType == PromotionRuleType.BuyXGetY)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Discount.TierBased");

        if (rule.DiscountType == DiscountType.Percentage)
            return $"{rule.DiscountValue:0.##}%";

        if (rule.DiscountType == DiscountType.FixedAmount)
            return await _priceFormatter.FormatPriceAsync(rule.DiscountValue, true, false);

        if (rule.DiscountType == DiscountType.FixedBundlePrice)
        {
            var price = await _priceFormatter.FormatPriceAsync(rule.DiscountValue, true, false);
            return string.Format(await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Discount.FixedBundlePrice"), price);
        }

        if (rule.DiscountType == DiscountType.FreeItem)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Discount.FreeItem");

        return string.Empty;
    }

    private async Task<string> GetSummaryAsync(PromotionRule rule)
    {
        if (rule.RuleType == PromotionRuleType.ProductBased && rule.DiscountType == DiscountType.FreeItem)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ProductBasedFreeItem");

        return rule.RuleType switch
        {
            PromotionRuleType.ProductBased => await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ProductBased"),
            PromotionRuleType.ComboPricing => await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ComboPricing"),
            PromotionRuleType.BuyXGetY => await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.BuyXGetY"),
            PromotionRuleType.CartCondition => await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.CartCondition"),
            PromotionRuleType.SubtotalBased => await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Summary.SubtotalBased"),
            _ => string.Empty
        };
    }

    private async Task<string> FormatDateAsync(DateTime? dateTimeUtc)
    {
        if (!dateTimeUtc.HasValue)
            return string.Empty;

        var userDate = await _dateTimeHelper.ConvertToUserTimeAsync(dateTimeUtc.Value, DateTimeKind.Utc);
        return userDate.ToString("g");
    }

    public async Task<PromotionOfferListModel> PreparePromotionOfferListModelAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var rules = await _promotionRuleService.GetActiveRulesAsync(store.Id);

        var model = new PromotionOfferListModel
        {
            Title = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Title"),
            EmptyMessage = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Offers.Empty")
        };

        foreach (var rule in rules.OrderBy(x => x.Priority).ThenByDescending(x => x.UpdatedOnUtc))
        {
            model.Offers.Add(new PromotionOfferModel
            {
                Id = rule.Id,
                Name = rule.Name,
                Summary = await GetSummaryAsync(rule),
                RuleTypeName = await _localizationService.GetLocalizedEnumAsync(rule.RuleType),
                DiscountDisplay = await GetDiscountDisplayAsync(rule),
                StartDate = await FormatDateAsync(rule.StartDateUtc),
                EndDate = await FormatDateAsync(rule.EndDateUtc)
            });
        }

        return model;
    }
}
