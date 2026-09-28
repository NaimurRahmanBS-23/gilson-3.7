using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Web.Framework.Models.Extensions;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;

public class PromotionAnalyticsModelFactory : IPromotionAnalyticsModelFactory
{
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ILocalizationService _localizationService;
    private readonly IOrderService _orderService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IStoreService _storeService;

    public PromotionAnalyticsModelFactory(
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        IOrderService orderService,
        IPromotionRuleService promotionRuleService,
        IStoreService storeService)
    {
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _orderService = orderService;
        _promotionRuleService = promotionRuleService;
        _storeService = storeService;
    }

    public async Task<PromotionRuleAnalyticsSearchModel> PreparePromotionRuleAnalyticsSearchModelAsync(PromotionRuleAnalyticsSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var stores = await _storeService.GetAllStoresAsync();
        searchModel.AvailableStores = stores.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = x.Name
        }).ToList();
        searchModel.AvailableStores.Insert(0, new SelectListItem
        {
            Value = "0",
            Text = await _localizationService.GetResourceAsync("Admin.Common.All")
        });

        searchModel.SetGridPageSize();
        return searchModel;
    }

    public async Task<PromotionRuleAnalyticsListModel> PreparePromotionRuleAnalyticsListModelAsync(PromotionRuleAnalyticsSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var createdFromUtc = await ConvertToUtcAsync(searchModel.SearchCreatedFrom);
        var createdToUtc = await ConvertToUtcAsync(searchModel.SearchCreatedTo, true);

        var totalOrders = await _orderService.SearchOrdersAsync(
            storeId: searchModel.SearchStoreId,
            createdFromUtc: createdFromUtc,
            createdToUtc: createdToUtc,
            pageIndex: 0,
            pageSize: 1,
            getOnlyTotalCount: true);

        var analytics = await _promotionRuleService.SearchPromotionRuleAnalyticsAsync(
            createdFromUtc: createdFromUtc,
            createdToUtc: createdToUtc,
            storeId: searchModel.SearchStoreId,
            pageIndex: searchModel.Page - 1,
            pageSize: searchModel.PageSize);

        var model = await new PromotionRuleAnalyticsListModel().PrepareToGridAsync(searchModel, analytics, () =>
            analytics.SelectAwait(async item =>
            {
                var impactedOrdersCount = item.ImpactedOrdersCount;
                var averageDiscountPerOrder = impactedOrdersCount > 0
                    ? item.TotalDiscountAmount / impactedOrdersCount
                    : 0m;

                var orderImpactRatePercent = totalOrders.TotalCount > 0
                    ? (decimal)impactedOrdersCount * 100m / totalOrders.TotalCount
                    : 0m;

                return new PromotionRuleAnalyticsModel
                {
                    Id = item.PromotionRuleId,
                    RuleName = item.RuleName,
                    RuleTypeName = await _localizationService.GetLocalizedEnumAsync((PromotionRuleType)item.RuleTypeId),
                    IsActive = item.IsActive,
                    UsageCount = item.UsageCount,
                    ImpactedOrdersCount = impactedOrdersCount,
                    ImpactedCustomersCount = item.ImpactedCustomersCount,
                    TotalDiscountAmount = item.TotalDiscountAmount,
                    TotalRevenueAmount = item.TotalRevenueAmount,
                    AverageDiscountPerOrder = averageDiscountPerOrder,
                    OrderImpactRatePercent = Math.Round(orderImpactRatePercent, 2)
                };
            }));

        return model;
    }

    public async Task<PromotionRuleAnalyticsSummaryModel> PreparePromotionRuleAnalyticsSummaryModelAsync(PromotionRuleAnalyticsSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var createdFromUtc = await ConvertToUtcAsync(searchModel.SearchCreatedFrom);
        var createdToUtc = await ConvertToUtcAsync(searchModel.SearchCreatedTo, true);

        var totalOrders = await _orderService.SearchOrdersAsync(
            storeId: searchModel.SearchStoreId,
            createdFromUtc: createdFromUtc,
            createdToUtc: createdToUtc,
            pageIndex: 0,
            pageSize: 1,
            getOnlyTotalCount: true);

        var totals = await _promotionRuleService.GetPromotionRuleAnalyticsTotalsAsync(
            createdFromUtc: createdFromUtc,
            createdToUtc: createdToUtc,
            storeId: searchModel.SearchStoreId);

        var orderImpactRatePercent = totalOrders.TotalCount > 0
            ? (decimal)totals.ImpactedOrdersCount * 100m / totalOrders.TotalCount
            : 0m;

        return new PromotionRuleAnalyticsSummaryModel
        {
            TotalUsageCount = totals.UsageCount,
            TotalImpactedOrdersCount = totals.ImpactedOrdersCount,
            TotalImpactedCustomersCount = totals.ImpactedCustomersCount,
            TotalDiscountAmount = totals.TotalDiscountAmount,
            TotalRevenueAmount = totals.TotalRevenueAmount,
            TotalOrdersCount = totalOrders.TotalCount,
            OrderImpactRatePercent = Math.Round(orderImpactRatePercent, 2)
        };
    }

    private async Task<DateTime?> ConvertToUtcAsync(DateTime? value, bool endOfDay = false)
    {
        if (!value.HasValue)
            return null;

        var dateTime = value.Value;
        if (endOfDay)
            dateTime = dateTime.Date.AddDays(1).AddTicks(-1);

        var timeZone = await _dateTimeHelper.GetCurrentTimeZoneAsync();
        return _dateTimeHelper.ConvertToUtcTime(dateTime, timeZone);
    }
}
