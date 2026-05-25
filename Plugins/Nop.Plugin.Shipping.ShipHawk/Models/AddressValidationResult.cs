using Nop.Plugin.Shipping.ShipHawk.Models.Api;
using System.Collections.Generic;

namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Represents the result of address validation for checkout
    /// </summary>
    public class AddressValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsDeliverable { get; set; }
        public AddressCheckResult SuggestedAddress { get; set; }
        public bool? IsResidential { get; set; }
        public bool WasCorrected { get; set; }
        public string LocationType { get; set; }
        public string Error { get; set; }
        public List<string> ValidationMessages { get; set; } = new List<string>();
    }
}