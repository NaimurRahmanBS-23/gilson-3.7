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

        private RateResponse SendRateRequest(RateRequest rateRequest)
        {
            // Set TLS 1.2 for HTTPS connections (required by ShipHawk API)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var requestUrl = _shipHawkSettings.ApiUrl + "/rates";
            var jsonContent = JsonConvert.SerializeObject(rateRequest);

            if (_shipHawkSettings.Tracing)
            {
                _logger.Information("ShipHawk Rate Request URL: " + requestUrl);
                _logger.Information("ShipHawk Rate Request Body: " + jsonContent);
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
                        _logger.Information("ShipHawk Rate Response: " + responseContent);
                    }

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
                            if (_shipHawkSettings.Tracing)
                            {
                                _logger.Information("ShipHawk Rate Error Response: " + errorResponse);
                            }
                        }
                    }
                    _logger.Error(errorMessage, ex);
                    return new RateResponse { Error = "The system encountered a problem retrieving shipping rates. Please verify your shipping information." };
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
        /// Format: ShipHawk_{customerId}_{destinationZip}_{cartHash}
        /// Requirement (Section 8.1): Prevents redundant API calls during checkout session.
        /// </summary>
        private string GenerateCacheKey(GetShippingOptionRequest request)
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

            var key = $"ShipHawk_{request.Customer?.Id ?? 0}_{request.ShippingAddress?.ZipPostalCode}_{cartHash}";
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
                            }).ToList()
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

                var totalRate = breakdowns.Sum(b => b.Rate);
                var blendedLabel = _shipHawkSettings.BlendedRateLabel ?? "Gilson Best";
                var hasFreight = selectedRates.Any(r => IsFreightCarrier(r.Carrier));

                var descriptionBuilder = new StringBuilder("<ul>");
                foreach (var b in breakdowns)
                {
                    descriptionBuilder.Append("<li>" + b.WarehouseCode + ": " + b.ServiceName + " - $" + b.Rate.ToString("F2") + "</li>");
                }
                descriptionBuilder.Append("</ul>");
                var description = descriptionBuilder.ToString();

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
                    WarehouseBreakdowns = breakdowns
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
                // ========================================
                // Generate cache key (needed for both cache read and write)
                var cacheKey = GenerateCacheKey(shippingOptionRequest);
                
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

                    rateTasks.Add(System.Threading.Tasks.Task.Run(() =>
                    {
                        var rateResponse = SendRateRequest(rateRequest);

                        var validRates = rateResponse.Rates?
                            .Where(r => !string.IsNullOrEmpty(r.Price) && decimal.TryParse(r.Price, out _))
                            .Where(r => !excludedCarriers.Contains(r.CarrierCode?.ToLowerInvariant() ?? r.Carrier?.ToLowerInvariant() ?? ""))
                            .ToList() ?? new List<Rate>();

                        return new WarehouseRateResult
                        {
                            WarehouseCode = warehouse.Name,
                            WarehouseName = warehouse.Name,
                            Rates = validRates.Select(r => new WarehouseRate
                            {
                                ServiceName = (r.RateDisplayName ?? (r.Carrier + " " + r.ServiceLevel).Trim()).ToLowerInvariant(),
                                OriginalServiceName = r.RateDisplayName ?? (r.Carrier + " " + r.ServiceLevel).Trim(),
                                Carrier = r.Carrier,
                                Price = ParseRatePrice(r.Price),
                                RateId = r.Id,
                                ServiceDays = r.ServiceDays
                            }).ToList()
                        };
                    }));
                }

                System.Threading.Tasks.Task.WaitAll(rateTasks.ToArray());

                foreach (var task in rateTasks)
                {
                    var whResult = task.Result;
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
                    response.AddError("The system encountered a problem retrieving shipping rates. Please verify your shipping information.");
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
                response.AddError("An error occurred while calculating shipping rates. Please try again.");
            }

            return response;
        }

        #endregion
    }
}