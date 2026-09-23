using System.Web.Mvc;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Controllers
{
    [AdminAuthorize]
    public class PromotionAnalyticsController : BasePluginController
    {
        private readonly IPermissionService _permissionService;
        private readonly IPromotionAnalyticsModelFactory _promotionAnalyticsModelFactory;

        public PromotionAnalyticsController(
            IPermissionService permissionService,
            IPromotionAnalyticsModelFactory promotionAnalyticsModelFactory)
        {
            _permissionService = permissionService;
            _promotionAnalyticsModelFactory = promotionAnalyticsModelFactory;
        }

        private bool CanManageRules()
        {
            return _permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManagePromotionRules)
                || _permissionService.Authorize(StandardPermissionProvider.ManageDiscounts);
        }

        public ActionResult List()
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var searchModel = _promotionAnalyticsModelFactory.PreparePromotionRuleAnalyticsSearchModel(new PromotionRuleAnalyticsSearchModel());
            return View("~/Plugins/Misc.DiscountManagerPlus/Views/PromotionAnalytics/List.cshtml", searchModel);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult List(DataSourceRequest command, PromotionRuleAnalyticsSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var pageIndex = command != null && command.Page > 0 ? command.Page - 1 : 0;
            var pageSize = command != null && command.PageSize > 0 ? command.PageSize : 15;
            var items = _promotionAnalyticsModelFactory.PreparePromotionRuleAnalyticsList(searchModel, pageIndex, pageSize, out totalCount);
            return Json(new DataSourceResult
            {
                Data = items,
                Total = totalCount
            });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Summary(PromotionRuleAnalyticsSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var model = _promotionAnalyticsModelFactory.PreparePromotionRuleAnalyticsSummaryModel(searchModel);
            return Json(model);
        }
    }
}
