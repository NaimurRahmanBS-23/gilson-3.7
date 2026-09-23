using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories
{
    public class PromotionAnalyticsModelFactory : IPromotionAnalyticsModelFactory
    {
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;

        public PromotionAnalyticsModelFactory(
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            IOrderService orderService,
            IPromotionRuleService promotionRuleService,
            IStoreService storeService,
            IWorkContext workContext)
        {
            _dateTimeHelper = dateTimeHelper;
            _localizationService = localizationService;
            _orderService = orderService;
            _promotionRuleService = promotionRuleService;
            _storeService = storeService;
            _workContext = workContext;
        }

        public PromotionRuleAnalyticsSearchModel PreparePromotionRuleAnalyticsSearchModel(PromotionRuleAnalyticsSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            searchModel.AvailableStores = _storeService.GetAllStores().Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            }).ToList();
            searchModel.AvailableStores.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Common.All")
            });

            if (searchModel.PageSize <= 0)
                searchModel.PageSize = 15;

            return searchModel;
        }

        public IList<PromotionRuleAnalyticsModel> PreparePromotionRuleAnalyticsList(PromotionRuleAnalyticsSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var createdFromUtc = ConvertToUtc(searchModel.SearchCreatedFrom, false);
            var createdToUtc = ConvertToUtc(searchModel.SearchCreatedTo, true);

            var totalOrders = _orderService.SearchOrders(
                storeId: searchModel.SearchStoreId,
                createdFromUtc: createdFromUtc,
                createdToUtc: createdToUtc,
                pageIndex: 0,
                pageSize: 1);

            var analytics = _promotionRuleService.SearchPromotionRuleAnalytics(
                createdFromUtc: createdFromUtc,
                createdToUtc: createdToUtc,
                storeId: searchModel.SearchStoreId,
                pageIndex: pageIndex < 0 ? 0 : pageIndex,
                pageSize: pageSize <= 0 ? 15 : pageSize);

            totalCount = analytics.TotalCount;

            var models = new List<PromotionRuleAnalyticsModel>();
            foreach (var item in analytics)
            {
                var impactedOrdersCount = item.ImpactedOrdersCount;
                var averageDiscountPerOrder = impactedOrdersCount > 0
                    ? item.TotalDiscountAmount / impactedOrdersCount
                    : 0m;

                var orderImpactRatePercent = totalOrders.TotalCount > 0
                    ? (decimal)impactedOrdersCount * 100m / totalOrders.TotalCount
                    : 0m;

                models.Add(new PromotionRuleAnalyticsModel
                {
                    Id = item.PromotionRuleId,
                    RuleName = item.RuleName,
                    RuleTypeName = ((PromotionRuleType)item.RuleTypeId).GetLocalizedEnum(_localizationService, _workContext),
                    IsActive = item.IsActive,
                    UsageCount = item.UsageCount,
                    ImpactedOrdersCount = impactedOrdersCount,
                    ImpactedCustomersCount = item.ImpactedCustomersCount,
                    TotalDiscountAmount = item.TotalDiscountAmount,
                    TotalRevenueAmount = item.TotalRevenueAmount,
                    AverageDiscountPerOrder = averageDiscountPerOrder,
                    OrderImpactRatePercent = Math.Round(orderImpactRatePercent, 2)
                });
            }

            return models;
        }

        public PromotionRuleAnalyticsSummaryModel PreparePromotionRuleAnalyticsSummaryModel(PromotionRuleAnalyticsSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var createdFromUtc = ConvertToUtc(searchModel.SearchCreatedFrom, false);
            var createdToUtc = ConvertToUtc(searchModel.SearchCreatedTo, true);

            var totalOrders = _orderService.SearchOrders(
                storeId: searchModel.SearchStoreId,
                createdFromUtc: createdFromUtc,
                createdToUtc: createdToUtc,
                pageIndex: 0,
                pageSize: 1);

            var totals = _promotionRuleService.GetPromotionRuleAnalyticsTotals(
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

        private DateTime? ConvertToUtc(DateTime value, bool endOfDay)
        {
            if (value == default(DateTime))
                return null;

            var dateTime = value;
            if (endOfDay)
                dateTime = dateTime.Date.AddDays(1).AddTicks(-1);

            return _dateTimeHelper.ConvertToUtcTime(dateTime, _dateTimeHelper.CurrentTimeZone);
        }
    }
}
