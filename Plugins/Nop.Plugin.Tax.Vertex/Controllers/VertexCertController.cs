using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Plugin.Tax.Vertex;

namespace Nop.Plugin.Tax.Vertex.Controllers
{
[Authorize]
public class VertexCertController : Controller
{
private readonly IWorkContext _workContext;
private readonly ISettingService _settingService;
private readonly IGenericAttributeService _genericAttributeService;
private readonly ICountryService _countryService;
private readonly IStateProvinceService _stateProvinceService;
private readonly ILogger _logger;

    private static readonly HttpClient _httpClient = new HttpClient();

    public VertexCertController(
        IWorkContext workContext,
        ISettingService settingService,
        IGenericAttributeService genericAttributeService,
        ICountryService countryService,
        IStateProvinceService stateProvinceService,
        ILogger logger)
    {
        _workContext = workContext;
        _settingService = settingService;
        _genericAttributeService = genericAttributeService;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult Index()
    {
        var customer = _workContext.CurrentCustomer;

        var model = new VertexECWModel
        {
            CustomerId = customer != null ? customer.Id : 0,
            CustomerEmail = customer != null ? customer.Email : null
        };

        return View("~/Plugins/Tax.Vertex/Views/VertexCert/Index.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> GetEcwConfig()
    {
        var customer = _workContext.CurrentCustomer;
        if (customer == null || customer.IsGuest())
            return new HttpStatusCodeResult(401);

        var settings = _settingService.LoadSetting<VertexSettings>();
        if (settings == null)
            return new HttpStatusCodeResult(500, "Vertex settings missing");

        var buyerCode = customer.CustomerGuid.ToString();

        var storeId = 0;

        var street1 = customer.GetAttribute<string>(SystemCustomerAttributeNames.StreetAddress, _genericAttributeService, storeId);
        var street2 = customer.GetAttribute<string>(SystemCustomerAttributeNames.StreetAddress2, _genericAttributeService, storeId);
        var city = customer.GetAttribute<string>(SystemCustomerAttributeNames.City, _genericAttributeService, storeId);
        var zip = customer.GetAttribute<string>(SystemCustomerAttributeNames.ZipPostalCode, _genericAttributeService, storeId);
        var phone = customer.GetAttribute<string>(SystemCustomerAttributeNames.Phone, _genericAttributeService, storeId);

        var countryId = customer.GetAttribute<int>(SystemCustomerAttributeNames.CountryId, _genericAttributeService, storeId);
        var stateId = customer.GetAttribute<int>(SystemCustomerAttributeNames.StateProvinceId, _genericAttributeService, storeId);

        var country = countryId > 0 ? _countryService.GetCountryById(countryId) : null;
        var state = stateId > 0 ? _stateProvinceService.GetStateProvinceById(stateId) : null;

        var buyerName = customer.GetAttribute<string>(SystemCustomerAttributeNames.Company, _genericAttributeService, storeId);

        if (string.IsNullOrWhiteSpace(buyerName))
        {
            var first = customer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName, _genericAttributeService, storeId);
            var last = customer.GetAttribute<string>(SystemCustomerAttributeNames.LastName, _genericAttributeService, storeId);
            buyerName = ((first ?? "").Trim() + " " + (last ?? "").Trim()).Trim();
        }

        if (string.IsNullOrWhiteSpace(buyerName))
            buyerName = buyerCode;

        var token = await GetVertexEcwTokenAsync(settings.EcwClientId, settings.EcwClientSecret);
        if (string.IsNullOrWhiteSpace(token))
            return new HttpStatusCodeResult(500, "ECW token failure");

        var overrides = new List<object>
        {
            new { qId = 2, value = buyerName },
            new { qId = 3, value = CombineStreet(street1, street2) },
            new { qId = 4, value = city },
            new { qId = 5, value = state != null ? state.Abbreviation : "" },
            new { qId = 6, value = zip },
            new { qId = 7, value = country != null ? (country.ThreeLetterIsoCode ?? country.TwoLetterIsoCode ?? country.Name) : "" },
            new { qId = 19, value = customer.Email },
            new { qId = 22, value = phone }
        };

        return Json(new
        {
            accessToken = token,
            sellerCode = settings.SellerCode,
            partitionUuid = settings.PartitionUuid,
            buyerCode = buyerCode,
            overrides = overrides
        });
    }

    private async Task<string> GetVertexEcwTokenAsync(string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return null;

        var response = await _httpClient.PostAsync(
            "https://tokenguard.vertexcloud.com/cached/oauth/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("audience", "verx://migration-api"),
                new KeyValuePair<string,string>("client_id", clientId),
                new KeyValuePair<string,string>("client_secret", clientSecret),
                new KeyValuePair<string,string>("grant_type", "client_credentials"),
                new KeyValuePair<string,string>("scope", "vtms-internal-api ecw-wizard-api")
            })
        );

        if (!response.IsSuccessStatusCode)
        {
            _logger.Error("Vertex ECW token request failed: " + response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();

        var marker = "\"access_token\":\"";
        var start = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;

        start += marker.Length;
        var end = json.IndexOf("\"", start);
        if (end <= start) return null;

        return json.Substring(start, end - start);
    }

    private string CombineStreet(string s1, string s2)
    {
        s1 = (s1 ?? "").Trim();
        s2 = (s2 ?? "").Trim();

        if (string.IsNullOrWhiteSpace(s1) && string.IsNullOrWhiteSpace(s2))
            return "";

        if (string.IsNullOrWhiteSpace(s2))
            return s1;

        return s1 + " " + s2;
    }
}

public class VertexECWModel
{
    public int CustomerId { get; set; }
    public string CustomerEmail { get; set; }
}


}