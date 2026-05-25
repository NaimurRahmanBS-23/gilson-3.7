using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Tax.Vertex
{
public class VertexTaxProvider : BasePlugin, ITaxProvider
{
private readonly IWorkContext _workContext;
private readonly IShoppingCartService _shoppingCartService;
private readonly IOrderTotalCalculationService _orderTotals;
private readonly IPriceCalculationService _priceCalculationService;
private readonly IStoreContext _storeContext;
private readonly ICountryService _countryService;
private readonly IStateProvinceService _stateService;
private readonly ISettingService _settingService;
private readonly IGenericAttributeService _genericAttributeService;
private readonly ShippingSettings _shippingSettings;
private readonly TaxSettings _taxSettings;
private readonly VertexSettings _vertexSettings;
private readonly ILogger _logger;

    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(8);
    private static readonly HttpClient Http = new HttpClient { Timeout = HttpTimeout };

    private static string _cachedToken;
    private static DateTime _tokenExpiryUtc = DateTime.MinValue;
    private static readonly object _tokenLock = new object();

    private static readonly string GuardKey = "VertexTaxProvider_Inflight";

    public VertexTaxProvider(
        IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPriceCalculationService priceCalculationService,
        IStoreContext storeContext,
        ICountryService countryService,
        IStateProvinceService stateService,
        ISettingService settingService,
        IGenericAttributeService genericAttributeService,
        ShippingSettings shippingSettings,
        TaxSettings taxSettings,
        VertexSettings vertexSettings,
        ILogger logger)
    {
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _orderTotals = orderTotalCalculationService;
        _priceCalculationService = priceCalculationService;
        _storeContext = storeContext;
        _countryService = countryService;
        _stateService = stateService;
        _settingService = settingService;
        _genericAttributeService = genericAttributeService;
        _shippingSettings = shippingSettings;
        _taxSettings = taxSettings;
        _vertexSettings = vertexSettings;
        _logger = logger;

        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Culture = System.Globalization.CultureInfo.InvariantCulture
        };
    }

    public CalculateTaxResult GetTaxRate(CalculateTaxRequest request)
    {
        var res = new CalculateTaxResult();
        Customer ctxCust = null;

        try { ctxCust = _workContext != null ? _workContext.CurrentCustomer : null; } catch { ctxCust = null; }

        if (IsReentrant())
        {
            LogWarn(
                "Vertex reentry blocked",
                ctxCust,
                "GetTaxRate was called recursively on the same HTTP request."
                + " TaxCategoryId=" + (request != null ? request.TaxCategoryId.ToString() : "null"));
            res.TaxRate = 0m;
            return res;
        }

        MarkReentrant();

        try
        {
            var customer = request != null && request.Customer != null ? request.Customer : ctxCust;
            if (customer == null)
            {
                LogWarn(
                    "Vertex returning 0: no customer resolved",
                    null,
                    "request.Customer was null and WorkContext.CurrentCustomer was also null."
                    + " TaxCategoryId=" + (request != null ? request.TaxCategoryId.ToString() : "null"));
                res.TaxRate = 0m;
                return res;
            }

            var cart = GetCart(customer);
            if (cart.Count == 0)
            {
                LogWarn(
                    "Vertex returning 0: cart is empty",
                    ctxCust,
                    "No shopping cart items found for customer."
                    + " CustomerId=" + (customer != null ? customer.Id.ToString() : "null")
                    + " TaxCategoryId=" + (request != null ? request.TaxCategoryId.ToString() : "null"));
                res.TaxRate = 0m;
                return res;
            }

            var shipTo = ResolveShipTo(request, customer);


            // Short-circuit for international addresses — Gilson does not collect tax on non-US orders.
            //if (shipTo != null && shipTo.CountryId.HasValue && shipTo.CountryId.Value > 0)
            //{
            //    var shipToCountry = _countryService.GetCountryById(shipTo.CountryId.Value);
            //    var shipToIso2 = shipToCountry != null ? shipToCountry.TwoLetterIsoCode : null;
            //    if (!string.IsNullOrEmpty(shipToIso2) && !shipToIso2.Equals("US", StringComparison.OrdinalIgnoreCase))
            //    {
            //        LogWarn(
            //            "Vertex returning 0: international address, tax not collected",
            //            ctxCust,
            //            "CountryId=" + shipTo.CountryId.Value + " IsoCode=" + shipToIso2
            //            + " CustomerId=" + customer.Id);
            //        res.TaxRate = 0m;
            //        return res;
            //    }
            //}


            if (!IsAddressComplete(shipTo))
            {
                if (_vertexSettings != null && _vertexSettings.EnableVerboseLogging)
                {                
                    var detail = shipTo == null
                        ? "Resolved address is null."
                          + " request.Address null=" + (request == null || request.Address == null)
                          + " | customer.Addresses count=" + (customer.Addresses != null ? customer.Addresses.Count.ToString() : "null")
                        : "Address fields:"
                          + " CountryId=" + (shipTo.CountryId.HasValue ? shipTo.CountryId.Value.ToString() : "null")
                          + " StateProvinceId=" + (shipTo.StateProvinceId.HasValue ? shipTo.StateProvinceId.Value.ToString() : "null")
                          + " ZipPostalCode=" + (string.IsNullOrWhiteSpace(shipTo.ZipPostalCode) ? "[empty]" : shipTo.ZipPostalCode)
                          + " City=" + (string.IsNullOrWhiteSpace(shipTo.City) ? "[empty]" : shipTo.City)
                          + " Address1=" + (string.IsNullOrWhiteSpace(shipTo.Address1) ? "[empty]" : "[present]");

                    LogWarn("Vertex destination incomplete, returning 0", ctxCust, detail);
                }
                res.TaxRate = 0m;
                return res;
            }

            var isShipping = IsShippingTaxRequest(request);

            IList<ShoppingCartItem> subset;
            decimal shippingForThisCall = 0m;

            if (isShipping)
            {
                shippingForThisCall = GetShippingTotal(cart, customer);
                subset = new List<ShoppingCartItem>();

                if (shippingForThisCall <= 0m)
                {
                    LogWarn(
                        "Vertex returning 0: shipping total is zero",
                        ctxCust,
                        "Shipping is taxable but GetShippingTotal returned 0 or less."
                        + " CustomerId=" + customer.Id
                        + " CartItems=" + cart.Count);
                    res.TaxRate = 0m;
                    return res;
                }
            }
            else
            {
                var taxCat = request != null ? request.TaxCategoryId : 0;
                subset = cart.Where(ci => ci.Product != null && ci.Product.TaxCategoryId == taxCat).ToList();
                if (subset.Count == 0)
                {
                    LogWarn(
                        "Vertex returning 0: no cart items match tax category",
                        ctxCust,
                        "No shopping cart items found with TaxCategoryId=" + taxCat
                        + " | Total cart items=" + cart.Count
                        + " | CustomerId=" + customer.Id);
                    res.TaxRate = 0m;
                    return res;
                }
            }

            var cacheKey = BuildCacheKey(
                customer,
                shipTo,
                isShipping,
                request != null ? request.TaxCategoryId : 0,
                isShipping ? new List<ShoppingCartItem>() : subset
            );

            var pr = TryReadPerRequest(cacheKey);
            if (pr.HasValue)
            {
                res.TaxRate = pr.Value;
                return res;
            }

            var sr = TryReadSession(cacheKey);
            if (sr.HasValue)
            {
                res.TaxRate = sr.Value;
                WritePerRequest(cacheKey, sr.Value);
                return res;
            }

            var vxReq = BuildVertexRequest(customer, shipTo, subset, shippingForThisCall);

            decimal baseTaxable = 0m;
            try
            {
                if (vxReq != null && vxReq.lineItems != null)
                    baseTaxable = vxReq.lineItems.Sum(li => li != null ? li.extendedPrice : 0m);
            }
            catch { baseTaxable = 0m; }

            if (baseTaxable <= 0m)
            {
                LogWarn(
                    "Vertex returning 0: baseTaxable is zero",
                    ctxCust,
                    "All line items resolved to zero extended price."
                    + " isShipping=" + isShipping
                    + " | LineItemCount=" + (vxReq != null && vxReq.lineItems != null ? vxReq.lineItems.Count.ToString() : "null")
                    + " | CustomerId=" + customer.Id);
                res.TaxRate = 0m;
                return res;
            }

            var token = GetCachedToken();
            if (string.IsNullOrEmpty(token))
            {
                LogError(
                    "Vertex returning 0: auth token unavailable",
                    null,
                    ctxCust,
                    "GetCachedToken returned null or empty. Check VertexAuthHelper logs for the specific auth failure."
                    + " AuthUrl=" + (_vertexSettings != null ? _vertexSettings.AuthUrl : "null")
                    + " | CustomerId=" + customer.Id);
                res.TaxRate = 0m;
                res.Errors.Add("Vertex token unavailable");
                return res;
            }

            var apiUrl = string.IsNullOrWhiteSpace(_vertexSettings != null ? _vertexSettings.ApiUrl : null)
                ? "https://calcconnect.vertexsmb.com/vertex-ws/v2/supplies"
                : _vertexSettings.ApiUrl;

            var correlationId = vxReq != null && !string.IsNullOrWhiteSpace(vxReq.documentNumber)
                ? vxReq.documentNumber
                : Guid.NewGuid().ToString();

            var payload = JsonConvert.SerializeObject(vxReq);

            if (_vertexSettings != null && _vertexSettings.EnableVerboseLogging)
            {
                LogInfo(
                    "Vertex call start cid=" + correlationId
                    + " isShipping=" + isShipping
                    + " taxCategory=" + (request != null ? request.TaxCategoryId.ToString() : "null")
                    + " base=" + baseTaxable.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    ctxCust
                );

                if (_vertexSettings.LogBodies)
                {
                    var safeReq = Sanitize(CloneForLog(vxReq));
                    LogInfo("Vertex request cid=" + correlationId + " body=" + Sample(JsonConvert.SerializeObject(safeReq)), ctxCust);
                }
            }

            var started = DateTime.UtcNow;

            using (var httpReq = new HttpRequestMessage(HttpMethod.Post, apiUrl))
            {
                httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpReq.Headers.Accept.Clear();
                httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpReq.Headers.UserAgent.ParseAdd("NopVertexBridge-3.7");
                httpReq.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);

                httpReq.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                HttpResponseMessage resp;
                string body;

                try
                {
                    // Retry up to 3 attempts with exponential backoff for transient failures.
                    // A transient failure (network hiccup, timeout) should not silently return 0 tax.
                    const int maxAttempts = 3;
                    Exception lastEx = null;
                    resp = null;
                    body = null;

                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        try
                        {
                            // HttpRequestMessage is not reusable — rebuild content each attempt.
                            if (attempt > 1)
                            {
                                httpReq.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                                var delayMs = 300 * (int)Math.Pow(2, attempt - 2); // 300ms, 600ms
                                System.Threading.Thread.Sleep(delayMs);
                            }

                            resp = Http.SendAsync(httpReq).Result;
                            body = resp.Content.ReadAsStringAsync().Result;
                            lastEx = null;
                            break;
                        }
                        catch (Exception attemptEx)
                        {
                            lastEx = attemptEx;
                            LogError(
                                "Vertex HTTP attempt " + attempt + "/" + maxAttempts + " failed cid=" + correlationId,
                                attemptEx,
                                ctxCust);
                        }
                    }

                    if (lastEx != null)
                    {
                        LogError("Vertex HTTP all attempts failed cid=" + correlationId, lastEx, ctxCust);
                        res.TaxRate = 0m;
                        return res;
                    }
                }
                catch (Exception sendEx)
                {
                    LogError("Vertex HTTP send/read exception cid=" + correlationId, sendEx, ctxCust);
                    res.TaxRate = 0m;
                    return res;
                }

                var elapsed = (int)(DateTime.UtcNow - started).TotalMilliseconds;

                if (_vertexSettings != null && _vertexSettings.EnableVerboseLogging)
                {
                    LogInfo(
                        "Vertex completed cid=" + correlationId
                        + " status=" + (int)resp.StatusCode
                        + " elapsedMs=" + elapsed,
                        ctxCust
                    );

                    if (_vertexSettings.LogBodies)
                        LogInfo("Vertex response cid=" + correlationId + " body=" + Sample(body), ctxCust);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var errMsg = ExtractVertexError(body);
                    if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                        InvalidateTokenCache();

                    LogError(
                        "Vertex API error " + (int)resp.StatusCode + " cid=" + correlationId,
                        null,
                        ctxCust,
                        "HTTP Status=" + (int)resp.StatusCode
                        + " | VertexError=" + (string.IsNullOrEmpty(errMsg) ? "[none]" : errMsg)
                        + " | ResponseBody=" + Sample(body));

                    res.TaxRate = 0m;
                    res.Errors.Add("Vertex error " + (int)resp.StatusCode + (string.IsNullOrEmpty(errMsg) ? "" : " " + errMsg));
                    return res;
                }

                var totalTax = ParseTotalTax(body);
                var pct = totalTax > 0m ? Math.Round((totalTax / baseTaxable) * 100m, 6) : 0m;

                res.TaxRate = pct;

                WritePerRequest(cacheKey, pct);
                WriteSession(cacheKey, pct, 15);

                return res;
            }
        }
        catch (Exception ex)
        {
            LogError("Vertex exception in GetTaxRate", ex, ctxCust);
            res.TaxRate = 0m;
            return res;
        }
        finally
        {
            ClearReentrant();
        }
    }

    public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
    {
        actionName = null;
        controllerName = null;
        routeValues = null;
    }

    public override void Install()
    {
        var settings = new VertexSettings
        {
            ClientId = "YOUR_DEFAULT_CLIENT_ID",
            ClientSecret = "YOUR_DEFAULT_CLIENT_SECRET",
            AuthUrl = "https://tokenguard.vertexcloud.com/cached/oauth/token",
            TokenUrl = "https://auth.vertexcloud.com/oauth/token",
            ApiUrl = "https://calcconnect.vertexsmb.com/vertex-ws/v2/supplies",
            GrantType = "client_credentials",
            Audience = "verx://migration-api",
            SellerCode = "Gilson",
            SellerStreet1 = "7975 N Central Dr",
            SellerCity = "Lewis Center",
            SellerState = "OH",
            SellerPostal = "43035",
            SellerCountry = "USA",
            EnableVerboseLogging = false,
            LogBodies = false,
            LogPiiCityStateZip = false,
            BodySampleBytes = 2000,
            EcwClientId = "YOUR_DEFAULT_CLIENT_ID",
            EcwClientSecret = "YOUR_DEFAULT_CLIENT_SECRET",
            PartitionUuid = "YOUR_DEFAULT_PartitionUuid",
        };

        _settingService.SaveSetting(settings);
        base.Install();
    }

    public override void Uninstall()
    {
        _settingService.DeleteSetting<VertexSettings>();
        base.Uninstall();
    }

    private bool IsShippingTaxRequest(CalculateTaxRequest request)
    {
        if (!_taxSettings.ShippingIsTaxable)
            return false;

        return request != null && request.TaxCategoryId == _taxSettings.ShippingTaxClassId;
    }

    private IList<ShoppingCartItem> GetCart(Customer customer)
    {
        if (customer == null)
            return new List<ShoppingCartItem>();

        var storeId = _storeContext.CurrentStore != null ? _storeContext.CurrentStore.Id : 0;

        var items = (customer.ShoppingCartItems ?? new List<ShoppingCartItem>())
            .Where(i =>
                i != null
                && i.ShoppingCartType == ShoppingCartType.ShoppingCart
                && i.Quantity > 0
                && (storeId == 0 || i.StoreId == storeId))
            .ToList();

        return items;
    }

    private Address ResolveShipTo(CalculateTaxRequest request, Customer customer)
    {
        if (request != null && request.Address != null)
            return request.Address;

        if (customer == null)
            return null;

        // In nopCommerce 3.7, customer.ShippingAddress is the actively selected
        // shipping address set during checkout. Prefer this over FirstOrDefault(),
        // which could return a billing address or a stale address from a prior order.
        try
        {
            if (customer.ShippingAddress != null)
                return customer.ShippingAddress;
        }
        catch { }

        // Fall back to first address only if no shipping address is set.
        if (customer.Addresses != null && customer.Addresses.Any())
            return customer.Addresses.FirstOrDefault();

        return null;
    }

    private bool IsAddressComplete(Address addr)
    {
        if (addr == null)
            return false;

        var hasCountry = addr.CountryId.HasValue && addr.CountryId.Value > 0;
        var hasState   = addr.StateProvinceId.HasValue && addr.StateProvinceId.Value > 0;
        var hasPostal  = !string.IsNullOrWhiteSpace(addr.ZipPostalCode);
        var hasCity    = !string.IsNullOrWhiteSpace(addr.City);
        var hasStreet  = !string.IsNullOrWhiteSpace(addr.Address1);

        if (!hasCountry || !hasState)
            return false;

        var country = _countryService.GetCountryById(addr.CountryId.Value);
        var iso2    = country != null ? country.TwoLetterIsoCode : null;

        if (!string.IsNullOrEmpty(iso2) && iso2.Equals("US", StringComparison.OrdinalIgnoreCase))
        {
            // For US addresses, state + zip is the minimum Vertex needs for an accurate
            // tax calculation. Street and city improve accuracy but are not required,
            // allowing tax estimation to work before the customer reaches the full
            // address confirmation step in checkout.
            return hasPostal;
        }

        // Non-US: require at least street plus city or postal.
        return hasStreet && (hasCity || hasPostal);
    }

    private decimal GetShippingTotal(IList<ShoppingCartItem> cart, Customer customer)
    {
        try
        {
            var storeId = _storeContext.CurrentStore != null ? _storeContext.CurrentStore.Id : 0;

            var pickUp = _shippingSettings.AllowPickUpInStore
                         && customer.GetAttribute<bool>(SystemCustomerAttributeNames.SelectedPickUpInStore, storeId);

            if (pickUp)
                return _shippingSettings.PickUpInStoreFee;

            var selected = customer.GetAttribute<ShippingOption>(
                SystemCustomerAttributeNames.SelectedShippingOption,
                _genericAttributeService,
                storeId);

            decimal baseAmount = selected != null && selected.Rate > 0m ? selected.Rate : 0m;

            if (cart != null)
            {
                foreach (var sci in cart)
                {
                    var p = sci.Product;
                    if (p == null) continue;
                    if (p.AdditionalShippingCharge > 0m && sci.Quantity > 0)
                        baseAmount += p.AdditionalShippingCharge * sci.Quantity;
                }
            }

            if (baseAmount < 0m)
                baseAmount = 0m;

            return Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero);
        }
        catch (Exception ex)
        {
            LogWarn("Error in GetShippingTotal " + ex.Message);
            return 0m;
        }
    }

    private decimal GetEffectiveUnitPrice(Product product, Customer customer, int quantity)
    {
        if (product == null) return 0m;
        if (_priceCalculationService == null) return product.Price;

        var qty = quantity > 0 ? quantity : 1;

        try
        {
            var svc = (object)_priceCalculationService;
            var t = svc.GetType();

            var methods = t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(m => m.Name == "GetFinalPrice")
                .ToList();

            foreach (var m in methods)
            {
                try
                {
                    var ps = m.GetParameters();

                    if (ps.Length >= 2 && ps[0].ParameterType.Name == "Product" && ps[1].ParameterType.Name == "Customer")
                    {
                        var args = new object[ps.Length];

                        args[0] = product;
                        args[1] = customer;

                        for (int i = 2; i < ps.Length; i++)
                        {
                            var p = ps[i];

                            if (p.ParameterType == typeof(decimal))
                                args[i] = 0m;
                            else if (p.ParameterType == typeof(bool))
                                args[i] = true;
                            else if (p.ParameterType == typeof(int))
                                args[i] = qty;
                            else if (p.ParameterType.IsValueType)
                                args[i] = Activator.CreateInstance(p.ParameterType);
                            else
                                args[i] = null;
                        }

                        var val = m.Invoke(svc, args);
                        if (val is decimal)
                            return (decimal)val;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return product.Price;
    }

    private decimal GetEffectiveLineExtendedPrice(ShoppingCartItem item, Customer customer)
    {
        if (item == null || item.Product == null) return 0m;

        var qty = item.Quantity > 0 ? item.Quantity : 1;
        var unit = GetEffectiveUnitPrice(item.Product, customer, qty);

        if (unit < 0m) unit = 0m;

        return Math.Round(unit * qty, 4, MidpointRounding.AwayFromZero);
    }

    private VertexSaleRequest BuildVertexRequest(Customer customer, Address shipTo, IList<ShoppingCartItem> items, decimal shippingForThisCall)
    {
        if (shippingForThisCall > 0m && items != null && items.Count > 0)
            items = new List<ShoppingCartItem>();

        var stateAbbrev = "";
        if (shipTo != null && shipTo.StateProvinceId.HasValue && shipTo.StateProvinceId.Value > 0)
        {
            var sp = _stateService.GetStateProvinceById(shipTo.StateProvinceId.Value);
            if (sp != null)
                stateAbbrev = sp.Abbreviation ?? "";
        }

        var countryIso2 = "US";
        if (shipTo != null && shipTo.CountryId.HasValue && shipTo.CountryId.Value > 0)
        {
            var c = _countryService.GetCountryById(shipTo.CountryId.Value);
            if (c != null && !string.IsNullOrEmpty(c.TwoLetterIsoCode))
                countryIso2 = c.TwoLetterIsoCode;
        }

        Func<string, string> Safe = s => string.IsNullOrWhiteSpace(s) ? "" : s;
        Func<string, string> MapCountry = iso2 => string.Equals(iso2, "US", StringComparison.OrdinalIgnoreCase)
            ? "USA"
            : (string.IsNullOrEmpty(iso2) ? "USA" : iso2);

        var sellerCode = string.IsNullOrWhiteSpace(_vertexSettings.SellerCode) ? "Gilson" : _vertexSettings.SellerCode;
        var sellerStreet1 = string.IsNullOrWhiteSpace(_vertexSettings.SellerStreet1) ? "7975 N Central Dr" : _vertexSettings.SellerStreet1;
        var sellerCity = string.IsNullOrWhiteSpace(_vertexSettings.SellerCity) ? "Lewis Center" : _vertexSettings.SellerCity;
        var sellerState = string.IsNullOrWhiteSpace(_vertexSettings.SellerState) ? "OH" : _vertexSettings.SellerState;
        var sellerPostal = string.IsNullOrWhiteSpace(_vertexSettings.SellerPostal) ? "43035" : _vertexSettings.SellerPostal;
        var sellerCountry = string.IsNullOrWhiteSpace(_vertexSettings.SellerCountry) ? "USA" : _vertexSettings.SellerCountry;

        var req = new VertexSaleRequest
        {
            saleMessageType = "QUOTATION",
            documentNumber = Guid.NewGuid().ToString(),
            documentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            transactionType = "SALE",
            seller = new Seller
            {
                company = sellerCode,
                physicalOrigin = new VxAddress
                {
                    streetAddress1 = sellerStreet1,
                    city = sellerCity,
                    mainDivision = sellerState,
                    postalCode = sellerPostal,
                    country = sellerCountry
                }
            },
            customer = new CustomerBlock
            {
                customerCode = new CustomerCode
                {
                    classCode = "Taxable",
                    value = (customer != null && !string.IsNullOrWhiteSpace(customer.GilsonCustomerNumber)
                        ? customer.GilsonCustomerNumber.Trim()
                        : customer.CustomerGuid.ToString())
                },
                destination = new VxAddress
                {
                    // Send null for optional fields that are empty so NullValueHandling.Ignore
                    // omits them from the serialized payload. Vertex rejects empty strings for
                    // streetAddress1 (max 100) and city (max 60) but accepts absent fields fine.
                    streetAddress1 = string.IsNullOrWhiteSpace(shipTo != null ? shipTo.Address1 : null) ? null : shipTo.Address1.Trim(),
                    city           = string.IsNullOrWhiteSpace(shipTo != null ? shipTo.City : null) ? null : shipTo.City.Trim(),
                    mainDivision   = string.IsNullOrWhiteSpace(stateAbbrev) ? null : stateAbbrev,
                    postalCode     = string.IsNullOrWhiteSpace(shipTo != null ? shipTo.ZipPostalCode : null) ? null : shipTo.ZipPostalCode.Trim(),
                    country        = MapCountry(countryIso2)
                }
            },
            lineItems = new List<LineItem>()
        };

        var lineNo = 1;

        if (items != null)
        {
            foreach (var item in items)
            {
                var prod = item.Product;
                if (prod == null) continue;

                var qty = item.Quantity > 0 ? item.Quantity : 1;

                var lineExt = GetEffectiveLineExtendedPrice(item, customer);
                if (lineExt <= 0m)
                    continue;

                req.lineItems.Add(new LineItem
                {
                    lineItemNumber = lineNo++,
                    product = new ProductBlock
                    {
                        productClass = "STANDARD",
                        value = !string.IsNullOrEmpty(prod.Sku) ? prod.Sku : prod.Id.ToString()
                    },
                    quantity = new Quantity { unitOfMeasure = "EA", value = qty },
                    extendedPrice = Math.Round(lineExt, 4, MidpointRounding.AwayFromZero)
                });
            }
        }

        if (shippingForThisCall > 0m)
        {
            var shippingMethodName = "Shipping";

            var selectedShipping = customer.GetAttribute<ShippingOption>(
                SystemCustomerAttributeNames.SelectedShippingOption,
                _genericAttributeService,
                _storeContext.CurrentStore != null ? _storeContext.CurrentStore.Id : 0);

            if (selectedShipping != null && !string.IsNullOrWhiteSpace(selectedShipping.Name))
            {
                var rawName = selectedShipping.Name;

                if (rawName.Contains("+"))
                {
                    var parts = rawName.Split('+');
                    var carriers = new List<string>();

                    foreach (var part in parts)
                    {
                        var cleanPart = System.Text.RegularExpressions.Regex.Replace(part, @"\s*\([^)]*\)", "");
                        cleanPart = cleanPart.Trim();

                        if (!string.IsNullOrWhiteSpace(cleanPart) && cleanPart != "=")
                        {
                            carriers.Add(cleanPart);
                        }
                    }

                    if (carriers.Count > 0)
                    {
                        shippingMethodName = string.Join(" + ", carriers);
                    }
                }
                else
                {
                    shippingMethodName = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s*\([^)]*\)", "").Trim();
                }

                if (shippingMethodName.Length > 40)
                {
                    shippingMethodName = shippingMethodName.Substring(0, 40);
                }

                if (string.IsNullOrWhiteSpace(shippingMethodName))
                {
                    shippingMethodName = "Shipping";
                }
            }

            req.lineItems.Add(new LineItem
            {
                lineItemNumber = lineNo++,
                product = new ProductBlock
                {
                    productClass = "Freight",
                    value = shippingMethodName
                },
                quantity = new Quantity { unitOfMeasure = "EA", value = 1 },
                extendedPrice = Math.Round(shippingForThisCall, 4, MidpointRounding.AwayFromZero)
            });
        }

        return req;
    }

    private static decimal ParseTotalTax(string body)
    {
        if (string.IsNullOrEmpty(body))
            return 0m;

        try
        {
            var jo = JObject.Parse(body);

            // Primary: use data.totalTax if present and non-null.
            var totalToken = jo.SelectToken("data.totalTax");
            if (totalToken != null && totalToken.Type != JTokenType.Null)
            {
                var total = totalToken.Value<decimal>();
                if (total > 0m)
                    return total;
            }

            // Fallback: sum calculatedTax across all line item tax entries.
            // Vertex does not always return data.totalTax depending on the endpoint
            // version or response shape, but always returns per-imposition amounts.
            var lineItems = jo.SelectToken("data.lineItems");
            if (lineItems != null && lineItems.Type == JTokenType.Array)
            {
                decimal sum = 0m;
                foreach (var li in lineItems)
                {
                    var taxes = li.SelectToken("taxes");
                    if (taxes == null || taxes.Type != JTokenType.Array)
                        continue;

                    foreach (var tax in taxes)
                    {
                        var calcTax = tax.SelectToken("calculatedTax");
                        if (calcTax != null && calcTax.Type != JTokenType.Null)
                            sum += calcTax.Value<decimal>();
                    }
                }

                if (sum > 0m)
                    return sum;
            }
        }
        catch
        {
        }

        return 0m;
    }

    private static string ExtractVertexError(string body)
    {
        try
        {
            if (string.IsNullOrEmpty(body)) return null;
            var jo = JObject.Parse(body);
            var msg = jo.SelectToken("errors[0].message");
            return msg != null ? msg.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void InvalidateTokenCache()
    {
        lock (_tokenLock)
        {
            _cachedToken = null;
            _tokenExpiryUtc = DateTime.MinValue;
        }
    }

    private string GetCachedToken()
    {
        lock (_tokenLock)
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryUtc)
                return _cachedToken;

            try
            {
                var newToken = VertexAuthHelper.GetVertexOAuthToken();
                if (!string.IsNullOrEmpty(newToken))
                {
                    _cachedToken = newToken;
                    _tokenExpiryUtc = DateTime.UtcNow.AddMinutes(18);
                }
            }
            catch
            {
                _cachedToken = null;
                _tokenExpiryUtc = DateTime.MinValue;
            }

            return _cachedToken;
        }
    }

    private static bool IsReentrant()
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return false;
        return ctx.Items.Contains(GuardKey);
    }

    private static void MarkReentrant()
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return;
        if (!ctx.Items.Contains(GuardKey))
            ctx.Items[GuardKey] = true;
    }

    private static void ClearReentrant()
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return;
        if (ctx.Items.Contains(GuardKey))
            ctx.Items.Remove(GuardKey);
    }

    private void LogInfo(string shortMessage, Customer customerForLog = null, string detail = null)
    {
        if (_vertexSettings == null || !_vertexSettings.EnableVerboseLogging)
            return;

        try
        {
            var carrier = string.IsNullOrEmpty(detail) ? null : new Exception(detail);
            _logger.Information(shortMessage, carrier, customerForLog);
        }
        catch { }
    }

    // LogWarn is verbose-gated for noisy informational warnings (e.g. cache hits, reentry).
    private void LogWarn(string shortMessage, Customer customerForLog = null, string detail = null)
    {
        if (_vertexSettings == null || !_vertexSettings.EnableVerboseLogging)
            return;

        try
        {
            var carrier = string.IsNullOrEmpty(detail) ? null : new Exception(detail);
            _logger.Warning(shortMessage, carrier, customerForLog);
        }
        catch { }
    }

    // LogWarnAlways writes regardless of EnableVerboseLogging.
    // Use for any zero-return exit that is unexpected or business-critical.
    private void LogWarnAlways(string shortMessage, Customer customerForLog = null, string detail = null)
    {
        try
        {
            var carrier = string.IsNullOrEmpty(detail) ? null : new Exception(detail);
            _logger.Warning(shortMessage, carrier, customerForLog);
        }
        catch { }
    }

    private void LogError(string shortMessage, Exception ex = null, Customer customerForLog = null, string detail = null)
    {
        try
        {
            // If we have supplemental detail but no real exception, use a carrier exception
            // so nopCommerce writes it into the FullMessage column.
            // If we have both, prepend the detail to the real exception's string.
            Exception logEx = ex;
            if (!string.IsNullOrEmpty(detail))
            {
                var combined = detail + (ex != null ? "\r\n\r\n" + ex.ToString() : "");
                logEx = new Exception(combined, ex);
            }

            _logger.Error(shortMessage, logEx, customerForLog);
        }
        catch { }
    }

    private static VertexSaleRequest CloneForLog(VertexSaleRequest req)
    {
        if (req == null) return null;
        try
        {
            var s = JsonConvert.SerializeObject(req);
            return JsonConvert.DeserializeObject<VertexSaleRequest>(s);
        }
        catch
        {
            return req;
        }
    }

    private VertexSaleRequest Sanitize(VertexSaleRequest req)
    {
        if (req == null) return null;
        try
        {
            if (req.customer != null && req.customer.destination != null && !_vertexSettings.LogPiiCityStateZip)
            {
                req.customer.destination.city = null;
                req.customer.destination.mainDivision = null;
                req.customer.destination.postalCode = null;
            }

            if (req.customer != null && req.customer.destination != null)
            {
                req.customer.destination.streetAddress1 = Redact(req.customer.destination.streetAddress1);
                req.customer.destination.streetAddress2 = null;
            }
        }
        catch
        {
        }

        return req;
    }

    private static string Redact(string s, int keep = 2)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length <= keep) return new string('*', s.Length);
        return s.Substring(0, keep) + new string('*', Math.Min(6, s.Length - keep));
    }

    private string Sample(string body)
    {
        if (string.IsNullOrEmpty(body)) return body;
        var max = Math.Max(200, _vertexSettings != null && _vertexSettings.BodySampleBytes > 0 ? _vertexSettings.BodySampleBytes : 2000);
        return body.Length > max ? body.Substring(0, max) + "...(truncated)" : body;
    }

    private sealed class CacheHit
    {
        public decimal RatePct { get; set; }
        public DateTime ExpiresUtc { get; set; }
    }

    private static string SafeStr(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s;
    }

    private string ComputeCartHash(Customer customer, IList<ShoppingCartItem> cart)
    {
        if (cart == null || cart.Count == 0) return "empty";

        unchecked
        {
            int h = 17;

            try
            {
                if (customer != null && customer.CustomerRoles != null)
                {
                    foreach (var rid in customer.CustomerRoles.Where(r => r != null).Select(r => r.Id).OrderBy(x => x))
                    {
                        h = (h * 23) + rid.GetHashCode();
                    }
                }
            }
            catch
            {
            }

            foreach (var sci in cart.OrderBy(x => x.ProductId))
            {
                if (sci == null) continue;

                h = (h * 23) + sci.ProductId.GetHashCode();
                h = (h * 23) + Math.Max(0, sci.Quantity).GetHashCode();

                try
                {
                    var ext = GetEffectiveLineExtendedPrice(sci, customer);
                    var cents = (int)Math.Round(ext * 100m, 0, MidpointRounding.AwayFromZero);
                    h = (h * 23) + cents.GetHashCode();
                }
                catch
                {
                }
            }

            return h.ToString("X");
        }
    }

    private static string ComputeShipOptKey(Customer customer, IGenericAttributeService attrs, int storeId)
    {
        try
        {
            var so = customer.GetAttribute<ShippingOption>(
                SystemCustomerAttributeNames.SelectedShippingOption,
                attrs,
                storeId);

            if (so == null) return "noopt";

            var name = SafeStr(so.Name);
            var rate = so.Rate.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            return name + "|" + rate;
        }
        catch
        {
            return "noopt";
        }
    }

    private string BuildCacheKey(Customer customer, Address addr, bool isShipping, int taxCategoryId, IList<ShoppingCartItem> cart)
    {
        var storeId = _storeContext.CurrentStore != null ? _storeContext.CurrentStore.Id : 0;

        var cust = customer != null && customer.CustomerGuid != Guid.Empty
            ? customer.CustomerGuid.ToString()
            : "guest";

        var dest = string.Format(
                "{0}-{1}-{2}-{3}-{4}",
                addr != null && addr.Id > 0 ? addr.Id.ToString() : "0",
                addr != null && addr.CountryId.HasValue ? addr.CountryId.Value : 0,
                addr != null && addr.StateProvinceId.HasValue ? addr.StateProvinceId.Value : 0,
                SafeStr(addr != null ? addr.ZipPostalCode : ""),
                SafeStr(addr != null ? addr.City : "")
            )
            .ToLowerInvariant();

        var cartHash = ComputeCartHash(customer, cart);
        var shipKey = ComputeShipOptKey(customer, _genericAttributeService, storeId);

        return string.Join("|", new[]
        {
            "vx",
            storeId.ToString(),
            cust,
            isShipping ? "ship" : "item",
            taxCategoryId.ToString(),
            dest,
            shipKey,
            cartHash
        });
    }

    private static decimal? TryReadPerRequest(string key)
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return null;

        if (ctx.Items.Contains(key))
        {
            var valObj = ctx.Items[key];
            if (valObj is decimal)
                return (decimal)valObj;
        }
        return null;
    }

    private static void WritePerRequest(string key, decimal ratePct)
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return;
        ctx.Items[key] = ratePct;
    }

    private static decimal? TryReadSession(string key)
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return null;
        var sess = ctx.Session;
        if (sess == null) return null;

        var hit = sess[key] as CacheHit;
        if (hit == null) return null;

        if (DateTime.UtcNow >= hit.ExpiresUtc)
        {
            sess.Remove(key);
            return null;
        }
        return hit.RatePct;
    }

    private static void WriteSession(string key, decimal ratePct, int ttlSeconds)
    {
        var ctx = HttpContext.Current;
        if (ctx == null) return;
        var sess = ctx.Session;
        if (sess == null) return;

        sess[key] = new CacheHit
        {
            RatePct = ratePct,
            ExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(5, ttlSeconds))
        };
    }
}
}