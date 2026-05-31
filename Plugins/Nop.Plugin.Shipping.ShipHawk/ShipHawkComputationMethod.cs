using System;
using System.Collections.Generic;
using System.Web.Routing;
using Nop.Core.Plugins;
using Nop.Plugin.Shipping.ShipHawk.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.ShipHawk
{
    /// <summary>
    /// ShipHawk shipping rate computation method
    /// </summary>
    public class ShipHawkComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IShipHawkService _shipHawkService;
        private readonly ShipHawkSettings _shipHawkSettings;

        public ShipHawkComputationMethod(
            ILocalizationService localizationService,
            ISettingService settingService,
            IShipHawkService shipHawkService,
            ShipHawkSettings shipHawkSettings)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _shipHawkService = shipHawkService;
            _shipHawkSettings = shipHawkSettings;
        }

        #region Properties

        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get { return ShippingRateComputationMethodType.Realtime; }
        }

        public IShipmentTracker ShipmentTracker
        {
            get { return null; }
        }

        #endregion

        #region Methods

        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            return _shipHawkService.GetRates(getShippingOptionRequest);
        }

        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ShipHawkShipping";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Shipping.ShipHawk.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            var settings = new ShipHawkSettings
            {
                ApiUrl = ShipHawkDefaults.DefaultApiUrl,
                RequestTimeout = ShipHawkDefaults.RequestTimeout,
                MaxShippingOptions = ShipHawkDefaults.DefaultMaxShippingOptions,
                SortByPrice = true,
                ApplyRules = true,
                BlendedRateLabel = "Gilson Best",
                CacheDurationMinutes = 5,
                Tracing = false,
                UseSandbox = false
            };

            _settingService.SaveSetting(settings);

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Configuration", "ShipHawk Configuration");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Instructions", "Setup Instructions");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiKey", "API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiKey.Hint", "Enter your ShipHawk API key. This can be obtained from your ShipHawk TMS account under API Settings.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.UseSandbox", "Use Sandbox Environment");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.UseSandbox.Hint", "Check to use ShipHawk's sandbox environment for testing. Uncheck for production.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiUrl", "API URL");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiUrl.Hint", "Enter your ShipHawk TMS URL (e.g., https://yourcompany.tms.myshiphawk.com/api/v4). Leave empty to use default.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.RequestTimeout", "Request Timeout (seconds)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.RequestTimeout.Hint", "Timeout in seconds for API requests. Default is 30 seconds.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.Tracing", "Enable Tracing");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.Tracing.Hint", "Check to enable detailed logging of API requests and responses. Warning: This logs sensitive data including API keys. Do not enable in production.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.DefaultWarehouseCode", "Default Warehouse Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.DefaultWarehouseCode.Hint", "Warehouse code to use when a product has no warehouse assigned. This should match the Name field of a warehouse in nopCommerce.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupPercentage", "Markup Percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupPercentage.Hint", "Percentage markup to add to shipping rates. For example, enter 10 to add 10% markup.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupFixedAmount", "Fixed Markup Amount");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupFixedAmount.Hint", "Fixed amount to add to all shipping rates (in store currency).");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ExcludedCarriers", "Excluded Carriers");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ExcludedCarriers.Hint", "Comma-separated list of carrier codes to exclude from results. Example: usps, fedex");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MaxShippingOptions", "Maximum Shipping Options");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MaxShippingOptions.Hint", "Maximum number of shipping options to display to customers. Default is 10.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.HideTransitTime", "Hide Transit Time");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.HideTransitTime.Hint", "Check to hide estimated transit days from customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.SortByPrice", "Sort by Price");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.SortByPrice.Hint", "Check to sort shipping options by price (lowest first). Uncheck to use default display order.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApplyRules", "Apply ShipHawk Rules");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApplyRules.Hint", "Check to apply ShipHawk rules engine (carrier filters, markups, service restrictions). Uncheck for raw carrier rates without modifications.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.BlendedRateLabel", "Blended Rate Label");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.BlendedRateLabel.Hint", "Label shown to customers for blended rate when multi-warehouse carts have mixed carrier services (e.g., 'Gilson Best').");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.CacheDurationMinutes", "Cache Duration (minutes)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.CacheDurationMinutes.Hint", "Rate response cache duration in minutes. Handles redundant API calls during checkout session. Default is 5 minutes.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.EnableCaching", "Enable Caching");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.EnableCaching.Hint", "Check to enable rate response caching. Uncheck to always fetch fresh rates from ShipHawk API.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.LiftGateCheckoutAttributeId", "Lift Gate Checkout Attribute");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.LiftGateCheckoutAttributeId.Hint", "Select the checkout attribute that customers use to indicate liftgate requirement. This attribute should have values like 'Yes' and 'No'. When 'Yes' is selected, ShipHawk will include destination_liftgate accessorial in rate requests.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MultiWarehouseMessage", "Multi-Warehouse Message");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MultiWarehouseMessage.Hint", "Message shown for Gilson Best when shipping from multiple warehouses. If empty, falls back to breakdown HTML showing warehouse/service/rate details.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.FreightMessage", "Freight Message");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.FreightMessage.Hint", "Additional message shown when freight/LTL carrier is required. IMPORTANT: Must contain 'freight' or 'ltl' keyword for OPC liftgate detection to work.");

            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.ApiKeyNotConfigured", "ShipHawk API key is not configured");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.ApiUrlNotConfigured", "ShipHawk API URL is not configured");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.NoRatesAvailable", "No shipping rates available for this destination");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.RequestTimeout", "Shipping rate request timed out. Please try again.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.GeneralError", "An error occurred while calculating shipping rates. Please try again.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.NoWarehouse", "No valid warehouse found for shipping");
            this.AddOrUpdatePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.CarrierExcluded", "All available carriers have been excluded");

            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<ShipHawkSettings>();

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Configuration");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Instructions");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiKey");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiUrl");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApiUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.RequestTimeout");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.RequestTimeout.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.Tracing");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.Tracing.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.DefaultWarehouseCode");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.DefaultWarehouseCode.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupPercentage");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupPercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupFixedAmount");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MarkupFixedAmount.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ExcludedCarriers");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ExcludedCarriers.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MaxShippingOptions");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MaxShippingOptions.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.HideTransitTime");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.HideTransitTime.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.SortByPrice");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.SortByPrice.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApplyRules");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.ApplyRules.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.BlendedRateLabel");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.BlendedRateLabel.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.CacheDurationMinutes");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.CacheDurationMinutes.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.EnableCaching");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.EnableCaching.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.LiftGateCheckoutAttributeId");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.LiftGateCheckoutAttributeId.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MultiWarehouseMessage");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.MultiWarehouseMessage.Hint");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.FreightMessage");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Fields.FreightMessage.Hint");

            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.ApiKeyNotConfigured");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.ApiUrlNotConfigured");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.NoRatesAvailable");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.RequestTimeout");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.GeneralError");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.NoWarehouse");
            this.DeletePluginLocaleResource("Plugins.Shipping.ShipHawk.Error.CarrierExcluded");

            base.Uninstall();
        }

        #endregion
    }
}