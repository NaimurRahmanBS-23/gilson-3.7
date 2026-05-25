using Nop.Core.Domain.Common;
using Nop.Plugin.Shipping.ShipHawk.Models;
using Nop.Plugin.Shipping.ShipHawk.Models.Api;

namespace Nop.Plugin.Shipping.ShipHawk.Services
{
    /// <summary>
    /// Represents the ShipHawk address service interface
    /// </summary>
    public interface IShipHawkAddressService
    {
        /// <summary>
        /// Validates an address with ShipHawk and returns residential status
        /// Used for background validation on address insert/update events
        /// </summary>
        /// <param name="address">The nopCommerce address to validate</param>
        /// <returns>The validated address response from ShipHawk</returns>
        ShipHawkAddress ValidateAddress(Address address);

        /// <summary>
        /// Validates an address during checkout flow
        /// This validation BLOCKS checkout if address is not deliverable
        /// Uses ShipHawk /api/v4/addresses/check endpoint
        /// </summary>
        /// <param name="address">The nopCommerce shipping address to validate</param>
        /// <returns>Validation result with IsValid, IsDeliverable, SuggestedAddress, IsResidential, Error</returns>
        AddressValidationResult ValidateAddressForCheckout(Address address);

        /// <summary>
        /// Gets the residential status for an address, checking cache first
        /// </summary>
        /// <param name="address">The nopCommerce address</param>
        /// <returns>The residential status (true = residential, false = commercial, null = unknown)</returns>
        bool? GetIsResidential(Address address);

        /// <summary>
        /// Saves the residential status to the address attribute
        /// </summary>
        /// <param name="address">The nopCommerce address</param>
        /// <param name="isResidential">The residential status</param>
        void SaveIsResidential(Address address, bool isResidential);

        /// <summary>
        /// Clears the cached residential status for an address
        /// </summary>
        /// <param name="address">The nopCommerce address</param>
        void ClearIsResidentialCache(Address address);
    }
}