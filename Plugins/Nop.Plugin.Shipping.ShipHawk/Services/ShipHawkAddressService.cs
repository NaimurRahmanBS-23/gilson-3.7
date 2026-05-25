using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Nop.Core.Domain.Common;
using Nop.Plugin.Shipping.ShipHawk.Models;
using Nop.Plugin.Shipping.ShipHawk.Models.Api;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;

namespace Nop.Plugin.Shipping.ShipHawk.Services
{
    /// <summary>
    /// ShipHawk address validation service
    /// </summary>
    public class ShipHawkAddressService : IShipHawkAddressService
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ILogger _logger;
        private readonly ShipHawkSettings _shipHawkSettings;

        public ShipHawkAddressService(
            IGenericAttributeService genericAttributeService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            ILogger logger,
            ShipHawkSettings shipHawkSettings)
        {
            _genericAttributeService = genericAttributeService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _logger = logger;
            _shipHawkSettings = shipHawkSettings;
        }

        public ShipHawkAddress ValidateAddress(Address address)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            var stateProvince = _stateProvinceService.GetStateProvinceById(address.StateProvinceId ?? 0);
            var country = _countryService.GetCountryById(address.CountryId ?? 0);

            var shipHawkAddress = new ShipHawkAddress
            {
                Name = (address.FirstName + " " + address.LastName).Trim(),
                Company = NullIfEmpty(address.Company),
                Street1 = address.Address1,
                Street2 = NullIfEmpty(address.Address2),
                City = address.City,
                State = stateProvince?.Abbreviation,
                Zip = address.ZipPostalCode,
                Country = country?.TwoLetterIsoCode ?? "US",
                PhoneNumber = NullIfEmpty(address.PhoneNumber),
                Email = NullIfEmpty(address.Email)
            };

            var requestUrl = _shipHawkSettings.ApiUrl + "/addresses";
            var jsonContent = JsonConvert.SerializeObject(shipHawkAddress);

            // Set TLS 1.2 for HTTPS connections (required by ShipHawk API)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            if (_shipHawkSettings.Tracing)
            {
                _logger.Information("ShipHawk Address Validation Request URL: " + requestUrl);
                _logger.Information("ShipHawk Address Validation Request Body: " + jsonContent);
            }

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("x-api-key", _shipHawkSettings.ApiKey);
                client.Encoding = Encoding.UTF8;

                try
                {
                    var responseContent = client.UploadString(requestUrl, "POST", jsonContent);

                    if (_shipHawkSettings.Tracing)
                    {
                        _logger.Information("ShipHawk Address Validation Response: " + responseContent);
                    }

                    return JsonConvert.DeserializeObject<ShipHawkAddress>(responseContent);
                }
                catch (WebException ex)
                {
                    var errorMessage = "ShipHawk address validation failed: " + ex.Message;
                    if (ex.Response != null)
                    {
                        using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                        {
                            var errorResponse = reader.ReadToEnd();
                            errorMessage += " - " + errorResponse;
                            _logger.Error(errorMessage, ex);
                        }
                    }
                    throw new Exception(errorMessage, ex);
                }
            }
        }

        public AddressValidationResult ValidateAddressForCheckout(Address address)
        {
            if (address == null)
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    IsDeliverable = false,
                    Error = "Shipping address is required"
                };
            }

            if (string.IsNullOrEmpty(_shipHawkSettings.ApiKey) || string.IsNullOrEmpty(_shipHawkSettings.ApiUrl))
            {
                return new AddressValidationResult
                {
                    IsValid = true,
                    IsDeliverable = true,
                    IsResidential = null
                };
            }

            var stateProvince = _stateProvinceService.GetStateProvinceById(address.StateProvinceId ?? 0);
            var country = _countryService.GetCountryById(address.CountryId ?? 0);

            var requestBody = new
            {
                street1 = address.Address1,
                city = address.City,
                state = stateProvince?.Abbreviation,
                zip = address.ZipPostalCode,
                country = country?.TwoLetterIsoCode ?? "US"
            };

            var requestUrl = _shipHawkSettings.ApiUrl + "/addresses/check";
            var jsonContent = JsonConvert.SerializeObject(requestBody);

            // Set TLS 1.2 for HTTPS connections (required by ShipHawk API)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            if (_shipHawkSettings.Tracing)
            {
                _logger.Information("ShipHawk Address Check Request URL: " + requestUrl);
                _logger.Information("ShipHawk Address Check Request Body: " + jsonContent);
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    client.Headers.Add("x-api-key", _shipHawkSettings.ApiKey);
                    client.Encoding = Encoding.UTF8;

                    var responseContent = client.UploadString(requestUrl, "POST", jsonContent);

                    if (_shipHawkSettings.Tracing)
                    {
                        _logger.Information("ShipHawk Address Check Response: " + responseContent);
                    }

                    var checkResponse = JsonConvert.DeserializeObject<AddressCheckResponse>(responseContent);

                    if (_shipHawkSettings.Tracing && checkResponse?.Address != null)
                    {
                        _logger.Information("ShipHawk Address Check Result: deliverable=" + checkResponse.Deliverable +
                            ", corrected=" + checkResponse.Corrected +
                            ", location_type='" + checkResponse.Address.LocationType + "'");
                    }

                    var result = new AddressValidationResult
                    {
                        IsValid = checkResponse?.Deliverable ?? false,
                        IsDeliverable = checkResponse?.Deliverable ?? false,
                        SuggestedAddress = checkResponse?.Address,
                        IsResidential = checkResponse?.Address?.IsResidential,
                        WasCorrected = checkResponse?.Corrected ?? false,
                        LocationType = checkResponse?.Address?.LocationType,
                        Error = checkResponse?.Deliverable == false
                            ? "This address cannot be verified as deliverable. Please check your address details."
                            : null
                    };

                    // IsResidential is now a bool (not nullable) in AddressCheckResult
                    // Store the residential status based on location_type
                    if (checkResponse?.Deliverable == true && checkResponse.Address != null)
                    {
                        SaveIsResidential(address, checkResponse.Address.IsResidential);
                    }

                    return result;
                }
            }
            catch (WebException ex)
            {
                string errorDetails = ex.Message;
                if (ex.Response != null)
                {
                    using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                    {
                        errorDetails = reader.ReadToEnd();
                    }
                }
                _logger.Error("ShipHawk address check error: " + errorDetails, ex);
                return new AddressValidationResult
                {
                    IsValid = true,
                    IsDeliverable = true,
                    IsResidential = null,
                    Error = null,
                    ValidationMessages = new List<string> { "Address verification service unavailable." }
                };
            }
            catch (Exception ex)
            {
                _logger.Error("ShipHawk address check error: " + ex.Message, ex);
                return new AddressValidationResult
                {
                    IsValid = true,
                    IsDeliverable = true,
                    IsResidential = null,
                    Error = null,
                    ValidationMessages = new List<string> { "Address verification service unavailable." }
                };
            }
        }

        public bool? GetIsResidential(Address address)
        {
            if (address == null)
                return null;

            var cachedValue = address.GetAttribute<string>(
                ShipHawkDefaults.IsResidentialAddressAttributeName,
                _genericAttributeService);

            if (!string.IsNullOrEmpty(cachedValue))
            {
                if (bool.TryParse(cachedValue, out var isResidential))
                    return isResidential;
            }

            return null;
        }

        public void SaveIsResidential(Address address, bool isResidential)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            _genericAttributeService.SaveAttribute(
                address,
                ShipHawkDefaults.IsResidentialAddressAttributeName,
                isResidential.ToString());
        }

        public void ClearIsResidentialCache(Address address)
        {
            if (address == null)
                return;

            _genericAttributeService.SaveAttribute<string>(
                address,
                ShipHawkDefaults.IsResidentialAddressAttributeName,
                null);
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}