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
        /// Gets the current plugin version. Used for migration tracking.
        /// Increment this when adding new locale resources or settings.
        /// </summary>
        public static string PluginVersion = "1.1.0";

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

        /// <summary>
        /// Gets the default label for blended/combined shipping option (Scenario B)
        /// </summary>
        public static string DefaultBlendedRateLabel = "Gilson Best";

        /// <summary>
        /// Gets the default message shown for Gilson Best when shipping from multiple warehouses.
        /// This message informs customer that items will arrive separately.
        /// </summary>
        public static string DefaultMultiWarehouseMessage = "Your shopping cart contains items that ship from multiple locations. These items will arrive separately.";

        /// <summary>
        /// Gets the default freight message shown when freight/LTL carrier is required.
        /// IMPORTANT: Contains "freight" keyword for OPC liftgate detection.
        /// </summary>
        public static string DefaultFreightMessage = "Some of the items are large in size and/or weight and require motor freight.";
    }
}