using System;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Controllers
{
    [AdminAuthorize]
    public class DiscountManagerPlusController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IPromotionRuleModelFactory _promotionRuleModelFactory;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public DiscountManagerPlusController(
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IPromotionRuleModelFactory promotionRuleModelFactory,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWorkContext workContext)
        {
            _localizationService = localizationService;
            _permissionService = permissionService;
            _promotionRuleModelFactory = promotionRuleModelFactory;
            _settingService = settingService;
            _storeContext = storeContext;
            _storeService = storeService;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        private bool CanManageConfiguration()
        {
            return _permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManageConfiguration)
                || _permissionService.Authorize(StandardPermissionProvider.ManageDiscounts)
                || _permissionService.Authorize(StandardPermissionProvider.ManagePlugins);
        }

        private bool CanManageRules()
        {
            return _permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManagePromotionRules)
                || _permissionService.Authorize(StandardPermissionProvider.ManageDiscounts);
        }

        public ActionResult Configure()
        {
            if (!CanManageConfiguration())
                return new HttpUnauthorizedResult();

            DeleteRemovedSettings();

            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(storeScope);

            var model = new ConfigurationModel
            {
                IsEnabled = settings.IsEnabled,
                UseDefaultDiscountPipeline = settings.UseDefaultDiscountPipeline,
                EnableLogging = settings.EnableLogging,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.IsEnabled_OverrideForStore = _settingService.SettingExists(settings, x => x.IsEnabled, storeScope);
                model.UseDefaultDiscountPipeline_OverrideForStore = _settingService.SettingExists(settings, x => x.UseDefaultDiscountPipeline, storeScope);
                model.EnableLogging_OverrideForStore = _settingService.SettingExists(settings, x => x.EnableLogging, storeScope);
            }

            return View("~/Plugins/Misc.DiscountManagerPlus/Views/DiscountManagerPlus/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!CanManageConfiguration())
                return new HttpUnauthorizedResult();

            if (!ModelState.IsValid)
                return Configure();

            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(storeScope);

            settings.IsEnabled = model.IsEnabled;
            settings.UseDefaultDiscountPipeline = model.UseDefaultDiscountPipeline;
            settings.EnableLogging = model.EnableLogging;

            if (model.IsEnabled_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(settings, x => x.IsEnabled, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(settings, x => x.IsEnabled, storeScope);

            if (model.UseDefaultDiscountPipeline_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(settings, x => x.UseDefaultDiscountPipeline, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(settings, x => x.UseDefaultDiscountPipeline, storeScope);

            if (model.EnableLogging_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(settings, x => x.EnableLogging, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(settings, x => x.EnableLogging, storeScope);

            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.Configuration.Saved"));

            return Configure();
        }

        public ActionResult DiscountDetails(int discountId)
        {
            if (!CanManageRules() && !CanManageConfiguration())
                return new HttpUnauthorizedResult();

            var model = _promotionRuleModelFactory.PrepareDiscountDetailsPromotionRulesModel(discountId);
            return View("~/Plugins/Misc.DiscountManagerPlus/Views/DiscountManagerPlus/DiscountDetails.cshtml", model);
        }

        private void DeleteRemovedSettings()
        {
            foreach (var setting in _settingService.GetAllSettings())
            {
                if (string.Equals(setting.Name, "discountmanagerplussettings.maxruleevaluationtimems", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(setting.Name, "discountmanagerplussettings.enablepromotionbadge", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(setting.Name, "discountmanagerplussettings.enablecartsavingsbreakdown", StringComparison.OrdinalIgnoreCase))
                {
                    _settingService.DeleteSetting(setting);
                }
            }
        }

        #endregion
    }
}
