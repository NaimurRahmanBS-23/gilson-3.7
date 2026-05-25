using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.ShipHawk.Services
{
    /// <summary>
    /// Represents the ShipHawk shipping service interface
    /// </summary>
    public interface IShipHawkService
    {
        /// <summary>
        /// Gets shipping rates from ShipHawk
        /// </summary>
        /// <param name="shippingOptionRequest">The shipping option request</param>
        /// <returns>The shipping option response with available rates</returns>
        GetShippingOptionResponse GetRates(GetShippingOptionRequest shippingOptionRequest);
    }
}