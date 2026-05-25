using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Plugin.Shipping.ShipHawk.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Shipping.ShipHawk.Controllers
{
    /// <summary>
    /// ShipHawk shipping plugin admin controller
    /// </summary>
    public class ShipHawkShippingController : BasePluginController
    {
        private readonly ICheckoutAttributeService _checkoutAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly ShipHawkSettings _shipHawkSettings;

        public ShipHawkShippingController(
            ICheckoutAttributeService checkoutAttributeService,
            ILocalizationService localizationService,
            ISettingService settingService,
            ShipHawkSettings shipHawkSettings)
        {
            _checkoutAttributeService = checkoutAttributeService;
            _localizationService = localizationService;
            _settingService = settingService;
            _shipHawkSettings = shipHawkSettings;
        }

        [AdminAuthorize]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel
            {
                ApiKey = _shipHawkSettings.ApiKey,
                ApiUrl = _shipHawkSettings.ApiUrl,
                UseSandbox = _shipHawkSettings.UseSandbox,
                DefaultWarehouseCode = _shipHawkSettings.DefaultWarehouseCode,
                RequestTimeout = _shipHawkSettings.RequestTimeout,
                MarkupPercentage = _shipHawkSettings.MarkupPercentage,
                MarkupFixedAmount = _shipHawkSettings.MarkupFixedAmount,
                ExcludedCarriers = _shipHawkSettings.ExcludedCarriers,
                MaxShippingOptions = _shipHawkSettings.MaxShippingOptions > 0 ? _shipHawkSettings.MaxShippingOptions : ShipHawkDefaults.DefaultMaxShippingOptions,
                HideTransitTime = _shipHawkSettings.HideTransitTime,
                SortByPrice = _shipHawkSettings.SortByPrice,
                ApplyRules = _shipHawkSettings.ApplyRules,
                BlendedRateLabel = _shipHawkSettings.BlendedRateLabel ?? "Gilson Best",
                EnableCaching = _shipHawkSettings.EnableCaching,
                CacheDurationMinutes = _shipHawkSettings.CacheDurationMinutes > 0 ? _shipHawkSettings.CacheDurationMinutes : 5,
                LiftGateCheckoutAttributeId = _shipHawkSettings.LiftGateCheckoutAttributeId,
                Tracing = _shipHawkSettings.Tracing
            };

            var checkoutAttributes = _checkoutAttributeService.GetAllCheckoutAttributes();
            foreach (var attribute in checkoutAttributes)
            {
                model.AvailableLiftGateCheckoutAttributes.Add(new SelectListItem
                {
                    Text = attribute.Name,
                    Value = attribute.Id.ToString(),
                    Selected = attribute.Id == model.LiftGateCheckoutAttributeId
                });
            }

            return View("~/Plugins/Shipping.ShipHawk/Views/ShipHawkShipping/Configure.cshtml", model);
        }

        [AdminAuthorize]
        [HttpPost]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            var apiUrl = model.UseSandbox
                ? ShipHawkDefaults.SandboxApiUrl
                : (!string.IsNullOrEmpty(model.ApiUrl) ? model.ApiUrl : ShipHawkDefaults.DefaultApiUrl);

            _shipHawkSettings.ApiKey = model.ApiKey;
            _shipHawkSettings.ApiUrl = apiUrl;
            _shipHawkSettings.UseSandbox = model.UseSandbox;
            _shipHawkSettings.DefaultWarehouseCode = model.DefaultWarehouseCode;
            _shipHawkSettings.RequestTimeout = model.RequestTimeout > 0 ? model.RequestTimeout : ShipHawkDefaults.RequestTimeout;
            _shipHawkSettings.MarkupPercentage = model.MarkupPercentage;
            _shipHawkSettings.MarkupFixedAmount = model.MarkupFixedAmount;
            _shipHawkSettings.ExcludedCarriers = model.ExcludedCarriers;
            _shipHawkSettings.MaxShippingOptions = model.MaxShippingOptions > 0 ? model.MaxShippingOptions : ShipHawkDefaults.DefaultMaxShippingOptions;
            _shipHawkSettings.HideTransitTime = model.HideTransitTime;
            _shipHawkSettings.SortByPrice = model.SortByPrice;
            _shipHawkSettings.ApplyRules = model.ApplyRules;
            _shipHawkSettings.BlendedRateLabel = model.BlendedRateLabel ?? "Gilson Best";
            _shipHawkSettings.EnableCaching = model.EnableCaching;
            _shipHawkSettings.CacheDurationMinutes = model.CacheDurationMinutes > 0 ? model.CacheDurationMinutes : 5;
            _shipHawkSettings.LiftGateCheckoutAttributeId = model.LiftGateCheckoutAttributeId;
            _shipHawkSettings.Tracing = model.Tracing;

            _settingService.SaveSetting(_shipHawkSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}