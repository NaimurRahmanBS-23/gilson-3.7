using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;

namespace Nop.Plugin.Shipping.FedexFreight.Models
{
    public class FedexFreightShippingModel
    {
        public FedexFreightShippingModel()
        {
            CarrierServicesOffered = new List<string>();
            AvailableCarrierServices = new List<string>();
        }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.Url")]
        public string Url { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.Key")]
        public string Key { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.AccountNumber")]
        public string AccountNumber { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.MeterNumber")]
        public string MeterNumber { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.UseResidentialRates")]
        public bool UseResidentialRates { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.ApplyDiscounts")]
        public bool ApplyDiscounts { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.AdditionalHandlingCharge")]
        public decimal AdditionalHandlingCharge { get; set; }

        public IList<string> CarrierServicesOffered { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.CarrierServices")]
        public IList<string> AvailableCarrierServices { get; set; }
        public string[] CheckedCarrierServices { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.PassDimensions")]
        public bool PassDimensions { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.PackingPackageVolume")]
        public int PackingPackageVolume { get; set; }

        public int PackingType { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.PackingType")]
        public SelectList PackingTypeValues { get; set; }

        public int DropoffType { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.DropoffType")]
        public SelectList AvailableDropOffTypes { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAccountNumber")]
        public string FreightAccountNumber { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightBillingAddress1")]
        public string FreightBillingAddress1 { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightBillingAddress2")]
        public string FreightBillingAddress2 { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightBillingCity")]
        public string FreightBillingCity { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightBillingState")]
        public string FreightBillingState { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightBillingPostalCode")]
        public string FreightBillingPostalCode { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightBillingCountry")]
        public string FreightBillingCountry { get; set; }


        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltAccountNumber")]
        public string FreightAltAccountNumber { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingAddress1")]
        public string FreightAltBillingAddress1 { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingAddress2")]
        public string FreightAltBillingAddress2 { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingCity")]
        public string FreightAltBillingCity { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingState")]
        public string FreightAltBillingState { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingPostalCode")]
        public string FreightAltBillingPostalCode { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.FedexFreight.Fields.FreightAltBillingCountry")]
        public string FreightAltBillingCountry { get; set; }

    }
}