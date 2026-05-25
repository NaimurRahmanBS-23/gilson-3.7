using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Bss.Nop.Plugin.Custom.Attributes;
using Bss.Nop.Plugin.Custom.Models;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Services.Configuration;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using DataSourceRequest = Kendo.Mvc.UI.DataSourceRequest;

namespace Bss.Nop.Plugin.Custom.Controllers
{
    [NopHttpsRequirement(SslRequirement.NoMatter)]
    [AdminAuthorize]
    public class BssCustomAdminController : BasePluginController
    {
        #region Fields
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        #endregion

        #region Ctor
        public BssCustomAdminController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
        }
        #endregion

        #region Actions

        public ActionResult Configure()
        {
            //load settings for a current store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(storeScope);

            var model = new ConfigurationModel
            {
                Settings = customSettings,
                SelectedTabIndex = GetSelectedTabIndex()
            };

            return View(model);
        }

        [HttpPost]
        [JsonErrorHandler]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTab1Settings(ConfigurationModel model)
        {
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var customSettings = _settingService.LoadSetting<BssCustomSettings>(storeScope);

            //Addition Charge to total shipping
            customSettings.AdditionalHandlingCharge = model.Settings.AdditionalHandlingCharge;
            customSettings.IsAdditionalHandlingChargePercent = model.Settings.IsAdditionalHandlingChargePercent;

            //Adjustment to FedEx shipping
            customSettings.FedexShippingAdjustment = model.Settings.FedexShippingAdjustment;
            customSettings.IsFedexShippingAdjustmentPercent = model.Settings.IsFedexShippingAdjustmentPercent;

            //Adjustment to FedEx Express shipping
            customSettings.FedexExpressShippingAdjustment = model.Settings.FedexExpressShippingAdjustment;
            customSettings.IsFedexExpressShippingAdjustmentPercent = model.Settings.IsFedexExpressShippingAdjustmentPercent;

            //Adjustment to FedEx Freight shipping
            customSettings.FedexFreightShippingAdjustment = model.Settings.FedexFreightShippingAdjustment;
            customSettings.IsFedexFreightShippingAdjustmentPercent = model.Settings.IsFedexFreightShippingAdjustmentPercent;

            _settingService.SaveSetting(customSettings);

            return Json(new {success = true});
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a selected tab index (used in admin area to store selected tab index)
        /// </summary>
        /// <returns>Index</returns>
        private int GetSelectedTabIndex()
        {
            //keep this method synchornized with
            //"SetSelectedTabIndex" method of \Administration\Controllers\BaseNopController.cs
            int index = 0;
            string dataKey = "nop.selected-tab-index";
            if (ViewData[dataKey] is int)
            {
                index = (int)ViewData[dataKey];
            }
            if (TempData[dataKey] is int)
            {
                index = (int)TempData[dataKey];
            }

            //ensure it's not negative
            if (index < 0)
                index = 0;

            return index;
        }
        #endregion

    }
}   
