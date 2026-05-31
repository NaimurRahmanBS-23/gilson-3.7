using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.ShipHawk.Models;
using Nop.Plugin.Shipping.ShipHawk.Models.Api;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;

namespace Nop.Plugin.Shipping.ShipHawk.Services
{
    /// <summary>
    /// Represents the ShipHawk shipping service
    /// </summary>
    public class ShipHawkService : IShipHawkService
    {
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IShipHawkAddressService _shipHawkAddressService;
        private readonly ICacheManager _cacheManager;
        private readonly ILogger _logger;
        private readonly ShipHawkSettings _shipHawkSettings;

        public ShipHawkService(
            IProductService productService,
            IShippingService shippingService,
            IAddressService addressService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            ICheckoutAttributeParser checkoutAttributeParser,
            IGenericAttributeService genericAttributeService,
            IShipHawkAddressService shipHawkAddressService,
            ICacheManager cacheManager,
            ILogger logger,
            ShipHawkSettings shipHawkSettings)
        {
            _productService = productService;
            _shippingService = shippingService;
            _addressService = addressService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _genericAttributeService = genericAttributeService;
            _shipHawkAddressService = shipHawkAddressService;
            _cacheManager = cacheManager;
            _logger = logger;
            _shipHawkSettings = shipHawkSettings;
        }

        #region Utilities

        private bool GetLiftgateRequirement(GetShippingOptionRequest shippingOptionRequest)
        {
            try
            {
                if (shippingOptionRequest.Customer == null)
                    return false;

                if (!_shipHawkSettings.LiftGateCheckoutAttributeId.HasValue || _shipHawkSettings.LiftGateCheckoutAttributeId.Value <= 0)
                {
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: LiftGateCheckoutAttributeId is not configured in ShipHawk settings");
                    return false;
                }

                var checkoutAttributesXml = shippingOptionRequest.Customer.GetAttribute<string>(
                    SystemCustomerAttributeNames.CheckoutAttributes,
                    _genericAttributeService,
                    shippingOptionRequest.StoreId);

                if (string.IsNullOrEmpty(checkoutAttributesXml))
                {
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: No checkout attributes XML found for customer");
                    return false;
                }

                if (_shipHawkSettings.Tracing)
                    _logger.Information("ShipHawk: Checkout attributes XML: " + checkoutAttributesXml);

                var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttributesXml);
                foreach (var attributeValue in attributeValues)
                {
                    var attribute = _checkoutAttributeParser.ParseCheckoutAttributes(checkoutAttributesXml)
                        .FirstOrDefault(a => a.Id == _shipHawkSettings.LiftGateCheckoutAttributeId.Value);

                    if (attribute != null)
                    {
                        if (_shipHawkSettings.Tracing)
                            _logger.Information("ShipHawk: Found liftgate attribute '" + attribute.Name + "' (ID: " + attribute.Id + ")");

                        if (attributeValue.Name != null && attributeValue.Name.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (_shipHawkSettings.Tracing)
                                _logger.Information("ShipHawk: Liftgate required = TRUE (value name contains 'Yes')");
                            return true;
                        }
                    }
                }

                if (_shipHawkSettings.Tracing)
                    _logger.Information("ShipHawk: Liftgate attribute (ID: " + _shipHawkSettings.LiftGateCheckoutAttributeId + ") not found or value is not 'Yes'");
            }
            catch (Exception ex)
            {
                _logger.Warning("ShipHawk: Error getting liftgate requirement: " + ex.Message, ex);
            }

            if (_shipHawkSettings.Tracing)
                _logger.Information("ShipHawk: Liftgate required = FALSE");
            return false;
        }

        private bool GetIsResidential(Address shippingAddress)
        {
            var cachedValue = _shipHawkAddressService.GetIsResidential(shippingAddress);
            if (cachedValue.HasValue)
                return cachedValue.Value;

            try
            {
                var validatedAddress = _shipHawkAddressService.ValidateAddress(shippingAddress);
                var isResidential = validatedAddress?.IsResidential ?? true;
                _shipHawkAddressService.SaveIsResidential(shippingAddress, isResidential);
                return isResidential;
            }
            catch (Exception ex)
            {
                _logger.Warning("ShipHawk: Could not validate address for residential status, defaulting to residential: " + ex.Message, ex);
                return true;
            }
        }

        private ShipHawkAddress BuildDestinationAddressFromValidation(
            Address shippingAddress,
            AddressValidationResult validationResult,
            bool isResidential)
        {
            var stateProvince = _stateProvinceService.GetStateProvinceById(shippingAddress.StateProvinceId ?? 0);
            var country = _countryService.GetCountryById(shippingAddress.CountryId ?? 0);
            var suggestedAddress = validationResult.SuggestedAddress;

            return new ShipHawkAddress
            {
                Name = (shippingAddress.FirstName + " " + shippingAddress.LastName).Trim(),
                Company = NullIfEmpty(shippingAddress.Company),
                Street1 = suggestedAddress?.Street1 ?? shippingAddress.Address1,
                Street2 = suggestedAddress?.Street2 ?? NullIfEmpty(shippingAddress.Address2),
                City = suggestedAddress?.City ?? shippingAddress.City,
                State = suggestedAddress?.State ?? stateProvince?.Abbreviation,
                Zip = suggestedAddress?.ZipCode ?? shippingAddress.ZipPostalCode,
                Country = suggestedAddress?.Country ?? country?.TwoLetterIsoCode ?? "US",
                PhoneNumber = NullIfEmpty(shippingAddress.PhoneNumber),
                Email = NullIfEmpty(shippingAddress.Email),
                IsResidential = isResidential
            };
        }

        private ShipHawkAddress BuildOriginAddress(Warehouse warehouse)
        {
            var warehouseAddress = _addressService.GetAddressById(warehouse.AddressId);
            if (warehouseAddress == null)
            {
                _logger.Warning("ShipHawk: Warehouse '" + warehouse.Name + "' does not have an address configured");
                return null;
            }

            var stateProvince = _stateProvinceService.GetStateProvinceById(warehouseAddress.StateProvinceId ?? 0);
            var country = _countryService.GetCountryById(warehouseAddress.CountryId ?? 0);

            return new ShipHawkAddress
            {
                Name = warehouse.Name,
                Company = NullIfEmpty(warehouseAddress.Company),
                Street1 = warehouseAddress.Address1,
                Street2 = NullIfEmpty(warehouseAddress.Address2),
                City = warehouseAddress.City,
                State = stateProvince?.Abbreviation,
                Zip = warehouseAddress.ZipPostalCode,
                Country = country?.TwoLetterIsoCode ?? "US",
                PhoneNumber = NullIfEmpty(warehouseAddress.PhoneNumber)
            };
        }

        private Dictionary<Warehouse, List<GetShippingOptionRequest.PackageItem>> GroupItemsByWarehouse(
            GetShippingOptionRequest shippingOptionRequest)
        {
            var result = new Dictionary<Warehouse, List<GetShippingOptionRequest.PackageItem>>();
            Warehouse defaultWarehouse = null;

            foreach (var packageItem in shippingOptionRequest.Items)
            {
                var product = _productService.GetProductById(packageItem.ShoppingCartItem.ProductId);
                if (product == null)
                    continue;

                Warehouse warehouse = null;

                if (product.WarehouseId > 0)
                {
                    warehouse = _shippingService.GetWarehouseById(product.WarehouseId);
                }

                if (warehouse == null && !string.IsNullOrEmpty(_shipHawkSettings.DefaultWarehouseCode))
                {
                    if (defaultWarehouse == null)
                    {
                        var warehouses = _shippingService.GetAllWarehouses();
                        defaultWarehouse = warehouses.FirstOrDefault(w =>
                            w.Name != null && w.Name.Equals(_shipHawkSettings.DefaultWarehouseCode, StringComparison.OrdinalIgnoreCase));
                    }
                    warehouse = defaultWarehouse;

                    if (warehouse == null)
                    {
                        _logger.Warning("ShipHawk: Product '" + product.Sku + "' has no warehouse assigned and no default warehouse configured");
                        continue;
                    }

                    _logger.Warning("ShipHawk: Product '" + product.Sku + "' has no warehouse assigned, using default warehouse '" + warehouse.Name + "'");
                }

                if (warehouse == null)
                    continue;

                if (!result.ContainsKey(warehouse))
                {
                    result[warehouse] = new List<GetShippingOptionRequest.PackageItem>();
                }

                result[warehouse].Add(packageItem);
            }

            return result;
        }

        private List<RateItemRequest> BuildRateItems(
            List<GetShippingOptionRequest.PackageItem> items,
            string warehouseCode)
        {
            var rateItems = new List<RateItemRequest>();

            foreach (var packageItem in items)
            {
                var product = _productService.GetProductById(packageItem.ShoppingCartItem.ProductId);
                if (product == null)
                    continue;

                var quantity = packageItem.GetQuantity();
                var unitPrice = product.Price;
                var extendedPrice = unitPrice * quantity;

                var rateItem = new RateItemRequest
                {
                    Quantity = quantity,
                    Weight = product.Weight > 0 ? product.Weight : 1,
                    ProductSku = product.Sku ?? product.Id.ToString(),
                    Name = product.Name,
                    Value = extendedPrice,
                    WarehouseCode = warehouseCode
                };

                if (product.Length > 0)
                    rateItem.Length = product.Length;
                if (product.Width > 0)
                    rateItem.Width = product.Width;
                if (product.Height > 0)
                    rateItem.Height = product.Height;

                if (rateItem.Length.HasValue || rateItem.Width.HasValue || rateItem.Height.HasValue)
                    rateItem.DimensionUom = "in";

                rateItems.Add(rateItem);
            }

            return rateItems;
        }

        /// <summary>
        /// Sends a rate request to ShipHawk API.
        /// CAPTURES debug info instead of logging to ensure thread-safety in parallel contexts.
        /// Caller MUST log the returned debug info after Task.WaitAll completes.
        /// </summary>
        private RateResponse SendRateRequest(
            RateRequest rateRequest,
            string warehouseCode,
            out RateRequestDebugInfo debugInfo)
        {
            debugInfo = new RateRequestDebugInfo { WarehouseCode = warehouseCode };

            // Set TLS 1.2 for HTTPS connections (required by ShipHawk API)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var requestUrl = _shipHawkSettings.ApiUrl + "/rates";
            var jsonContent = JsonConvert.SerializeObject(rateRequest);

            // CAPTURE for later logging (instead of immediate logging)
            debugInfo.RequestUrl = requestUrl;
            debugInfo.RequestBody = jsonContent;

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("x-api-key", _shipHawkSettings.ApiKey);
                client.Encoding = Encoding.UTF8;

                try
                {
                    var responseContent = client.UploadString(requestUrl, "POST", jsonContent);

                    // CAPTURE response for later logging
                    debugInfo.ResponseJson = responseContent;

                    return JsonConvert.DeserializeObject<RateResponse>(responseContent);
                }
                catch (WebException ex)
                {
                    var errorMessage = "ShipHawk rate request failed: " + ex.Message;
                    if (ex.Response != null)
                    {
                        using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                        {
                            var errorResponse = reader.ReadToEnd();
                            errorMessage += " - " + errorResponse;

                            // CAPTURE error response for later logging
                            debugInfo.ErrorResponse = errorResponse;
                        }
                    }

                    // CAPTURE exception for later logging (ALWAYS logged, not gated by Tracing)
                    debugInfo.Exception = ex;
                    debugInfo.ErrorMessage = "The system encountered a problem retrieving shipping rates. Please verify your shipping information.";

                    return new RateResponse { Error = errorMessage };
                }
            }
        }

        private HashSet<string> GetExcludedCarriers()
        {
            if (string.IsNullOrEmpty(_shipHawkSettings.ExcludedCarriers))
                return new HashSet<string>();

            var carriers = _shipHawkSettings.ExcludedCarriers
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.ToLowerInvariant().Trim());

            return new HashSet<string>(carriers);
        }

        private decimal ApplyMarkup(decimal baseRate)
        {
            if (_shipHawkSettings.MarkupPercentage > 0)
            {
                baseRate = baseRate * (1 + _shipHawkSettings.MarkupPercentage / 100);
            }

            if (_shipHawkSettings.MarkupFixedAmount > 0)
            {
                baseRate = baseRate + _shipHawkSettings.MarkupFixedAmount;
            }

            return baseRate;
        }

        private decimal ParseRatePrice(string priceString)
        {
            if (string.IsNullOrEmpty(priceString))
                return 0;
            return decimal.TryParse(priceString, out var price) ? price : 0;
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private string SanitizeServiceName(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
                return "unknown";

            var sanitized = serviceName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "")
                .Replace("$", "")
                .Replace(",", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("/", "_")
                .Replace("\\", "_");

            while (sanitized.Contains("__"))
                sanitized = sanitized.Replace("__", "_");

            return sanitized.Trim('_').ToLowerInvariant();
        }

        private bool IsFreightCarrier(string carrier)
        {
            if (string.IsNullOrEmpty(carrier))
                return false;
            var normalized = carrier.ToLowerInvariant();
            return normalized.Contains("freight") || normalized.Contains("ltl");
        }

        /// <summary>
        /// Logs debug information captured during parallel rate requests.
        /// Called SEQUENTIALLY after Task.WaitAll - thread-safe database access.
        /// Tracing-gated logs only fire when Tracing is enabled.
        /// Error logs ALWAYS fire (not gated by Tracing).
        /// </summary>
        private void LogDebugInfo(RateRequestDebugInfo debugInfo)
        {
            if (debugInfo == null || !debugInfo.HasContent)
                return;

            var warehousePrefix = debugInfo.WarehouseCode != null
                ? "[" + debugInfo.WarehouseCode + "] "
                : "";

            // Tracing-gated logging (request/response details)
            if (_shipHawkSettings.Tracing)
            {
                if (!string.IsNullOrEmpty(debugInfo.RequestUrl))
                    _logger.Information("ShipHawk " + warehousePrefix + "Rate Request URL: " + debugInfo.RequestUrl);

                if (!string.IsNullOrEmpty(debugInfo.RequestBody))
                    _logger.Information("ShipHawk " + warehousePrefix + "Rate Request Body: " + debugInfo.RequestBody);

                if (!string.IsNullOrEmpty(debugInfo.ResponseJson))
                    _logger.Information("ShipHawk " + warehousePrefix + "Rate Response: " + debugInfo.ResponseJson);

                if (!string.IsNullOrEmpty(debugInfo.ErrorResponse))
                    _logger.Information("ShipHawk " + warehousePrefix + "Rate Error Response: " + debugInfo.ErrorResponse);
            }

            // ALWAYS log errors (not gated by Tracing) - this preserves original error logging behavior
            if (debugInfo.Exception != null)
            {
                var errorMessage = warehousePrefix + "ShipHawk rate request failed: " + debugInfo.Exception.Message;
                if (!string.IsNullOrEmpty(debugInfo.ErrorResponse))
                    errorMessage += " - " + debugInfo.ErrorResponse;

                _logger.Error(errorMessage, debugInfo.Exception);
            }
        }

        /// <summary>
        /// Generates MD5 hash of a string for cache key generation.
        /// Used for cart content hashing (Section 8.1).
        /// </summary>
        private string GenerateMd5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Generates cache key for rate response caching.
        /// Format: ShipHawk_{customerId}_{destinationZip}_{cartHash}_{liftgate}
        /// Requirement (Section 8.1): Prevents redundant API calls during checkout session.
        /// Fix: Include liftgate value to invalidate cache when liftgate changes.
        /// </summary>
        private string GenerateCacheKey(GetShippingOptionRequest request, bool needsLiftgate)
        {
            var cartHash = GenerateMd5Hash(
                request.Items.OrderBy(i => i.ShoppingCartItem.ProductId)
                    .Select(i =>
                    {
                        var product = _productService.GetProductById(i.ShoppingCartItem.ProductId);
                        var sku = product != null ? product.Sku : i.ShoppingCartItem.ProductId.ToString();
                        return $"{sku}:{i.ShoppingCartItem.Quantity}";
                    })
                    .Aggregate("", (a, b) => a + b + "|"));

            // Include liftgate requirement in cache key to invalidate when liftgate changes
            var key = $"ShipHawk_{request.Customer?.Id ?? 0}_{request.ShippingAddress?.ZipPostalCode}_{cartHash}_{needsLiftgate}";
            return key;
        }

        private MergeRatesResult MergeRates(List<WarehouseRateResult> warehouseResults)
        {
            var result = new MergeRatesResult();

            // Single warehouse
            if (warehouseResults.Count == 1)
            {
                var wh = warehouseResults[0];
                foreach (var rate in wh.Rates)
                {
                    result.ShippingOptions.Add(new ShippingOption
                    {
                        Name = rate.OriginalServiceName,
                        Rate = ApplyMarkup(rate.Price),
                        Description = null
                    });

                    result.RateDetails[rate.OriginalServiceName] = new ShipHawkRateDetail
                    {
                        ServiceName = rate.OriginalServiceName,
                        TotalRate = rate.Price,
                        HasFreightCarrier = IsFreightCarrier(rate.Carrier),
                        WarehouseBreakdowns = new List<WarehouseRateBreakdown>
                        {
                            new WarehouseRateBreakdown { WarehouseCode = wh.WarehouseCode, ServiceName = rate.OriginalServiceName, Rate = rate.Price }
                        },
                        // SKU-level breakdown for NetSuite order recreation
                        SkuWarehouseBreakdowns = new List<SkuWarehouseBreakdown>
                        {
                            new SkuWarehouseBreakdown
                            {
                                WarehouseCode = wh.WarehouseCode,
                                Carrier = rate.Carrier,
                                ServiceName = rate.OriginalServiceName,
                                Rate = rate.Price,
                                Skus = wh.SkuItems
                            }
                        }
                    };
                }
                result.ShippingFromMultipleLocations = false;
                return result;
            }

            // Multi-warehouse: detect scenario
            var allServiceNames = new HashSet<string>(
                warehouseResults.SelectMany(w => w.Rates).Select(r => r.ServiceName.ToLowerInvariant()));

            bool isScenarioA = allServiceNames.All(serviceName =>
                warehouseResults.All(w => w.Rates.Any(r => r.ServiceName.ToLowerInvariant() == serviceName)));

            if (isScenarioA)
            {
                // Scenario A: One combined option per service
                foreach (var serviceName in allServiceNames)
                {
                    var matchingRates = warehouseResults
                        .Select(w => w.Rates.FirstOrDefault(r => r.ServiceName.ToLowerInvariant() == serviceName))
                        .Where(r => r != null)
                        .ToList();

                    var totalRate = matchingRates.Sum(r => r.Price);
                    var originalName = matchingRates.First().OriginalServiceName;
                    var hasFreight = matchingRates.Any(r => IsFreightCarrier(r.Carrier));

                    result.ShippingOptions.Add(new ShippingOption
                    {
                        Name = originalName,
                        Rate = ApplyMarkup(totalRate),
                        Description = null
                    });

                    // Build SKU breakdowns for each warehouse that contributed to this rate
                    var skuBreakdowns = new List<SkuWarehouseBreakdown>();
                    foreach (var wh in warehouseResults)
                    {
                        var whRate = wh.Rates.FirstOrDefault(r => r.ServiceName.ToLowerInvariant() == serviceName);
                        if (whRate != null)
                        {
                            skuBreakdowns.Add(new SkuWarehouseBreakdown
                            {
                                WarehouseCode = wh.WarehouseCode,
                                Carrier = whRate.Carrier,
                                ServiceName = whRate.OriginalServiceName,
                                Rate = whRate.Price,
                                Skus = wh.SkuItems
                            });
                        }
                    }

                    result.RateDetails[originalName] = new ShipHawkRateDetail
                    {
                        ServiceName = originalName,
                        TotalRate = totalRate,
                        HasFreightCarrier = hasFreight,
                        WarehouseBreakdowns = matchingRates.Select(r =>
                            new WarehouseRateBreakdown
                            {
                                WarehouseCode = warehouseResults.First(w => w.Rates.Contains(r)).WarehouseCode,
                                ServiceName = r.OriginalServiceName,
                                Rate = r.Price
                            }).ToList(),
                        SkuWarehouseBreakdowns = skuBreakdowns
                    };
                }
            }
            else
            {
                // Scenario B: One blended option
                var selectedRates = warehouseResults
                    .Select(w => w.Rates.OrderBy(r => r.Price).FirstOrDefault())
                    .Where(r => r != null)
                    .ToList();

                var breakdowns = selectedRates
                    .Select(r => new WarehouseRateBreakdown
                    {
                        WarehouseCode = warehouseResults.First(w => w.Rates.Contains(r)).WarehouseCode,
                        ServiceName = r.OriginalServiceName,
                        Rate = r.Price
                    })
                    .ToList();

                // Build SKU breakdowns for each warehouse
                var skuBreakdowns = new List<SkuWarehouseBreakdown>();
                foreach (var wh in warehouseResults)
                {
                    var whRate = wh.Rates.OrderBy(r => r.Price).FirstOrDefault();
                    if (whRate != null)
                    {
                        skuBreakdowns.Add(new SkuWarehouseBreakdown
                        {
                            WarehouseCode = wh.WarehouseCode,
                            Carrier = whRate.Carrier,
                            ServiceName = whRate.OriginalServiceName,
                            Rate = whRate.Price,
                            Skus = wh.SkuItems
                        });
                    }
                }

                var totalRate = breakdowns.Sum(b => b.Rate);
                var blendedLabel = _shipHawkSettings.BlendedRateLabel ?? ShipHawkDefaults.DefaultBlendedRateLabel;
                var hasFreight = selectedRates.Any(r => IsFreightCarrier(r.Carrier));

                // ========================================
                // DESCRIPTION FOR GILSON BEST (SCENARIO B)
                // Two options based on settings:
                // 1. If MultiWarehouseMessage configured → show informational message
                // 2. If empty → fallback to breakdown HTML (backward compatible)
                // Freight message appended if freight detected AND setting configured
                // ========================================
                var description = "";

                if (!string.IsNullOrWhiteSpace(_shipHawkSettings.MultiWarehouseMessage))
                {
                    // Use configured informational message
                    description = "<p>" + _shipHawkSettings.MultiWarehouseMessage + "</p>";
                }
                else
                {
                    // Fallback to original breakdown HTML (backward compatible)
                    var descriptionBuilder = new StringBuilder("<ul>");
                    foreach (var b in breakdowns)
                    {
                        descriptionBuilder.Append("<li>" + b.WarehouseCode + ": " + b.ServiceName + " - $" + b.Rate.ToString("F2") + "</li>");
                    }
                    descriptionBuilder.Append("</ul>");
                    description = descriptionBuilder.ToString();
                }

                // Append freight message if freight carrier AND setting configured
                if (hasFreight && !string.IsNullOrWhiteSpace(_shipHawkSettings.FreightMessage))
                {
                    description += "<p>" + _shipHawkSettings.FreightMessage + "</p>";
                }

                result.ShippingOptions.Add(new ShippingOption
                {
                    Name = blendedLabel,
                    Rate = ApplyMarkup(totalRate),
                    Description = description
                });

                result.RateDetails[blendedLabel] = new ShipHawkRateDetail
                {
                    ServiceName = blendedLabel,
                    TotalRate = totalRate,
                    HasFreightCarrier = hasFreight,
                    WarehouseBreakdowns = breakdowns,
                    SkuWarehouseBreakdowns = skuBreakdowns
                };
            }

            result.ShippingFromMultipleLocations = true;
            return result;
        }

        #endregion

        #region Methods

        public GetShippingOptionResponse GetRates(GetShippingOptionRequest shippingOptionRequest)
        {
            var response = new GetShippingOptionResponse();

            try
            {
                if (shippingOptionRequest == null)
                    throw new ArgumentNullException("shippingOptionRequest");

                if (shippingOptionRequest.Items == null || !shippingOptionRequest.Items.Any())
                {
                    response.AddError("No shipment items");
                    return response;
                }

                if (shippingOptionRequest.ShippingAddress == null)
                {
                    response.AddError("Shipping address is not set");
                    return response;
                }

                if (string.IsNullOrEmpty(_shipHawkSettings.ApiKey))
                {
                    response.AddError("ShipHawk API key is not configured");
                    return response;
                }

                if (string.IsNullOrEmpty(_shipHawkSettings.ApiUrl))
                {
                    response.AddError("ShipHawk API URL is not configured");
                    return response;
                }

                var shippingAddress = shippingOptionRequest.ShippingAddress;

                // ========================================
                // PRE-API SPECIAL SCENARIO CHECKS
                // These scenarios return special shipping options without calling ShipHawk API
                // Matches BssShippingService pattern for consistency
                // ========================================

                // Scenario 2: International - Cannot rate international orders through ShipHawk
                if (!string.IsNullOrWhiteSpace(shippingAddress.Country?.TwoLetterIsoCode))
                {
                    if (shippingAddress.Country.TwoLetterIsoCode != "US")
                    {
                        if (_shipHawkSettings.Tracing)
                            _logger.Information("ShipHawk: International shipping detected - Country: " + shippingAddress.Country.TwoLetterIsoCode + " - Returning special International option");

                        response.ShippingOptions.Add(new ShippingOption
                        {
                            Name = "International",
                            Rate = 0,
                            Description = "A customer service representative will contact you with a shipping quote."
                        });
                        response.ShippingFromMultipleLocations = false;
                        return response;
                    }
                }
                else
                {
                    response.AddError("Please enter your shipping address information.");
                    return response;
                }

                // Scenario 3: CatalogOnly - USPS Bound Printed Material for catalog-only orders
                var allCatalogItems = shippingOptionRequest.Items.All(i =>
                {
                    var product = _productService.GetProductById(i.ShoppingCartItem.ProductId);
                    return product != null && product.Sku == "GILSON CATALOG";
                });

                if (allCatalogItems)
                {
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: CatalogOnly order detected - Returning USPS Bound Printed Material option");

                    response.ShippingOptions.Add(new ShippingOption
                    {
                        Name = "USPS Bound Printed Material",
                        Rate = 0,
                        Description = "Please allow 2-3 weeks for delivery."
                    });
                    response.ShippingFromMultipleLocations = false;
                    return response;
                }

                // Scenario 4: DownloadableProduct - No shipping required
                var allDownloadable = shippingOptionRequest.Items.All(i =>
                {
                    var product = _productService.GetProductById(i.ShoppingCartItem.ProductId);
                    return product != null && product.IsDownload;
                });

                if (allDownloadable)
                {
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: Downloadable products only - Returning special downloadable option");

                    response.ShippingOptions.Add(new ShippingOption
                    {
                        Name = "Downloadable Product",
                        Rate = 0,
                        Description = "No shipping required for downloadable products."
                    });
                    response.ShippingFromMultipleLocations = false;
                    return response;
                }

                // Scenario 5: FreeShipping - All items eligible for free shipping
                var allFreeShipping = shippingOptionRequest.Items.All(i =>
                {
                    var product = _productService.GetProductById(i.ShoppingCartItem.ProductId);
                    return product != null && product.IsFreeShipping;
                });

                if (allFreeShipping)
                {
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: All items are free shipping - Returning free shipping option");

                    response.ShippingOptions.Add(new ShippingOption
                    {
                        Name = "Free Shipping",
                        Rate = 0,
                        Description = "All shopping cart items are eligible for free shipping."
                    });
                    response.ShippingFromMultipleLocations = false;
                    return response;
                }

                // Address validation
                var validationResult = _shipHawkAddressService.ValidateAddressForCheckout(shippingAddress);

                if (!validationResult.IsValid || !validationResult.IsDeliverable)
                {
                    response.AddError(validationResult.Error ?? "Invalid shipping address. Please verify your address details.");
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: Address validation BLOCKED checkout - IsValid=" + validationResult.IsValid + ", IsDeliverable=" + validationResult.IsDeliverable);
                    return response;
                }

                if (_shipHawkSettings.Tracing)
                {
                    _logger.Information("ShipHawk: Address validation PASSED - IsDeliverable=" + validationResult.IsDeliverable);
                }

                bool isResidential;
                if (validationResult.IsResidential.HasValue)
                {
                    isResidential = validationResult.IsResidential.Value;
                }
                else
                {
                    isResidential = GetIsResidential(shippingAddress);
                }

                var destinationAddress = BuildDestinationAddressFromValidation(shippingAddress, validationResult, isResidential);

                var needsLiftgate = GetLiftgateRequirement(shippingOptionRequest);

                if (_shipHawkSettings.Tracing)
                {
                    _logger.Information("ShipHawk: Liftgate required = " + needsLiftgate);
                }

                if (shippingOptionRequest.Customer == null)
                {
                    response.AddError("No items with valid warehouse assignments found");
                    return response;
                }

                // ========================================
                // PHASE 4: REFERENCE NUMBERS FOR LIFTGATE
                // Per Requirements Document Section 6.3: "reference_numbers[Liftgate Delivery] - T if Yes, F if No"
                // ========================================
                var referenceNumbers = new System.Collections.Generic.List<ReferenceNumber>
                {
                    new ReferenceNumber
                    {
                        Name = "Liftgate Delivery",
                        Value = needsLiftgate ? "T" : "F"
                    }
                };

                if (_shipHawkSettings.Tracing)
                {
                    _logger.Information("ShipHawk: Reference number 'Liftgate Delivery' = '" + (needsLiftgate ? "T" : "F") + "'");
                }

                // Get excluded carriers
                var excludedCarriers = GetExcludedCarriers();

                // Group items by warehouse FIRST (before cache check to match 4.9 pattern)
                var itemsByWarehouse = GroupItemsByWarehouse(shippingOptionRequest);

                if (!itemsByWarehouse.Any())
                {
                    response.AddError("No items with valid warehouse assignments found");
                    return response;
                }

                // ========================================
                // PHASE 4: CHECK CACHE FIRST
                // Section 8: Rate response caching to handle redundant GetShippingOptions calls
                // Fix: Store RateDetails alongside response for order notes preservation
                // Fix: Include liftgate in cache key to invalidate when liftgate changes
                // ========================================
                // Generate cache key (needed for both cache read and write)
                // Pass needsLiftgate to ensure cache key includes liftgate requirement
                var cacheKey = GenerateCacheKey(shippingOptionRequest, needsLiftgate);
                
                // Skip cache if caching is disabled
                if (_shipHawkSettings.EnableCaching)
                {
                    var cachedResult = _cacheManager.Get<CachedRateResult>(cacheKey);

                    if (cachedResult != null && cachedResult.Response?.ShippingOptions?.Any() == true)
                    {
                        // CRITICAL: Restore RateDetails for order notes on cache hit
                        // Without this, Phase 3 (Order Notes) would fail because attributes were cleared above
                        foreach (var kvp in cachedResult.RateDetails)
                        {
                            var serviceName = kvp.Key;
                            var detail = kvp.Value;
                            var sanitized = SanitizeServiceName(serviceName);
                            _genericAttributeService.SaveAttribute(
                                shippingOptionRequest.Customer,
                                "ShipHawkRateDetail_" + sanitized,
                                JsonConvert.SerializeObject(detail),
                                shippingOptionRequest.StoreId);
                        }

                        if (_shipHawkSettings.Tracing)
                        {
                            _logger.Information("ShipHawk: Cache hit - Restored " + cachedResult.RateDetails.Count + " rate details for order notes");
                        }
                        return cachedResult.Response;
                    }

                    if (_shipHawkSettings.Tracing)
                    {
                        _logger.Information("ShipHawk: Cache miss - Generating new rates (key: " + cacheKey + ")");
                    }
                }
                else
                {
                    if (_shipHawkSettings.Tracing)
                    {
                        _logger.Information("ShipHawk: Caching disabled - Generating fresh rates");
                    }
                }

                // Clear stale rate details (on cache miss only, to match 4.9 pattern)
                if (shippingOptionRequest.Customer != null)
                {
                    var existingAttrs = _genericAttributeService.GetAttributesForEntity(shippingOptionRequest.Customer.Id, "Customer");
                    var staleRateDetails = existingAttrs.Where(a => a.Key != null && a.Key.StartsWith("ShipHawkRateDetail_")).ToList();
                    foreach (var attr in staleRateDetails)
                    {
                        _genericAttributeService.DeleteAttribute(attr);
                    }
                    if (_shipHawkSettings.Tracing && staleRateDetails.Any())
                    {
                        _logger.Information("ShipHawk: Cleared " + staleRateDetails.Count + " stale ShipHawkRateDetail_* attributes");
                    }
                }

                // Parallel rate requests
                var warehouseRateResults = new List<WarehouseRateResult>();
                var rateTasks = new List<System.Threading.Tasks.Task<WarehouseRateResult>>();

                foreach (var kvp in itemsByWarehouse)
                {
                    var warehouse = kvp.Key;
                    var items = kvp.Value;

                    var originAddress = BuildOriginAddress(warehouse);
                    if (originAddress == null)
                    {
                        response.AddError("Warehouse '" + warehouse.Name + "' does not have a valid address configured");
                        continue;
                    }

                    var rateItems = BuildRateItems(items, warehouse.Name);

                    // ========================================
                    // SKU-LEVEL CAPTURE: Build SKU item list BEFORE parallel task
                    // This allows us to show SKU, Warehouse, Carrier, Service, Rate in order notes
                    // for NetSuite order recreation
                    // ========================================
                    var skuItems = items.Select(i =>
                    {
                        var product = _productService.GetProductById(i.ShoppingCartItem.ProductId);
                        return new SkuItemInfo
                        {
                            Sku = product?.Sku ?? i.ShoppingCartItem.ProductId.ToString(),
                            ProductName = product?.Name ?? "Unknown",
                            Quantity = i.GetQuantity(),
                            Weight = product?.Weight ?? 0
                        };
                    }).ToList();

                    var rateRequest = new RateRequest
                    {
                        Items = rateItems,
                        OriginAddress = originAddress,
                        DestinationAddress = destinationAddress,
                        ApplyRules = _shipHawkSettings.ApplyRules,
                        DisplayRateDetail = true,
                        SourceSystem = "nopcommerce",
                        ReferenceNumbers = referenceNumbers
                    };

                    // Capture skuItems in closure for use inside Task.Run
                    var capturedSkuItems = skuItems;
                    var capturedWarehouseName = warehouse.Name;

                    rateTasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        // Thread-safe: SendRateRequest captures debug info instead of logging
                        RateRequestDebugInfo debugInfo;
                        var rateResponse = SendRateRequest(rateRequest, capturedWarehouseName, out debugInfo);

                        decimal tempPrice;
                        var validRates = rateResponse.Rates?
                            .Where(r => !string.IsNullOrEmpty(r.Price) && decimal.TryParse(r.Price, out tempPrice))
                            .Where(r => !excludedCarriers.Contains(r.CarrierCode?.ToLowerInvariant() ?? r.Carrier?.ToLowerInvariant() ?? ""))
                            .ToList() ?? new List<Rate>();

                        return new WarehouseRateResult
                        {
                            WarehouseCode = capturedWarehouseName,
                            WarehouseName = capturedWarehouseName,
                            Rates = validRates.Select(r => new WarehouseRate
                            {
                                ServiceName = (r.RateDisplayName ?? (r.Carrier + " " + r.ServiceLevel).Trim()).ToLowerInvariant(),
                                OriginalServiceName = r.RateDisplayName ?? (r.Carrier + " " + r.ServiceLevel).Trim(),
                                Carrier = r.Carrier,
                                Price = ParseRatePrice(r.Price),
                                RateId = r.Id,
                                ServiceDays = r.ServiceDays
                            }).ToList(),
                            SkuItems = capturedSkuItems,  // SKU items for order note breakdown
                            DebugInfo = debugInfo  // Captured for post-parallel logging
                        };
                    }));
                }

                System.Threading.Tasks.Task.WaitAll(rateTasks.ToArray());

                // SEQUENTIAL LOGGING - Thread-safe because we're now outside parallel context
                foreach (var task in rateTasks)
                {
                    var whResult = task.Result;

                    // Log debug info captured during parallel execution
                    LogDebugInfo(whResult.DebugInfo);

                    if (whResult.Rates.Any())
                    {
                        warehouseRateResults.Add(whResult);
                        if (_shipHawkSettings.Tracing)
                            _logger.Information("ShipHawk: Warehouse '" + whResult.WarehouseCode + "' returned " + whResult.Rates.Count + " valid rates");
                    }
                }

                if (!warehouseRateResults.Any())
                {
                    response.AddError("Unable to calculate shipping rates. Please contact support for assistance.");
                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: All warehouses returned null/empty rates - blocking checkout");
                    return response;
                }

                var mergeResult = MergeRates(warehouseRateResults);

                bool hasFreight = warehouseRateResults.Any(wh =>
                    wh.Rates.Any(r => (r.Carrier != null && r.Carrier.IndexOf("freight", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (r.Carrier != null && r.Carrier.IndexOf("ltl", StringComparison.OrdinalIgnoreCase) >= 0)));

                response.ShippingFromMultipleLocations = mergeResult.ShippingFromMultipleLocations;

                var maxOptions = _shipHawkSettings.MaxShippingOptions > 0
                    ? _shipHawkSettings.MaxShippingOptions
                    : ShipHawkDefaults.DefaultMaxShippingOptions;

                foreach (var option in mergeResult.ShippingOptions.OrderBy(o => o.Rate).Take(maxOptions))
                {
                    response.ShippingOptions.Add(option);
                }

                // Store rate details
                if (shippingOptionRequest.Customer != null)
                {
                    foreach (var kvp in mergeResult.RateDetails)
                    {
                        var serviceName = kvp.Key;
                        var detail = kvp.Value;
                        var sanitized = SanitizeServiceName(serviceName);
                        _genericAttributeService.SaveAttribute(
                            shippingOptionRequest.Customer,
                            "ShipHawkRateDetail_" + sanitized,
                            JsonConvert.SerializeObject(detail),
                            shippingOptionRequest.StoreId);

                        if (_shipHawkSettings.Tracing)
                            _logger.Information("ShipHawk: Stored rate detail for '" + serviceName + "' as ShipHawkRateDetail_" + sanitized);
                    }

                    _genericAttributeService.SaveAttribute(
                        shippingOptionRequest.Customer,
                        ShipHawkDefaults.HasFreightRatesAttribute,
                        hasFreight,
                        shippingOptionRequest.StoreId);

                    if (_shipHawkSettings.Tracing)
                        _logger.Information("ShipHawk: HasFreightRates = " + hasFreight);

                    // ========================================
                    // PHASE 4: STORE RESULT IN CACHE
                    // Requirement (Section 8): Cache rate responses for redundant call handling
                    // Fix: Cache both response and RateDetails for order notes preservation
                    // ========================================
                    // Only cache if caching is enabled
                    if (_shipHawkSettings.EnableCaching)
                    {
                        var cacheDuration = _shipHawkSettings.CacheDurationMinutes > 0
                            ? _shipHawkSettings.CacheDurationMinutes
                            : 5;
                        var cacheEntry = new CachedRateResult
                        {
                            Response = response,
                            RateDetails = mergeResult.RateDetails
                        };
                        _cacheManager.Set(cacheKey, cacheEntry, cacheDuration);

                        if (_shipHawkSettings.Tracing)
                        {
                            _logger.Information("ShipHawk: Stored " + mergeResult.RateDetails.Count + " rate details in cache (key: " + cacheKey + ", duration: " + cacheDuration + " min)");
                        }
                    }
                    else
                    {
                        if (_shipHawkSettings.Tracing)
                        {
                            _logger.Information("ShipHawk: Caching disabled - Not storing rates in cache");
                        }
                    }
                }

                if (!response.ShippingOptions.Any() && !response.Errors.Any())
                {
                    // HACK: Match BssShippingService error message for OPC retry button detection
                    // OPC looks for "retry" keyword to show retry button
                    response.AddError("The system encountered a problem retrieving shipping rates.  Please refresh to retry.");
                }

                // ========================================
                // SCENARIO 1: COLLECT SHIPPING OPTION
                // Always added at end of shipping options (matches BssShippingService pattern)
                // OPC displays checkout attributes for carrier/account when selected
                // ========================================
                if (response.ShippingOptions.Any())
                {
                    response.ShippingOptions.Add(new ShippingOption
                    {
                        Name = "Collect",
                        Rate = 0,
                        Description = "Have us ship with your account number."
                    });
                }

                // Log the final response for debugging
                if (_shipHawkSettings.Tracing)
                {
                    var optionsLog = response.ShippingOptions.Any() 
                        ? string.Join(", ", response.ShippingOptions.Select(o => $"Name: {o.Name}, Rate: {o.Rate}")) 
                        : "None";
                    var errorsLog = response.Errors.Any() 
                        ? string.Join(", ", response.Errors) 
                        : "None";
                    _logger.Information("ShipHawk: Final response - ShippingOptions count: " + response.ShippingOptions.Count + ", Errors count: " + response.Errors.Count + " | Options: " + optionsLog + " | Errors: " + errorsLog);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Error getting ShipHawk shipping rates: " + ex.Message;
                _logger.Error(errorMessage, ex);
                // HACK: Match BssShippingService error message for OPC retry button detection
                // OPC looks for "retry" keyword to show retry button
                response.AddError("An error occurred while calculating shipping rates.  Please refresh to retry.");
            }

            return response;
        }

        #endregion
    }
}