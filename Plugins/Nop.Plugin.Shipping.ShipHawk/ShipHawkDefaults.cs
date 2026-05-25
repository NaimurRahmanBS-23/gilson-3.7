using Nop.Core;

namespace Nop.Plugin.Shipping.ShipHawk
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public static class ShipHawkDefaults
    {
        /// <summary>
        /// Gets the plugin system name
        /// </summary>
        public static string SystemName = "Shipping.ShipHawk";

        /// <summary>
        /// Gets a default period (in seconds) before the request times out
        /// </summary>
        public static int RequestTimeout = 30;

        /// <summary>
        /// Gets the ShipHawk Sandbox API URL
        /// </summary>
        public static string SandboxApiUrl = "https://sandbox.shiphawk.com/api/v4";

        /// <summary>
        /// Gets the ShipHawk Production API URL (generic)
        /// </summary>
        public static string ProductionApiUrl = "https://shiphawk.com/api/v4";

        /// <summary>
        /// Gets the default ShipHawk API URL
        /// </summary>
        public static string DefaultApiUrl = "https://dyllbj.tms.myshiphawk.com/api/v4";

        /// <summary>
        /// Gets the name of the address attribute used to store residential status
        /// </summary>
        public static string IsResidentialAddressAttributeName = "ShipHawk_IsResidential";

        /// <summary>
        /// Gets the default maximum shipping options to display
        /// </summary>
        public static int DefaultMaxShippingOptions = 10;

        /// <summary>
        /// Gets the name of the generic attribute to store whether freight rates are available
        /// </summary>
        public static string HasFreightRatesAttribute = "ShipHawk_HasFreightRates";
    }
}