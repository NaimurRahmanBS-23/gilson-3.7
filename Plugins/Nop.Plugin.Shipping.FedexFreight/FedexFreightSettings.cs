
using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.FedexFreight
{
    public class FedexFreightSettings : ISettings
    {
        public string Url { get; set; }

        public string Key { get; set; }

        public string Password { get; set; }

        public string AccountNumber { get; set; }

        public string MeterNumber { get; set; }

        public DropoffType DropoffType { get; set; }

        public bool UseResidentialRates { get; set; }

        public bool ApplyDiscounts { get; set; }

        public decimal AdditionalHandlingCharge { get; set; }

        public string CarrierServicesOffered { get; set; }

        public bool PassDimensions { get; set; }

        public int PackingPackageVolume { get; set; }

        public PackingType PackingType { get; set; }

        public string FreightAccountNumber { get; set; }
        public string FreightBillingAddress1 { get; set; }
        public string FreightBillingAddress2 { get; set; }
        public string FreightBillingCity { get; set; }
        public string FreightBillingState { get; set; }
        public string FreightBillingPostalCode { get; set; }
        public string FreightBillingCountry { get; set; }

        public string FreightAltAccountNumber { get; set; }
        public string FreightAltBillingAddress1 { get; set; }
        public string FreightAltBillingAddress2 { get; set; }
        public string FreightAltBillingCity { get; set; }
        public string FreightAltBillingState { get; set; }
        public string FreightAltBillingPostalCode { get; set; }
        public string FreightAltBillingCountry { get; set; }

    }
}