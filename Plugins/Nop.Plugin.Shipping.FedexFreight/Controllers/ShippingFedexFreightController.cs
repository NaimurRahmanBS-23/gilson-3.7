using System;
using System.Text;
using System.Web.Mvc;
using Nop.Plugin.Shipping.FedexFreight.Domain;
using Nop.Plugin.Shipping.FedexFreight.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Shipping.FedexFreight.Controllers
{
    [AdminAuthorize]
    public class ShippingFedexFreightController : BasePluginController
    {
        private readonly FedexFreightSettings _fedexFreightSettings;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public ShippingFedexFreightController(FedexFreightSettings fedexFreightSettings,
            ISettingService settingService,
            ILocalizationService localizationService)
        {
            this._fedexFreightSettings = fedexFreightSettings;
            this._settingService = settingService;
            this._localizationService = localizationService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new FedexFreightShippingModel();
            model.Url = _fedexFreightSettings.Url;
            model.Key = _fedexFreightSettings.Key;
            model.Password = _fedexFreightSettings.Password;
            model.AccountNumber = _fedexFreightSettings.AccountNumber;
            model.MeterNumber = _fedexFreightSettings.MeterNumber;
            model.DropoffType = Convert.ToInt32(_fedexFreightSettings.DropoffType);
            model.AvailableDropOffTypes = _fedexFreightSettings.DropoffType.ToSelectList();
            model.UseResidentialRates = _fedexFreightSettings.UseResidentialRates;
            model.ApplyDiscounts = _fedexFreightSettings.ApplyDiscounts;
            model.AdditionalHandlingCharge = _fedexFreightSettings.AdditionalHandlingCharge;
            model.PackingPackageVolume = _fedexFreightSettings.PackingPackageVolume;
            model.PackingType = Convert.ToInt32(_fedexFreightSettings.PackingType);
            model.PackingTypeValues = _fedexFreightSettings.PackingType.ToSelectList();
            model.PassDimensions = _fedexFreightSettings.PassDimensions;
            model.FreightAccountNumber = _fedexFreightSettings.FreightAccountNumber;
            model.FreightBillingAddress1 = _fedexFreightSettings.FreightBillingAddress1;
            model.FreightBillingAddress2 = _fedexFreightSettings.FreightBillingAddress2;
            model.FreightBillingCity = _fedexFreightSettings.FreightBillingCity;
            model.FreightBillingCountry = _fedexFreightSettings.FreightBillingCountry;
            model.FreightBillingPostalCode = _fedexFreightSettings.FreightBillingPostalCode;
            model.FreightBillingState = _fedexFreightSettings.FreightBillingState;

            model.FreightAltAccountNumber = _fedexFreightSettings.FreightAltAccountNumber;
            model.FreightAltBillingAddress1 = _fedexFreightSettings.FreightAltBillingAddress1;
            model.FreightAltBillingAddress2 = _fedexFreightSettings.FreightAltBillingAddress2;
            model.FreightAltBillingCity = _fedexFreightSettings.FreightAltBillingCity;
            model.FreightAltBillingCountry = _fedexFreightSettings.FreightAltBillingCountry;
            model.FreightAltBillingPostalCode = _fedexFreightSettings.FreightAltBillingPostalCode;
            model.FreightAltBillingState = _fedexFreightSettings.FreightAltBillingState;


            var services = new FedexFreightServices();
            // Load service names
            string carrierServicesOfferedDomestic = _fedexFreightSettings.CarrierServicesOffered;
            foreach (string service in services.Services)
                model.AvailableCarrierServices.Add(service);

            if (!String.IsNullOrEmpty(carrierServicesOfferedDomestic))
                foreach (string service in services.Services)
                {
                    string serviceId = FedexFreightServices.GetServiceId(service);
                    if (!String.IsNullOrEmpty(serviceId) && !String.IsNullOrEmpty(carrierServicesOfferedDomestic))
                    {
                        if (carrierServicesOfferedDomestic.Contains(serviceId))
                            model.CarrierServicesOffered.Add(service);
                    }
                }

            return View("~/Plugins/Shipping.FedexFreight/Views/ShippingFedex/Configure.cshtml", model);
        }

        [HttpPost]
        [ChildActionOnly]
        public ActionResult Configure(FedexFreightShippingModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //save settings
            _fedexFreightSettings.Url = model.Url;
            _fedexFreightSettings.Key = model.Key;
            _fedexFreightSettings.Password = model.Password;
            _fedexFreightSettings.AccountNumber = model.AccountNumber;
            _fedexFreightSettings.MeterNumber = model.MeterNumber;
            _fedexFreightSettings.DropoffType = (DropoffType)model.DropoffType;
            _fedexFreightSettings.UseResidentialRates = model.UseResidentialRates;
            _fedexFreightSettings.ApplyDiscounts = model.ApplyDiscounts;
            _fedexFreightSettings.AdditionalHandlingCharge = model.AdditionalHandlingCharge;
            _fedexFreightSettings.PackingPackageVolume = model.PackingPackageVolume;
            _fedexFreightSettings.PackingType = (PackingType)model.PackingType;
            _fedexFreightSettings.PassDimensions = model.PassDimensions;
            _fedexFreightSettings.FreightAccountNumber = model.FreightAccountNumber;
            _fedexFreightSettings.FreightBillingAddress1 = model.FreightBillingAddress1;
            _fedexFreightSettings.FreightBillingAddress2 = model.FreightBillingAddress2;
            _fedexFreightSettings.FreightBillingCity = model.FreightBillingCity;
            _fedexFreightSettings.FreightBillingCountry = model.FreightBillingCountry;
            _fedexFreightSettings.FreightBillingPostalCode = model.FreightBillingPostalCode;
            _fedexFreightSettings.FreightBillingState = model.FreightBillingState;

            _fedexFreightSettings.FreightAltAccountNumber = model.FreightAltAccountNumber;
            _fedexFreightSettings.FreightAltBillingAddress1 = model.FreightAltBillingAddress1;
            _fedexFreightSettings.FreightAltBillingAddress2 = model.FreightAltBillingAddress2;
            _fedexFreightSettings.FreightAltBillingCity = model.FreightAltBillingCity;
            _fedexFreightSettings.FreightAltBillingCountry = model.FreightAltBillingCountry;
            _fedexFreightSettings.FreightAltBillingPostalCode = model.FreightAltBillingPostalCode;
            _fedexFreightSettings.FreightAltBillingState = model.FreightAltBillingState;


            // Save selected services
            var carrierServicesOfferedDomestic = new StringBuilder();
            int carrierServicesDomesticSelectedCount = 0;
            if (model.CheckedCarrierServices != null)
            {
                foreach (var cs in model.CheckedCarrierServices)
                {
                    carrierServicesDomesticSelectedCount++;
                    string serviceId = FedexFreightServices.GetServiceId(cs);
                    if (!String.IsNullOrEmpty(serviceId))
                        carrierServicesOfferedDomestic.AppendFormat("{0}:", serviceId);
                }
            }
            // Add default options if no services were selected
            if (carrierServicesDomesticSelectedCount == 0)
                _fedexFreightSettings.CarrierServicesOffered = "FEDEX_2_DAY:PRIORITY_OVERNIGHT:FEDEX_GROUND:GROUND_HOME_DELIVERY:INTERNATIONAL_ECONOMY";
            else
                _fedexFreightSettings.CarrierServicesOffered = carrierServicesOfferedDomestic.ToString();


            _settingService.SaveSetting(_fedexFreightSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}
