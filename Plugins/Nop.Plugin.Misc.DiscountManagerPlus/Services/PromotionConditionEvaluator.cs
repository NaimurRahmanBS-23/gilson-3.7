// Ported from nopCommerce 4.9 Discount Manager Plus services. C# 6 / nopCommerce 3.70.
using System.Linq;
using System.Collections.Generic;
using System;
using System.Web;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{

    public class PromotionConditionEvaluator : IPromotionConditionEvaluator
    {
        private readonly ICategoryService _categoryService;
        private readonly ICustomerService _customerService;
        
        private readonly IManufacturerService _manufacturerService;
        private readonly IOrderService _orderService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly IPromotionEvaluationContextFactory _promotionEvaluationContextFactory;
        private readonly ICacheManager _cacheManager;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ISpecificationAttributeService _specificationAttributeService;

        public PromotionConditionEvaluator(
            ICategoryService categoryService,
            ICustomerService customerService,
            IManufacturerService manufacturerService,
            IOrderService orderService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IPromotionEvaluationContextFactory promotionEvaluationContextFactory,
            ICacheManager cacheManager,
            IPriceCalculationService priceCalculationService,
            ISpecificationAttributeService specificationAttributeService)
        {
            _categoryService = categoryService;
            _customerService = customerService;
            _manufacturerService = manufacturerService;
            _orderService = orderService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _promotionEvaluationContextFactory = promotionEvaluationContextFactory;
            _cacheManager = cacheManager;
            _priceCalculationService = priceCalculationService;
            _specificationAttributeService = specificationAttributeService;
        }

        public bool EvaluateRuleConditions(
            PromotionRuleType ruleType,
            IList<PromotionRuleCondition> conditions,
            IList<ShoppingCartItem> cart,
            bool defaultWhenNoConditions,
            int storeId,
            PromotionEvaluationContext context = null)
        {
            return EvaluateRuleConditionsImpl(ruleType, conditions, cart, defaultWhenNoConditions, storeId, context);
        }

        public ConditionMatchResult EvaluateRuleConditionsWithQuantity(
            PromotionRuleType ruleType,
            IList<PromotionRuleCondition> conditions,
            IList<ShoppingCartItem> cart,
            bool defaultWhenNoConditions,
            int storeId,
            PromotionEvaluationContext context = null)
        {
            return EvaluateRuleConditionsWithQuantityImpl(ruleType, conditions, cart, defaultWhenNoConditions, storeId, context);
        }

        public IList<ShoppingCartItem> GetMatchedCartItemsByRuleProducts(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts)
        {
            return GetMatchedCartItemsByRuleProductsImpl(cart, ruleProducts);
        }

        public IList<PromotionRuleProduct> GetQualifiedProductBasedRuleProducts(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts)
        {
            return GetQualifiedProductBasedRuleProductsImpl(cart, ruleProducts);
        }

        public ComboSetCountResult GetComboSetCountAndSubtotal(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> buyProducts)
        {
            return GetComboSetCountAndSubtotalImpl(cart, buyProducts);
        }

        public ComboSetMatchSummary GetComboSetMatchSummary(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> buyProducts)
        {
            return GetComboSetMatchSummaryImpl(cart, buyProducts);
        }

        public bool HasSpecificCartConditionQuantityCriteria(IList<PromotionRuleCondition> conditions)
        {
            return conditions.Any(condition =>
            {
                if (condition.RequiredProductId.GetValueOrDefault() > 0 ||
                    condition.RequiredCategoryId.GetValueOrDefault() > 0 ||
                    condition.RequiredVendorId.GetValueOrDefault() > 0 ||
                    condition.RequireSameLineMatch)
                {
                    return true;
                }

                if (condition.ConditionSourceTypeId <= 0 || string.IsNullOrWhiteSpace(condition.ConditionSourceData))
                    return false;

                var sourceType = Enum.IsDefined(typeof(ConditionSourceType), condition.ConditionSourceTypeId)
                    ? condition.ConditionSourceType
                    : ConditionSourceType.Products;
                if (IsSessionSourceType(sourceType))
                    return false;

                var restrictionType = Enum.IsDefined(typeof(ConditionRestrictionType), condition.ConditionRestrictionTypeId)
                    ? condition.ConditionRestrictionType
                    : ConditionRestrictionType.Include;

                return restrictionType != ConditionRestrictionType.Exclude;
            });
        }

        public PromotionRuleTier GetMatchingTierByMetric(IList<PromotionRuleTier> tiers, decimal metric)
        {
            if (tiers == null || !tiers.Any())
                return null;

            return tiers
                .Where(t => metric >= t.MinQuantity && (t.MaxQuantity == 0 || metric <= t.MaxQuantity))
                .OrderByDescending(t => t.MinQuantity)
                .FirstOrDefault();
        }

        public IList<PromotionRuleTier> FilterTiersByMatchedRuleProducts(
            IList<PromotionRuleTier> tiers,
            IList<PromotionRuleTierProductMapping> tierMappings,
            IList<int> matchedRuleProductIds)
        {
            if (tiers == null || !tiers.Any())
                return new PromotionRuleTier[0];

            if (tierMappings == null || !tierMappings.Any())
                return tiers.ToList();

            var tierIdsWithMappings = tierMappings
                .Select(x => x.PromotionRuleTierId)
                .ToList();
            if (!tierIdsWithMappings.Any())
                return tiers.ToList();

            var matchedRuleProductIdSet = matchedRuleProductIds?
                .Where(x => x > 0)
                .ToList();
            if (!matchedRuleProductIdSet.Any())
                return new PromotionRuleTier[0];

            var mappedTierIdsForMatchedProducts = tierMappings
                .Where(x => matchedRuleProductIdSet.Contains(x.PromotionRuleProductId))
                .Select(x => x.PromotionRuleTierId)
                .ToList();

            var unmappedTierIds = tiers
                .Select(x => x.Id)
                .Where(x => !tierIdsWithMappings.Contains(x))
                .ToList();

            return tiers
                .Where(x => mappedTierIdsForMatchedProducts.Contains(x.Id) || unmappedTierIds.Contains(x.Id))
                .ToList();
        }

        public IList<PromotionRuleProduct> FilterRuleProductsByTierMapping(
            IList<PromotionRuleProduct> ruleProducts,
            PromotionRuleTier tier,
            IList<PromotionRuleTierProductMapping> tierMappings)
        {
            if (ruleProducts == null || !ruleProducts.Any())
                return new PromotionRuleProduct[0];

            if (tier == null || tierMappings == null || !tierMappings.Any())
                return ruleProducts.ToList();

            var mappedRuleProductIds = tierMappings
                .Where(x => x.PromotionRuleTierId == tier.Id)
                .Select(x => x.PromotionRuleProductId)
                .ToList();
            if (!mappedRuleProductIds.Any())
                return ruleProducts.ToList();

            return ruleProducts
                .Where(x => mappedRuleProductIds.Contains(x.Id))
                .ToList();
        }

        private sealed class ConditionSourceEntry
        {
            public int Id { get; set; }
            public int? MinQuantity { get; set; }
            public int? MaxQuantity { get; set; }
            public HashSet<int> OptionIds { get; set; }
            public HashSet<string> OptionNames { get; set; }
            public ConditionSourceEntry()
            {
                OptionIds = new HashSet<int>();
                OptionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            public bool HasQuantityRange => MinQuantity.HasValue || MaxQuantity.HasValue;
            public bool HasOptions => OptionIds.Count > 0 || OptionNames.Count > 0;
        }

        private static bool IsSessionSourceType(ConditionSourceType sourceType)
        {
            return sourceType == ConditionSourceType.DeviceType ||
                   sourceType == ConditionSourceType.SalesChannel ||
                   sourceType == ConditionSourceType.CampaignSource ||
                   sourceType == ConditionSourceType.ReferralSource;
        }

        private static IList<string> ParseSessionSourceEntries(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new string[0];

            return raw
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeToken)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().Trim('"', '\'').ToLowerInvariant();
        }

        private static string DetectDeviceType(string userAgent)
        {
            var ua = NormalizeToken(userAgent);
            if (string.IsNullOrWhiteSpace(ua))
                return "desktop";

            if (ua.IndexOf("ipad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("tablet", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "tablet";
            }

            if (ua.IndexOf("mobi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("android", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("iphone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "mobile";
            }

            return "desktop";
        }

        private static string DetectSalesChannel(string userAgent, string channelHint)
        {
            var channel = NormalizeToken(channelHint);
            if (channel == "app" || channel == "mobileapp" || channel == "mobile-app")
                return "app";
            if (channel == "web" || channel == "website" || channel == "storefront")
                return "web";

            var ua = NormalizeToken(userAgent);
            if (ua.IndexOf("okhttp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("cfnetwork", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("mobileapp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("xamarin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("reactnative", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ua.IndexOf("dart/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "app";
            }

            return "web";
        }

        private static bool ValueMatchesToken(string value, string token)
        {
            var normalizedValue = NormalizeToken(value);
            var normalizedToken = NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedValue) || string.IsNullOrWhiteSpace(normalizedToken))
                return false;

            return normalizedValue.Equals(normalizedToken, StringComparison.OrdinalIgnoreCase) ||
                   normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetReferralHost(string referer)
        {
            Uri uri;
            if (!Uri.TryCreate(referer, UriKind.Absolute, out uri))
                return string.Empty;

            return uri.Host ?? string.Empty;
        }

        private static IList<ConditionSourceEntry> ParseConditionSourceEntries(string raw)
        {
            int id;
            int minQty;
            int maxQty;
            int minOnly;
            int optionId;
            var entries = new List<ConditionSourceEntry>();
            if (string.IsNullOrWhiteSpace(raw))
                return entries;

            foreach (var token in SplitOutsideParentheses(raw))
            {
                var text = token?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var colonIndex = IndexOfOutsideParentheses(text, ':');
                var rangePart = colonIndex >= 0 ? text.Substring(colonIndex + 1).Trim() : string.Empty;
                var mainPart = colonIndex >= 0 ? text.Substring(0, colonIndex).Trim() : text;

                var openIndex = mainPart.IndexOf('(');
                var closeIndex = mainPart.LastIndexOf(')');
                var idPart = mainPart;
                var optionsPart = string.Empty;
                if (openIndex >= 0 && closeIndex > openIndex)
                {
                    idPart = mainPart.Substring(0, openIndex).Trim();
                    optionsPart = mainPart.Substring(openIndex + 1, closeIndex - openIndex - 1);
                }

                if (!int.TryParse(idPart, out id) || id < 0)
                    continue;

                var entry = new ConditionSourceEntry { Id = id };

                if (!string.IsNullOrWhiteSpace(rangePart))
                {
                    var rangeSplit = rangePart.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (rangeSplit.Length == 2)
                    {
                        if (int.TryParse(rangeSplit[0], out minQty) && minQty >= 0)
                            entry.MinQuantity = minQty;
                        if (int.TryParse(rangeSplit[1], out maxQty) && maxQty >= 0)
                            entry.MaxQuantity = maxQty;
                    }
                    else if (int.TryParse(rangePart, out minOnly) && minOnly >= 0)
                    {
                        entry.MinQuantity = minOnly;
                    }
                }

                if (!string.IsNullOrWhiteSpace(optionsPart))
                {
                    foreach (var option in optionsPart.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(option, out optionId) && optionId > 0)
                            entry.OptionIds.Add(optionId);
                        else if (!string.IsNullOrWhiteSpace(option))
                            entry.OptionNames.Add(option);
                    }
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static IList<string> SplitOutsideParentheses(string input)
        {
            var parts = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
                return parts;

            var current = new System.Text.StringBuilder();
            var depth = 0;
            foreach (var ch in input)
            {
                if (ch == '(')
                    depth++;
                if (ch == ')')
                    depth = Math.Max(0, depth - 1);

                if (ch == ',' && depth == 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts;
        }

        private static int IndexOfOutsideParentheses(string input, char token)
        {
            if (string.IsNullOrEmpty(input))
                return -1;

            var depth = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '(')
                    depth++;
                else if (input[i] == ')')
                    depth = Math.Max(0, depth - 1);

                if (input[i] == token && depth == 0)
                    return i;
            }

            return -1;
        }

        private static IList<int> ParseIdList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new int[0];

            var ids = new List<int>();
            foreach (var token in raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int parsed;
                if (int.TryParse(token, out parsed) && parsed > 0)
                    ids.Add(parsed);
            }
            return ids.Distinct().ToList();
        }

        private static IList<string> ParseTokenList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new string[0];

            return raw
                .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool ContainsAllowCountryFallbackToken(IList<string> countryTokens)
        {
            return countryTokens.Any(x => x.Equals("*", StringComparison.OrdinalIgnoreCase) ||
                                          x.Equals("ALLOW_EMPTY", StringComparison.OrdinalIgnoreCase) ||
                                          x.Equals("FALLBACK_ALLOW", StringComparison.OrdinalIgnoreCase));
        }

        private bool EvaluateSessionSourceCondition(PromotionRuleCondition condition, ConditionSourceType sourceType)
        {
            var entries = ParseSessionSourceEntries(condition.ConditionSourceData ?? string.Empty);
            if (!entries.Any())
                return false;

            var restrictionType = Enum.IsDefined(typeof(ConditionRestrictionType), condition.ConditionRestrictionTypeId)
                ? condition.ConditionRestrictionType
                : ConditionRestrictionType.Include;

            var request = HttpContext.Current != null ? HttpContext.Current.Request : null;
            var userAgent = request != null ? (request.UserAgent ?? string.Empty) : string.Empty;
            var referer = (request != null && request.UrlReferrer != null) ? request.UrlReferrer.ToString() : string.Empty;
            var channelHint = request != null ? request.Headers["X-Channel"] : null;
            if (string.IsNullOrWhiteSpace(channelHint))
                channelHint = request != null ? request.Headers["X-Client-Channel"] : null;
            if (string.IsNullOrWhiteSpace(channelHint))
                channelHint = request != null ? request.QueryString["channel"] : null;

            var campaignSource = request != null ? request.QueryString["utm_campaign"] : null;
            if (string.IsNullOrWhiteSpace(campaignSource))
                campaignSource = request != null ? request.QueryString["campaign"] : null;
            if (string.IsNullOrWhiteSpace(campaignSource))
                campaignSource = request != null ? request.Headers["X-Campaign"] : null;
            if (string.IsNullOrWhiteSpace(campaignSource))
                campaignSource = request != null ? request.QueryString["utm_source"] : null;

            var referralSource = request != null ? request.QueryString["ref"] : null;
            if (string.IsNullOrWhiteSpace(referralSource))
                referralSource = request != null ? request.QueryString["referral"] : null;
            if (string.IsNullOrWhiteSpace(referralSource))
                referralSource = request != null ? request.QueryString["source"] : null;
            if (string.IsNullOrWhiteSpace(referralSource))
                referralSource = referer;

            string value;
            switch (sourceType)
            {
                case ConditionSourceType.DeviceType:
                    value = DetectDeviceType(userAgent);
                    break;
                case ConditionSourceType.SalesChannel:
                    value = DetectSalesChannel(userAgent, channelHint ?? string.Empty);
                    break;
                case ConditionSourceType.CampaignSource:
                    value = campaignSource ?? string.Empty;
                    break;
                case ConditionSourceType.ReferralSource:
                    value = referralSource ?? string.Empty;
                    break;
                default:
                    value = string.Empty;
                    break;
            }

            var referralHost = sourceType == ConditionSourceType.ReferralSource
                ? GetReferralHost(referralSource ?? string.Empty)
                : string.Empty;

            var matchedCount = entries.Count(entry =>
                ValueMatchesToken(value, entry) ||
                (sourceType == ConditionSourceType.ReferralSource && ValueMatchesToken(referralHost, entry)));

            var isMatched = matchedCount > 0;
            var hasTotalRange = condition.QuantityMin.HasValue || condition.QuantityMax.HasValue;
            if (hasTotalRange)
                isMatched = isMatched && IsQuantityInRange(matchedCount, condition.QuantityMin, condition.QuantityMax);

            return restrictionType == ConditionRestrictionType.Exclude ? !isMatched : isMatched;
        }

        private string GetProductCategoryIdsCacheKey(int productId)
        {
            return string.Format(DiscountManagerPlusDefaults.ProductCategoryIdsByProductCacheKey, productId);
        }

        private string GetProductManufacturerIdsCacheKey(int productId)
        {
            return string.Format(DiscountManagerPlusDefaults.ProductManufacturerIdsByProductCacheKey, productId);
        }

        private string GetProductSpecificationOptionIdsCacheKey(int productId)
        {
            return string.Format(DiscountManagerPlusDefaults.ProductSpecificationOptionIdsByProductCacheKey, productId);
        }

        private string GetCartAttributeValuesCacheKey(int shoppingCartItemId)
        {
            return string.Format(DiscountManagerPlusDefaults.CartAttributeValuesByCartItemCacheKey, shoppingCartItemId);
        }

        private string GetAttributeValueIdsByMappingCacheKey(int mappingId)
        {
            return string.Format(DiscountManagerPlusDefaults.AttributeValueIdsByMappingCacheKey, mappingId);
        }

        private HashSet<int> GetProductCategoryIds(int productId, Dictionary<int, HashSet<int>> cache)
        {
            if (productId <= 0)
                return new HashSet<int>();

            HashSet<int> cached;
            if (cache.TryGetValue(productId, out cached))
                return cached;

            var categoryIds = _cacheManager.Get(GetProductCategoryIdsCacheKey(productId), () => new HashSet<int>(_categoryService.GetProductCategoriesByProductId(productId, true).Select(x => x.CategoryId)));

            cache[productId] = categoryIds;
            return categoryIds;
        }

        private bool IsProductMatchedByRuleCategory(
            int productId,
            int ruleCategoryId,
            Dictionary<int, HashSet<int>> productCategoryIdsCache)
        {
            if (productId <= 0 || ruleCategoryId <= 0)
                return false;

            var productCategoryIds = GetProductCategoryIds(productId, productCategoryIdsCache);
            if (!productCategoryIds.Any())
                return false;

            var childCategories = _categoryService.GetAllCategoriesByParentCategoryId(ruleCategoryId, true, true);
            var matchedCategoryIds = new HashSet<int>(childCategories.Select(c => c.Id));
            matchedCategoryIds.Add(ruleCategoryId);

            return productCategoryIds.Overlaps(matchedCategoryIds);
        }

        private HashSet<int> GetProductManufacturerIds(int productId, Dictionary<int, HashSet<int>> cache)
        {
            if (productId <= 0)
                return new HashSet<int>();

            HashSet<int> cached;
            if (cache.TryGetValue(productId, out cached))
                return cached;

            var manufacturerIds = _cacheManager.Get(GetProductManufacturerIdsCacheKey(productId), () => new HashSet<int>(_manufacturerService.GetProductManufacturersByProductId(productId, true).Select(x => x.ManufacturerId)));

            cache[productId] = manufacturerIds;
            return manufacturerIds;
        }

        private HashSet<int> GetProductSpecificationOptionIds(int productId, Dictionary<int, HashSet<int>> cache)
        {
            if (productId <= 0)
                return new HashSet<int>();

            HashSet<int> cached;
            if (cache.TryGetValue(productId, out cached))
                return cached;

            var optionIds = _cacheManager.Get(GetProductSpecificationOptionIdsCacheKey(productId), () => new HashSet<int>(_specificationAttributeService.GetProductSpecificationAttributes(productId).Select(x => x.SpecificationAttributeOptionId)));

            cache[productId] = optionIds;
            return optionIds;
        }

        private IList<ProductAttributeValue> GetCartItemAttributeValues(
            ShoppingCartItem cartItem,
            Dictionary<int, IList<ProductAttributeValue>> cache)
        {
            if (cartItem == null)
                return new ProductAttributeValue[0];

            IList<ProductAttributeValue> cachedValues;
            if (cache.TryGetValue(cartItem.Id, out cachedValues))
                return cachedValues;

            if (string.IsNullOrWhiteSpace(cartItem.AttributesXml))
            {
                cache[cartItem.Id] = new ProductAttributeValue[0];
                return cache[cartItem.Id];
            }

            var values = _cacheManager.Get(
                GetCartAttributeValuesCacheKey(cartItem.Id),
                () => _productAttributeParser.ParseProductAttributeValues(cartItem.AttributesXml));
            cache[cartItem.Id] = values ?? new ProductAttributeValue[0];
            return cache[cartItem.Id];
        }

        private bool EvaluateRuleConditionsImpl(
            PromotionRuleType ruleType,
            IList<PromotionRuleCondition> conditions,
            IList<ShoppingCartItem> cart,
            bool defaultWhenNoConditions,
            int storeId,
            PromotionEvaluationContext context)
        {
            var result = EvaluateRuleConditionsWithQuantityImpl(ruleType, conditions, cart, defaultWhenNoConditions, storeId, context);
            return result.IsMatched;
        }

        private ConditionMatchResult EvaluateRuleConditionsWithQuantityImpl(
            PromotionRuleType ruleType,
            IList<PromotionRuleCondition> conditions,
            IList<ShoppingCartItem> cart,
            bool defaultWhenNoConditions,
            int storeId,
            PromotionEvaluationContext context)
        {
            if (!conditions.Any())
                return CreateConditionMatchResult(defaultWhenNoConditions);

            var cartProductIds = new HashSet<int>(cart.Select(x => x.ProductId));
            var productCache = new Dictionary<int, Product>();
            var categoryIdsCache = new Dictionary<int, HashSet<int>>();
            var manufacturerIdsCache = new Dictionary<int, HashSet<int>>();
            var cartAttributeValuesCache = new Dictionary<int, IList<ProductAttributeValue>>();
            var productSpecOptionIdsCache = new Dictionary<int, HashSet<int>>();
            var specOptionCache = new Dictionary<int, SpecificationAttributeOption>();
            var attributeMappingCache = new Dictionary<int, ProductAttributeMapping>();
            var subtotal = ruleType == PromotionRuleType.SubtotalBased || conditions.Any(x => x.ConditionOperatorId > 0)
                ? GetCartSubtotal(cart)
                : 0m;

            if (context == null)
            context = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);

            Customer customer = null;
            var customerId = context.CustomerId > 0 ? context.CustomerId : cart.FirstOrDefault()?.CustomerId ?? 0;
            if (context.CustomerId <= 0)
                context.CustomerId = customerId;

            if (customerId > 0)
                customer = _customerService.GetCustomerById(customerId);

            var customerRoleIds = customer != null ? customer.GetCustomerRoleIds(true) : new int[0];
            var paidCompletedOrderCount = 0;
            if (customer != null)
            {
                var paidOrders = _orderService.SearchOrders(storeId, 0, customer.Id, 0, 0, 0, 0, null, null, null, null, PaymentStatus.Paid, null, null, "", null, null, 0, 1);

                var completedOrders = _orderService.SearchOrders(storeId, 0, customer.Id, 0, 0, 0, 0, null, null, null, OrderStatus.Complete, null, null, null, "", null, null, 0, 1);

                paidCompletedOrderCount = Math.Max(paidOrders.TotalCount, completedOrders.TotalCount);
            }

            var isFirstOrder = customer != null && paidCompletedOrderCount == 0;
            var isGuestCustomer = customer != null && customer.IsGuest();
            var isNewCustomer = customer != null && !isGuestCustomer && customer.CreatedOnUtc >= context.CurrentUtc.AddDays(-30);

            var groupedConditions = conditions
                .OrderBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
                .ThenBy(x => x.Id)
                .GroupBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup);

            var hasAnyGroup = false;
            foreach (var group in groupedConditions)
            {
                var groupConditions = group.OrderBy(x => x.Id).ToList();
                var groupLookup = groupConditions.ToDictionary(x => x.Id);
                var childrenByParent = groupConditions
                    .Where(x => x.ParentConditionId.HasValue && groupLookup.ContainsKey(x.ParentConditionId.Value))
                    .GroupBy(x => x.ParentConditionId.Value)
                    .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).ToList());

                var rootConditions = groupConditions
                    .Where(x => !x.ParentConditionId.HasValue || !groupLookup.ContainsKey(x.ParentConditionId.Value))
                    .OrderBy(x => x.Id)
                    .ToList();
                if (!rootConditions.Any() && groupConditions.Any())
                    rootConditions = groupConditions;

                ConditionMatchResult groupResult = null;
                var hasConditionInGroup = false;
                foreach (var rootCondition in rootConditions)
                {
                    var conditionResult = EvaluateConditionTreeWithQuantity(
                        rootCondition,
                        childrenByParent,
                        ruleType,
                        cart,
                        cartProductIds,
                        productCache,
                        categoryIdsCache,
                        manufacturerIdsCache,
                        productSpecOptionIdsCache,
                        specOptionCache,
                        cartAttributeValuesCache,
                        attributeMappingCache,
                        customer,
                        customerRoleIds,
                        isFirstOrder,
                        isNewCustomer,
                        subtotal,
                        paidCompletedOrderCount,
                        context,
                        new HashSet<int>());

                    if (!hasConditionInGroup)
                    {
                        groupResult = conditionResult;
                        hasConditionInGroup = true;
                        continue;
                    }

                    var logicalOperator = Enum.IsDefined(typeof(ConditionLogicalOperator), rootCondition.LogicalOperatorId)
                        ? rootCondition.LogicalOperator
                        : ConditionLogicalOperator.And;

                    groupResult = CombineConditionMatchResults(groupResult, conditionResult, logicalOperator);
                }

                if (!hasConditionInGroup)
                    continue;

                hasAnyGroup = true;
                if (groupResult != null && groupResult.IsMatched)
                    return groupResult;
            }

            return CreateConditionMatchResult(!hasAnyGroup && defaultWhenNoConditions);
        }

        private IList<ShoppingCartItem> GetMatchedCartItemsByRuleProductsImpl(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts)
        {
            var matchedItems = new List<ShoppingCartItem>();
            if (!cart.Any() || !ruleProducts.Any())
                return matchedItems;

            var productCache = new Dictionary<int, Product>();
            var categoryIdsCache = new Dictionary<int, HashSet<int>>();
            var manufacturerIdsCache = new Dictionary<int, HashSet<int>>();
            var cartAttributeValuesCache = new Dictionary<int, IList<ProductAttributeValue>>();
            var attributeValueCache = new Dictionary<int, ProductAttributeValue>();
            var mappingAllValueIdsCache = new Dictionary<int, HashSet<int>>();

            foreach (var cartItem in cart)
            {
                var isMatched = false;
                foreach (var ruleProduct in ruleProducts)
                {
                    if (IsCartItemMatchedByRuleProduct(cartItem, ruleProduct, productCache, categoryIdsCache, manufacturerIdsCache, cartAttributeValuesCache, attributeValueCache, mappingAllValueIdsCache))
                    {
                        isMatched = true;
                        break;
                    }
                }

                if (isMatched)
                    matchedItems.Add(cartItem);
            }

            return matchedItems;
        }

        private IList<PromotionRuleProduct> GetQualifiedProductBasedRuleProductsImpl(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts)
        {
            var qualifiedRuleProducts = new List<PromotionRuleProduct>();
            if (cart == null || !cart.Any() || ruleProducts == null || !ruleProducts.Any())
                return qualifiedRuleProducts;

            var productCache = new Dictionary<int, Product>();
            var categoryIdsCache = new Dictionary<int, HashSet<int>>();
            var manufacturerIdsCache = new Dictionary<int, HashSet<int>>();
            var cartAttributeValuesCache = new Dictionary<int, IList<ProductAttributeValue>>();
            var attributeValueCache = new Dictionary<int, ProductAttributeValue>();
            var mappingAllValueIdsCache = new Dictionary<int, HashSet<int>>();

            foreach (var ruleProduct in ruleProducts)
            {
                if (ruleProduct.IsAllProducts)
                {
                    qualifiedRuleProducts.Add(ruleProduct);
                    continue;
                }

                var matchedQuantity = GetMatchedQuantityForRuleProduct(
                    cart,
                    ruleProduct,
                    productCache,
                    categoryIdsCache,
                    manufacturerIdsCache,
                    cartAttributeValuesCache,
                    attributeValueCache,
                    mappingAllValueIdsCache);

                var requiredQuantity = ruleProduct.MinQuantity > 0 ? ruleProduct.MinQuantity : 1;
                if (matchedQuantity < requiredQuantity)
                    continue;

                if (ruleProduct.MaxQuantity > 0 && matchedQuantity > ruleProduct.MaxQuantity)
                    continue;

                qualifiedRuleProducts.Add(ruleProduct);
            }

            return qualifiedRuleProducts;
        }

        private ComboSetCountResult GetComboSetCountAndSubtotalImpl(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> buyProducts)
        {
            var summary = GetComboSetMatchSummaryImpl(cart, buyProducts);
            return new ComboSetCountResult(summary.SetCount, summary.MatchedSubtotal);
        }

        private ComboSetMatchSummary GetComboSetMatchSummaryImpl(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> buyProducts)
        {
            if (!cart.Any() || !buyProducts.Any())
                return new ComboSetMatchSummary();

            var groupedBuyProducts = buyProducts
                .GroupBy(x => new { x.ProductId, x.CategoryId, x.ManufacturerId, x.VendorId })
                .Select(g => new PromotionRuleProduct
                {
                    ProductId = g.Key.ProductId,
                    CategoryId = g.Key.CategoryId,
                    ManufacturerId = g.Key.ManufacturerId,
                    VendorId = g.Key.VendorId,
                    MinQuantity = g.Sum(x => x.MinQuantity > 0 ? x.MinQuantity : 1)
                })
                .ToList();

            if (!groupedBuyProducts.Any())
                return new ComboSetMatchSummary();

            var cartLines = cart.Where(x => x.Quantity > 0).ToList();
            if (!cartLines.Any())
                return new ComboSetMatchSummary();

            var unitPrices = new decimal[cartLines.Count];
            var remainingQuantities = new int[cartLines.Count];
            for (var i = 0; i < cartLines.Count; i++)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(cartLines[i], false);
                unitPrices[i] = unitPrice < 0 ? 0 : unitPrice;
                remainingQuantities[i] = cartLines[i].Quantity;
            }

            var productCache = new Dictionary<int, Product>();
            var categoryIdsCache = new Dictionary<int, HashSet<int>>();
            var manufacturerIdsCache = new Dictionary<int, HashSet<int>>();
            var cartAttributeValuesCache = new Dictionary<int, IList<ProductAttributeValue>>();
            var attributeValueCache = new Dictionary<int, ProductAttributeValue>();
            var mappingAllValueIdsCache = new Dictionary<int, HashSet<int>>();
            var requirementMatches = new Dictionary<int, List<int>>();

            for (var requirementIndex = 0; requirementIndex < groupedBuyProducts.Count; requirementIndex++)
            {
                var ruleProduct = groupedBuyProducts[requirementIndex];
                var matchedLineIndexes = new List<int>();

                for (var lineIndex = 0; lineIndex < cartLines.Count; lineIndex++)
                {
                    if (IsCartItemMatchedByRuleProduct(cartLines[lineIndex], ruleProduct, productCache, categoryIdsCache, manufacturerIdsCache, cartAttributeValuesCache, attributeValueCache, mappingAllValueIdsCache))
                        matchedLineIndexes.Add(lineIndex);
                }

                if (!matchedLineIndexes.Any())
                    return new ComboSetMatchSummary();

                requirementMatches[requirementIndex] = matchedLineIndexes;
            }

            var setCount = 0;
            var matchedSubtotal = 0m;
            var quantitiesByLineId = new Dictionary<int, int>();

            while (true)
            {
                var consumedInSet = new Dictionary<int, int>();
                var setSubtotal = 0m;
                var canBuildSet = true;

                for (var requirementIndex = 0; requirementIndex < groupedBuyProducts.Count; requirementIndex++)
                {
                    var requirement = groupedBuyProducts[requirementIndex];
                    var requiredQuantity = requirement.MinQuantity > 0 ? requirement.MinQuantity : 1;
                    var candidates = requirementMatches[requirementIndex]
                        .OrderByDescending(lineIndex => unitPrices[lineIndex]);

                    foreach (var lineIndex in candidates)
                    {
                        int value;
                        var alreadyConsumed = consumedInSet.TryGetValue(lineIndex, out value) ? value : 0;
                        var availableQuantity = remainingQuantities[lineIndex] - alreadyConsumed;
                        if (availableQuantity <= 0)
                            continue;

                        var usedQuantity = Math.Min(requiredQuantity, availableQuantity);
                        requiredQuantity -= usedQuantity;
                        setSubtotal += usedQuantity * unitPrices[lineIndex];
                        consumedInSet[lineIndex] = alreadyConsumed + usedQuantity;

                        if (requiredQuantity <= 0)
                            break;
                    }

                    if (requiredQuantity > 0)
                    {
                        canBuildSet = false;
                        break;
                    }
                }

                if (!canBuildSet)
                    break;

                foreach (var consumed in consumedInSet)
                {
                    remainingQuantities[consumed.Key] -= consumed.Value;
                    var lineId = cartLines[consumed.Key].Id;
                    int existingQuantity;
                    if (quantitiesByLineId.TryGetValue(lineId, out existingQuantity))
                        quantitiesByLineId[lineId] = existingQuantity + consumed.Value;
                    else
                        quantitiesByLineId[lineId] = consumed.Value;
                }

                setCount++;
                matchedSubtotal += setSubtotal;
            }

            return new ComboSetMatchSummary
            {
                SetCount = setCount,
                MatchedSubtotal = matchedSubtotal,
                QuantitiesByLineId = quantitiesByLineId
            };
        }

        private bool IsCartItemMatchedByRuleProduct(
            ShoppingCartItem cartItem,
            PromotionRuleProduct ruleProduct,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeValue> attributeValueCache,
            Dictionary<int, HashSet<int>> mappingAllValueIdsCache)
        {
            Product product;
            if (ruleProduct.IsAllProducts)
                return true;

            if (ruleProduct.ProductId > 0 && cartItem.ProductId != ruleProduct.ProductId)
                return false;

            if (!ruleProduct.CategoryId.HasValue && !ruleProduct.ManufacturerId.HasValue && !ruleProduct.VendorId.HasValue && ruleProduct.ProductId <= 0)
                return false;

            if (!productCache.TryGetValue(cartItem.ProductId, out product))
            {
                product = _productService.GetProductById(cartItem.ProductId);
                productCache[cartItem.ProductId] = product;
            }

            if (product == null || product.Deleted)
                return false;

            if (ruleProduct.VendorId.HasValue && product.VendorId != ruleProduct.VendorId.Value)
                return false;

            if (ruleProduct.CategoryId.HasValue)
            {
                if (!IsProductMatchedByRuleCategory(cartItem.ProductId, ruleProduct.CategoryId.Value, categoryIdsCache))
                    return false;
            }

            if (ruleProduct.ManufacturerId.HasValue)
            {
                var manufacturerIds = GetProductManufacturerIds(cartItem.ProductId, manufacturerIdsCache);
                if (!manufacturerIds.Contains(ruleProduct.ManufacturerId.Value))
                    return false;
            }

            if (ruleProduct.RewardAttributeSelectionType == RewardAttributeSelectionType.SpecificValues &&
                !string.IsNullOrWhiteSpace(ruleProduct.RewardAttributeValueIds))
            {
                var requiredValueIds = ParseIdList(ruleProduct.RewardAttributeValueIds);
                if (requiredValueIds.Count > 0)
                {
                    var values = GetCartItemAttributeValues(cartItem, cartAttributeValuesCache);
                    if (!AreCartItemAttributesMatched(values, requiredValueIds, attributeValueCache, mappingAllValueIdsCache))
                        return false;
                }
            }

            return true;
        }

        private bool AreCartItemAttributesMatched(
            IList<ProductAttributeValue> cartValues,
            IList<int> requiredValueIds,
            Dictionary<int, ProductAttributeValue> attributeValueCache,
            Dictionary<int, HashSet<int>> mappingAllValueIdsCache)
        {
            ProductAttributeValue requiredValue;
            HashSet<int> ids;
            List<int> cartValueIds;
            if (requiredValueIds == null || requiredValueIds.Count == 0)
                return true;

            var requiredByMapping = new Dictionary<int, HashSet<int>>();
            foreach (var valueId in requiredValueIds.Distinct())
            {
                if (!attributeValueCache.TryGetValue(valueId, out requiredValue))
                {
                    requiredValue = _productAttributeService.GetProductAttributeValueById(valueId);
                    attributeValueCache[valueId] = requiredValue;
                }

                if (requiredValue == null)
                    continue;

                if (!requiredByMapping.TryGetValue(requiredValue.ProductAttributeMappingId, out ids))
                {
                    ids = new HashSet<int>();
                    requiredByMapping[requiredValue.ProductAttributeMappingId] = ids;
                }

                ids.Add(requiredValue.Id);
            }

            if (!requiredByMapping.Any())
                return false;

            var normalizedRequiredByMapping = new Dictionary<int, HashSet<int>>();
            foreach (var required in requiredByMapping)
            {
                var allMappingValueIds = GetAllAttributeValueIdsByMapping(required.Key, mappingAllValueIdsCache);
                if (allMappingValueIds.Any() && required.Value.SetEquals(allMappingValueIds))
                    continue;

                normalizedRequiredByMapping[required.Key] = required.Value;
            }

            if (!normalizedRequiredByMapping.Any())
                return true;

            if (cartValues == null || cartValues.Count == 0)
                return false;

            var cartByMapping = cartValues
                .GroupBy(x => x.ProductAttributeMappingId)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToList());

            foreach (var required in normalizedRequiredByMapping)
            {
                if (!cartByMapping.TryGetValue(required.Key, out cartValueIds))
                    return false;

                if (!required.Value.Any(cartValueIds.Contains))
                    return false;
            }

            return true;
        }

        private HashSet<int> GetAllAttributeValueIdsByMapping(
            int mappingId,
            Dictionary<int, HashSet<int>> mappingAllValueIdsCache)
        {
            if (mappingId <= 0)
                return new HashSet<int>();

            HashSet<int> cachedIds;
            if (mappingAllValueIdsCache.TryGetValue(mappingId, out cachedIds))
                return cachedIds;

            var mappingValueIds = _cacheManager.Get(GetAttributeValueIdsByMappingCacheKey(mappingId), () => new HashSet<int>(_productAttributeService.GetProductAttributeValues(mappingId).Select(x => x.Id)));

            mappingAllValueIdsCache[mappingId] = mappingValueIds;
            return mappingValueIds;
        }

        private int GetMatchedQuantityForRuleProduct(
            IList<ShoppingCartItem> cart,
            PromotionRuleProduct ruleProduct,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeValue> attributeValueCache,
            Dictionary<int, HashSet<int>> mappingAllValueIdsCache)
        {
            if (cart == null || !cart.Any() || ruleProduct == null)
                return 0;

            var matchedQuantity = 0;
            foreach (var cartItem in cart)
            {
                if (IsCartItemMatchedByRuleProduct(cartItem, ruleProduct, productCache, categoryIdsCache, manufacturerIdsCache, cartAttributeValuesCache, attributeValueCache, mappingAllValueIdsCache))
                    matchedQuantity += cartItem.Quantity;
            }

            return matchedQuantity;
        }

        private decimal GetCartSubtotal(IList<ShoppingCartItem> cart)
        {
            var subtotal = 0m;
            foreach (var item in cart)
            {
                var product = _productService.GetProductById(item.ProductId);
                if (product == null)
                    continue;

                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                subtotal += unitPrice * item.Quantity;
            }

            return subtotal;
        }

        private static bool EvaluateSubtotalCondition(PromotionRuleCondition condition, decimal subtotal)
        {
            switch (condition.ConditionOperator)
            {
                case ConditionOperator.GreaterThan:
                    return subtotal > condition.MinValue;
                case ConditionOperator.LessThan:
                    return subtotal < condition.MinValue;
                case ConditionOperator.Between:
                    return subtotal >= condition.MinValue && subtotal <= condition.MaxValue;
                case ConditionOperator.EqualTo:
                    return subtotal == condition.MinValue;
                default:
                    return false;
            }
        }

        private static ConditionMatchResult CreateConditionMatchResult(
            bool isMatched,
            int? matchedQuantity = null,
            IList<int> matchedShoppingCartItemIds = null)
        {
            var hasQuantityMetric = matchedQuantity.HasValue && matchedQuantity.Value > 0;
            return new ConditionMatchResult
            {
                IsMatched = isMatched,
                HasQuantityMetric = hasQuantityMetric,
                MatchedQuantity = hasQuantityMetric ? matchedQuantity.Value : 0,
                MatchedShoppingCartItemIds = isMatched && matchedShoppingCartItemIds != null
                    ? matchedShoppingCartItemIds.Where(x => x > 0).Distinct().ToList()
                    : new List<int>()
            };
        }

        private static ConditionMatchResult CombineConditionMatchResults(
            ConditionMatchResult left,
            ConditionMatchResult right,
            ConditionLogicalOperator logicalOperator)
        {
            if (left == null)
            left = CreateConditionMatchResult(false);
            if (right == null)
            right = CreateConditionMatchResult(false);

            if (logicalOperator == ConditionLogicalOperator.Or)
            {
                if (!left.IsMatched && !right.IsMatched)
                    return CreateConditionMatchResult(false);

                if (left.IsMatched && right.IsMatched)
                {
                    var matchedItemIds = CombineOrMatchedItemIds(left, right);
                    if (left.HasQuantityMetric && right.HasQuantityMetric)
                        return CreateConditionMatchResult(true, Math.Max(left.MatchedQuantity, right.MatchedQuantity), matchedItemIds);

                    if (left.HasQuantityMetric)
                        return CreateConditionMatchResult(true, left.MatchedQuantity, matchedItemIds);

                    if (right.HasQuantityMetric)
                        return CreateConditionMatchResult(true, right.MatchedQuantity, matchedItemIds);

                    return CreateConditionMatchResult(true, null, matchedItemIds);
                }

                return left.IsMatched
                    ? CreateConditionMatchResult(true, left.HasQuantityMetric ? (int?)left.MatchedQuantity : null, left.MatchedShoppingCartItemIds)
                    : CreateConditionMatchResult(true, right.HasQuantityMetric ? (int?)right.MatchedQuantity : null, right.MatchedShoppingCartItemIds);
            }

            if (!left.IsMatched || !right.IsMatched)
                return CreateConditionMatchResult(false);

            var andMatchedItemIds = CombineAndMatchedItemIds(left, right);
            if (left.HasQuantityMetric && right.HasQuantityMetric)
                return CreateConditionMatchResult(true, Math.Min(left.MatchedQuantity, right.MatchedQuantity), andMatchedItemIds);

            if (left.HasQuantityMetric)
                return CreateConditionMatchResult(true, left.MatchedQuantity, andMatchedItemIds);

            if (right.HasQuantityMetric)
                return CreateConditionMatchResult(true, right.MatchedQuantity, andMatchedItemIds);

            return CreateConditionMatchResult(true, null, andMatchedItemIds);
        }

        private static IList<int> CombineOrMatchedItemIds(ConditionMatchResult left, ConditionMatchResult right)
        {
            var leftIds = left.MatchedShoppingCartItemIds ?? new List<int>();
            var rightIds = right.MatchedShoppingCartItemIds ?? new List<int>();

            if (!leftIds.Any() || !rightIds.Any())
                return new List<int>();

            return leftIds.Concat(rightIds).Distinct().ToList();
        }

        private static IList<int> CombineAndMatchedItemIds(ConditionMatchResult left, ConditionMatchResult right)
        {
            var leftIds = left.MatchedShoppingCartItemIds ?? new List<int>();
            var rightIds = right.MatchedShoppingCartItemIds ?? new List<int>();

            if (leftIds.Any() && rightIds.Any())
                return leftIds.Intersect(rightIds).ToList();

            if (leftIds.Any())
                return leftIds.Distinct().ToList();

            if (rightIds.Any())
                return rightIds.Distinct().ToList();

            return new List<int>();
        }

        private static IList<int> IntersectLineItemCandidates(IList<IList<int>> lineItemCandidates)
        {
            if (lineItemCandidates == null || !lineItemCandidates.Any())
                return new List<int>();

            var result = lineItemCandidates
                .Where(x => x != null)
                .Select(x => x.Where(id => id > 0).Distinct().ToList())
                .Where(x => x.Any())
                .ToList();

            if (!result.Any())
                return new List<int>();

            return result
                .Skip(1)
                .Aggregate(result.First().AsEnumerable(), (current, next) => current.Intersect(next))
                .Distinct()
                .ToList();
        }

        private ConditionMatchResult EvaluateConditionTreeWithQuantity(
            PromotionRuleCondition condition,
            Dictionary<int, List<PromotionRuleCondition>> childrenByParent,
            PromotionRuleType ruleType,
            IList<ShoppingCartItem> cart,
            HashSet<int> cartProductIds,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache,
            Customer customer,
            int[] customerRoleIds,
            bool isFirstOrder,
            bool isNewCustomer,
            decimal subtotal,
            int paidCompletedOrderCount,
            PromotionEvaluationContext evaluationContext,
            HashSet<int> visited)
        {
            List<PromotionRuleCondition> childConditions;
            if (condition == null)
                return CreateConditionMatchResult(false);

            if (!visited.Add(condition.Id))
                return CreateConditionMatchResult(false);

            var result = EvaluateRuleConditionWithQuantity(
                ruleType,
                condition,
                cart,
                cartProductIds,
                productCache,
                categoryIdsCache,
                manufacturerIdsCache,
                productSpecOptionIdsCache,
                specOptionCache,
                cartAttributeValuesCache,
                attributeMappingCache,
                customer,
                customerRoleIds,
                isFirstOrder,
                isNewCustomer,
                subtotal,
                paidCompletedOrderCount,
                evaluationContext);

            if (childrenByParent.TryGetValue(condition.Id, out childConditions) && childConditions.Any())
            {
                foreach (var childCondition in childConditions.OrderBy(x => x.Id))
                {
                    var childResult = EvaluateConditionTreeWithQuantity(
                        childCondition,
                        childrenByParent,
                        ruleType,
                        cart,
                        cartProductIds,
                        productCache,
                        categoryIdsCache,
                        manufacturerIdsCache,
                        productSpecOptionIdsCache,
                        specOptionCache,
                        cartAttributeValuesCache,
                        attributeMappingCache,
                        customer,
                        customerRoleIds,
                        isFirstOrder,
                        isNewCustomer,
                        subtotal,
                        paidCompletedOrderCount,
                        evaluationContext,
                        visited);

                    var logicalOperator = Enum.IsDefined(typeof(ConditionLogicalOperator), childCondition.LogicalOperatorId)
                        ? childCondition.LogicalOperator
                        : ConditionLogicalOperator.And;

                    result = CombineConditionMatchResults(result, childResult, logicalOperator);
                }
            }

            visited.Remove(condition.Id);
            return result;
        }

        private ConditionMatchResult EvaluateRuleConditionWithQuantity(
            PromotionRuleType ruleType,
            PromotionRuleCondition condition,
            IList<ShoppingCartItem> cart,
            HashSet<int> cartProductIds,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache,
            Customer customer,
            int[] customerRoleIds,
            bool isFirstOrder,
            bool isNewCustomer,
            decimal subtotal,
            int paidCompletedOrderCount,
            PromotionEvaluationContext evaluationContext)
        {
            Product product;
            var hasCriteria = false;
            var result = true;
            var quantityCandidates = new List<int>();
            var lineItemCandidates = new List<IList<int>>();
            if (evaluationContext == null)
            evaluationContext = new PromotionEvaluationContext
            {
                StoreId = 0,
                CustomerId = customer?.Id ?? 0,
                Cart = cart,
                CurrentUtc = DateTime.UtcNow
            };

            var hasSubtotalCriteria = condition.ConditionOperatorId > 0 &&
                                      (ruleType == PromotionRuleType.SubtotalBased ||
                                       condition.MinValue != 0 ||
                                       condition.MaxValue != 0);
            if (hasSubtotalCriteria)
            {
                hasCriteria = true;
                result &= EvaluateSubtotalCondition(condition, subtotal);
            }

            var hasSourceCondition = condition.ConditionSourceTypeId > 0 &&
                                     !string.IsNullOrWhiteSpace(condition.ConditionSourceData);

            var requiresSameLine = condition.RequireSameLineMatch;
            if (requiresSameLine)
            {
                hasCriteria = true;
                var sameLineResult = EvaluateSameLineConditionWithQuantity(
                    condition,
                    cart,
                    productCache,
                    categoryIdsCache,
                    manufacturerIdsCache,
                    productSpecOptionIdsCache,
                    specOptionCache,
                    cartAttributeValuesCache,
                    attributeMappingCache);
                result &= sameLineResult.IsMatched;
                if (sameLineResult.IsMatched && sameLineResult.HasQuantityMetric)
                    quantityCandidates.Add(sameLineResult.MatchedQuantity);
                if (sameLineResult.IsMatched && sameLineResult.MatchedShoppingCartItemIds.Any())
                    lineItemCandidates.Add(sameLineResult.MatchedShoppingCartItemIds);
            }
            else if (hasSourceCondition)
            {
                hasCriteria = true;
                var sourceResult = EvaluateSourceConditionWithQuantity(
                    condition,
                    cart,
                    productCache,
                    categoryIdsCache,
                    manufacturerIdsCache,
                    productSpecOptionIdsCache,
                    specOptionCache,
                    cartAttributeValuesCache,
                    attributeMappingCache);
                result &= sourceResult.IsMatched;
                if (sourceResult.IsMatched && sourceResult.HasQuantityMetric)
                    quantityCandidates.Add(sourceResult.MatchedQuantity);
                if (sourceResult.IsMatched && sourceResult.MatchedShoppingCartItemIds.Any())
                    lineItemCandidates.Add(sourceResult.MatchedShoppingCartItemIds);
            }

            if (!requiresSameLine && condition.RequiredProductId.HasValue && condition.RequiredProductId.Value > 0)
            {
                hasCriteria = true;
                var matchedProductItems = cart
                    .Where(x => x.ProductId == condition.RequiredProductId.Value)
                    .ToList();
                var matchedProductQuantity = matchedProductItems.Sum(x => x.Quantity);
                result &= matchedProductQuantity > 0;
                if (matchedProductQuantity > 0)
                {
                    quantityCandidates.Add(matchedProductQuantity);
                    lineItemCandidates.Add(matchedProductItems.Select(x => x.Id).ToList());
                }
            }

            if (condition.ExcludedProductId.HasValue && condition.ExcludedProductId.Value > 0)
            {
                hasCriteria = true;
                result &= !cartProductIds.Contains(condition.ExcludedProductId.Value);
            }

            if (!requiresSameLine && condition.RequiredVendorId.HasValue && condition.RequiredVendorId.Value > 0)
            {
                hasCriteria = true;
                var matchedVendorQuantity = 0;
                var matchedVendorItemIds = new List<int>();
                foreach (var item in cart)
                {
                    if (!productCache.TryGetValue(item.ProductId, out product))
                    {
                        product = _productService.GetProductById(item.ProductId);
                        productCache[item.ProductId] = product;
                    }

                    if (product != null && !product.Deleted && product.VendorId == condition.RequiredVendorId.Value)
                    {
                        matchedVendorQuantity += item.Quantity;
                        matchedVendorItemIds.Add(item.Id);
                    }
                }

                result &= matchedVendorQuantity > 0;
                if (matchedVendorQuantity > 0)
                {
                    quantityCandidates.Add(matchedVendorQuantity);
                    lineItemCandidates.Add(matchedVendorItemIds);
                }
            }

            if (!requiresSameLine && condition.RequiredCategoryId.HasValue && condition.RequiredCategoryId.Value > 0)
            {
                hasCriteria = true;
                var matchedCategoryQuantity = 0;
                var matchedCategoryItemIds = new List<int>();
                foreach (var item in cart)
                {
                    if (IsProductMatchedByRuleCategory(item.ProductId, condition.RequiredCategoryId.Value, categoryIdsCache))
                    {
                        matchedCategoryQuantity += item.Quantity;
                        matchedCategoryItemIds.Add(item.Id);
                    }
                }

                result &= matchedCategoryQuantity > 0;
                if (matchedCategoryQuantity > 0)
                {
                    quantityCandidates.Add(matchedCategoryQuantity);
                    lineItemCandidates.Add(matchedCategoryItemIds);
                }
            }

            if (condition.RequiredCustomerRoleId.HasValue && condition.RequiredCustomerRoleId.Value > 0)
            {
                hasCriteria = true;
                result &= customer != null && customerRoleIds.Contains(condition.RequiredCustomerRoleId.Value);
            }

            if ((condition.QuantityMin.HasValue || condition.QuantityMax.HasValue) && !hasSourceCondition)
            {
                hasCriteria = true;
                var quantityMetric = quantityCandidates.Any()
                    ? quantityCandidates.Min()
                    : cart.Sum(x => x.Quantity);
                result &= IsQuantityInRange(quantityMetric, condition.QuantityMin, condition.QuantityMax);
            }

            if (condition.IsFirstOrderOnly)
            {
                hasCriteria = true;
                result &= isFirstOrder;
            }

            if (condition.IsNewCustomerOnly)
            {
                hasCriteria = true;
                result &= isNewCustomer;
            }

            if (condition.RequiredOrderCountMin.HasValue || condition.RequiredOrderCountMax.HasValue)
            {
                hasCriteria = true;
                var minOrderCount = condition.RequiredOrderCountMin;
                var maxOrderCount = condition.RequiredOrderCountMax.GetValueOrDefault() > 0
                    ? condition.RequiredOrderCountMax
                    : null;
                result &= IsQuantityInRange(paidCompletedOrderCount, minOrderCount, maxOrderCount);
            }

            if (!string.IsNullOrWhiteSpace(condition.RequiredCountryCodesCsv))
            {
                hasCriteria = true;
                var countryTokens = ParseTokenList(condition.RequiredCountryCodesCsv ?? string.Empty)
                    .Select(x => x.ToUpperInvariant())
                    .ToList();
                var hasFallbackAllow = ContainsAllowCountryFallbackToken(countryTokens);
                countryTokens = countryTokens
                    .Where(x => !x.Equals("*", StringComparison.OrdinalIgnoreCase) &&
                                !x.Equals("ALLOW_EMPTY", StringComparison.OrdinalIgnoreCase) &&
                                !x.Equals("FALLBACK_ALLOW", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (string.IsNullOrWhiteSpace(evaluationContext.CountryIso2))
                    result &= hasFallbackAllow;
                else
                    result &= countryTokens.Contains(evaluationContext.CountryIso2.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(condition.RequiredPaymentMethodsCsv))
            {
                hasCriteria = true;
                var requiredPaymentMethods = ParseTokenList(condition.RequiredPaymentMethodsCsv ?? string.Empty);
                if (requiredPaymentMethods.Count == 0)
                {
                    result = false;
                }
                else
                {
                    result &= !string.IsNullOrWhiteSpace(evaluationContext.SelectedPaymentMethodSystemName) &&
                              requiredPaymentMethods.Contains(evaluationContext.SelectedPaymentMethodSystemName, StringComparer.OrdinalIgnoreCase);
                }
            }

            var isMatched = hasCriteria && result;
            var matchedQuantity = isMatched && quantityCandidates.Any()
                ? quantityCandidates.Min()
                : (int?)null;
            var matchedItemIds = isMatched
                ? IntersectLineItemCandidates(lineItemCandidates)
                : null;

            return CreateConditionMatchResult(isMatched, matchedQuantity, matchedItemIds);
        }

        private ConditionMatchResult EvaluateSameLineConditionWithQuantity(
            PromotionRuleCondition condition,
            IList<ShoppingCartItem> cart,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache)
        {
            Product product;
            var hasLineBoundCriteria = condition.RequiredProductId.GetValueOrDefault() > 0 ||
                                       condition.RequiredCategoryId.GetValueOrDefault() > 0 ||
                                       condition.RequiredVendorId.GetValueOrDefault() > 0 ||
                                       (condition.ConditionSourceTypeId > 0 && !string.IsNullOrWhiteSpace(condition.ConditionSourceData));
            if (!hasLineBoundCriteria)
                return CreateConditionMatchResult(false);

            var sourceType = Enum.IsDefined(typeof(ConditionSourceType), condition.ConditionSourceTypeId)
                ? condition.ConditionSourceType
                : ConditionSourceType.Products;
            var hasSourceCondition = condition.ConditionSourceTypeId > 0 && !string.IsNullOrWhiteSpace(condition.ConditionSourceData);
            var sourceEntries = hasSourceCondition
                ? ParseConditionSourceEntries(condition.ConditionSourceData ?? string.Empty)
                : new List<ConditionSourceEntry>();
            var sourceRestriction = Enum.IsDefined(typeof(ConditionRestrictionType), condition.ConditionRestrictionTypeId)
                ? condition.ConditionRestrictionType
                : ConditionRestrictionType.Include;
            var matchedQuantity = 0;
            var matchedItemIds = new List<int>();

            foreach (var item in cart)
            {
                if (condition.RequiredProductId.GetValueOrDefault() > 0 && item.ProductId != condition.RequiredProductId.Value)
                    continue;

                if (!productCache.TryGetValue(item.ProductId, out product))
                {
                    product = _productService.GetProductById(item.ProductId);
                    productCache[item.ProductId] = product;
                }

                if (product == null || product.Deleted)
                    continue;

                if (condition.RequiredVendorId.GetValueOrDefault() > 0 && product.VendorId != condition.RequiredVendorId.Value)
                    continue;

                if (condition.RequiredCategoryId.GetValueOrDefault() > 0)
                {
                    if (!IsProductMatchedByRuleCategory(item.ProductId, condition.RequiredCategoryId.Value, categoryIdsCache))
                        continue;
                }

                if (hasSourceCondition)
                {
                    var anyEntryMatched = false;
                    foreach (var entry in sourceEntries)
                    {
                        if (!IsCartItemMatchedBySourceEntry(
                                item,
                                entry,
                                sourceType,
                                condition.AttributeMatchMode,
                                productCache,
                                categoryIdsCache,
                                manufacturerIdsCache,
                                productSpecOptionIdsCache,
                                specOptionCache,
                                cartAttributeValuesCache,
                                attributeMappingCache))
                        {
                            continue;
                        }

                        anyEntryMatched = true;
                        break;
                    }

                    if (sourceRestriction == ConditionRestrictionType.Include && !anyEntryMatched)
                        continue;

                    if (sourceRestriction == ConditionRestrictionType.Exclude && anyEntryMatched)
                        continue;
                }

                matchedQuantity += item.Quantity;
                matchedItemIds.Add(item.Id);
            }

            if (!IsQuantityInRange(matchedQuantity, condition.QuantityMin, condition.QuantityMax))
                return CreateConditionMatchResult(false);

            return CreateConditionMatchResult(matchedQuantity > 0, matchedQuantity, matchedItemIds);
        }

        private ConditionMatchResult EvaluateSourceConditionWithQuantity(
            PromotionRuleCondition condition,
            IList<ShoppingCartItem> cart,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache)
        {
            var restrictionType = Enum.IsDefined(typeof(ConditionRestrictionType), condition.ConditionRestrictionTypeId)
                ? condition.ConditionRestrictionType
                : ConditionRestrictionType.Include;

            var sourceType = Enum.IsDefined(typeof(ConditionSourceType), condition.ConditionSourceTypeId)
                ? condition.ConditionSourceType
                : ConditionSourceType.Products;

            if (IsSessionSourceType(sourceType))
                return CreateConditionMatchResult(EvaluateSessionSourceCondition(condition, sourceType));

            var entries = ParseConditionSourceEntries(condition.ConditionSourceData ?? string.Empty);
            if (!entries.Any())
                return CreateConditionMatchResult(false);

            var totalMatchedItemsById = new Dictionary<int, ShoppingCartItem>();
            var entryRangeMatchedItemsById = new Dictionary<int, ShoppingCartItem>();
            var anyEntryMatched = false;
            var hasEntryRange = entries.Any(x => x.HasQuantityRange);

            foreach (var entry in entries)
            {
                var entryItems = GetMatchedCartItemsForEntry(
                    entry,
                    sourceType,
                    condition.AttributeMatchMode,
                    cart,
                    productCache,
                    categoryIdsCache,
                    manufacturerIdsCache,
                    productSpecOptionIdsCache,
                    specOptionCache,
                    cartAttributeValuesCache,
                    attributeMappingCache);
                var entryQty = entryItems.Sum(x => x.Quantity);

                foreach (var item in entryItems.Where(x => x != null))
                    if (!totalMatchedItemsById.ContainsKey(item.Id))
                        totalMatchedItemsById[item.Id] = item;

                var entryMatched = entryQty > 0;
                if (entry.HasQuantityRange)
                    entryMatched = IsQuantityInRange(entryQty, entry.MinQuantity, entry.MaxQuantity);

                if (entryMatched)
                {
                    anyEntryMatched = true;
                    foreach (var item in entryItems.Where(x => x != null))
                        if (!entryRangeMatchedItemsById.ContainsKey(item.Id))
                            entryRangeMatchedItemsById[item.Id] = item;
                }
            }

            var matchedItemsForMetric = hasEntryRange
                ? entryRangeMatchedItemsById.Values.ToList()
                : totalMatchedItemsById.Values.ToList();
            var totalMatchedQty = matchedItemsForMetric.Sum(x => x.Quantity);
            var matchedItemIds = matchedItemsForMetric.Select(x => x.Id).ToList();
            var rangeMatch = IsQuantityInRange(totalMatchedQty, condition.QuantityMin, condition.QuantityMax);
            var hasTotalRange = condition.QuantityMin.HasValue || condition.QuantityMax.HasValue;

            if (restrictionType == ConditionRestrictionType.Exclude)
            {
                var excluded = hasEntryRange ? anyEntryMatched : totalMatchedItemsById.Any();
                if (hasTotalRange)
                    excluded = excluded && rangeMatch;

                return CreateConditionMatchResult(!excluded);
            }

            if (hasEntryRange && !anyEntryMatched)
                return CreateConditionMatchResult(false);

            if (!hasEntryRange && totalMatchedQty <= 0)
                return CreateConditionMatchResult(false);

            if (hasTotalRange && !rangeMatch)
                return CreateConditionMatchResult(false);

            return CreateConditionMatchResult(true, totalMatchedQty, matchedItemIds.Distinct().ToList());
        }

        private static bool IsQuantityInRange(int quantity, int? min, int? max)
        {
            if (min.HasValue && quantity < min.Value)
                return false;
            if (max.HasValue && max.Value > 0 && quantity > max.Value)
                return false;
            return true;
        }

        private IList<ShoppingCartItem> GetMatchedCartItemsForEntry(
            ConditionSourceEntry entry,
            ConditionSourceType sourceType,
            AttributeMatchMode attributeMatchMode,
            IList<ShoppingCartItem> cart,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache)
        {
            var matchedItems = new List<ShoppingCartItem>();
            foreach (var item in cart)
            {
                if (!IsCartItemMatchedBySourceEntry(
                        item,
                        entry,
                        sourceType,
                        attributeMatchMode,
                        productCache,
                        categoryIdsCache,
                        manufacturerIdsCache,
                        productSpecOptionIdsCache,
                        specOptionCache,
                        cartAttributeValuesCache,
                        attributeMappingCache))
                {
                    continue;
                }

                matchedItems.Add(item);
            }

            return matchedItems;
        }

        private bool IsCartItemMatchedBySourceEntry(
            ShoppingCartItem cartItem,
            ConditionSourceEntry entry,
            ConditionSourceType sourceType,
            AttributeMatchMode attributeMatchMode,
            Dictionary<int, Product> productCache,
            Dictionary<int, HashSet<int>> categoryIdsCache,
            Dictionary<int, HashSet<int>> manufacturerIdsCache,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache)
        {
            Product product;
            if (!productCache.TryGetValue(cartItem.ProductId, out product))
            {
                product = _productService.GetProductById(cartItem.ProductId);
                productCache[cartItem.ProductId] = product;
            }

            if (product == null || product.Deleted)
                return false;

            switch (sourceType)
            {
                case ConditionSourceType.Products:
                    return cartItem.ProductId == entry.Id;

                case ConditionSourceType.Categories:
                {
                    return IsProductMatchedByRuleCategory(cartItem.ProductId, entry.Id, categoryIdsCache);
                }

                case ConditionSourceType.Manufacturers:
                {
                    var manufacturerIds = GetProductManufacturerIds(cartItem.ProductId, manufacturerIdsCache);
                    return manufacturerIds.Contains(entry.Id);
                }

                case ConditionSourceType.Vendors:
                    return product.VendorId == entry.Id;

                case ConditionSourceType.SpecificationAttributeOptions:
                    return IsSpecificationAttributeMatched(entry, product, productSpecOptionIdsCache, specOptionCache);

                case ConditionSourceType.ProductAttributeValues:
                    return IsProductAttributeMatched(entry, cartItem, attributeMatchMode, cartAttributeValuesCache, attributeMappingCache);

                case ConditionSourceType.ExpiryDays:
                {
                    if (entry.Id > 0 && cartItem.ProductId != entry.Id)
                        return false;

                    if (!product.AvailableEndDateTimeUtc.HasValue)
                        return false;

                    var daysUntilExpiry = (int)Math.Floor((product.AvailableEndDateTimeUtc.Value - DateTime.UtcNow).TotalDays);
                    if (entry.HasQuantityRange)
                        return IsQuantityInRange(daysUntilExpiry, entry.MinQuantity, entry.MaxQuantity);

                    return true;
                }

                case ConditionSourceType.DeviceType:
                case ConditionSourceType.SalesChannel:
                case ConditionSourceType.CampaignSource:
                case ConditionSourceType.ReferralSource:
                default:
                    return false;
            }
        }

        private bool IsSpecificationAttributeMatched(
            ConditionSourceEntry entry,
            Product product,
            Dictionary<int, HashSet<int>> productSpecOptionIdsCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache)
        {
            SpecificationAttributeOption option;
            var optionIds = GetProductSpecificationOptionIds(product.Id, productSpecOptionIdsCache);
            if (!optionIds.Any())
                return false;

            foreach (var optionId in optionIds)
            {
                if (!specOptionCache.TryGetValue(optionId, out option))
                {
                    option = _specificationAttributeService.GetSpecificationAttributeOptionById(optionId);
                    if (option != null)
                        specOptionCache[optionId] = option;
                }

                if (option == null || option.SpecificationAttributeId != entry.Id)
                    continue;

                if (!entry.HasOptions)
                    return true;

                if (entry.OptionIds.Contains(option.Id))
                    return true;

                if (!string.IsNullOrWhiteSpace(option.Name) && entry.OptionNames.Contains(option.Name))
                    return true;
            }

            return false;
        }

        private bool IsProductAttributeMatched(
            ConditionSourceEntry entry,
            ShoppingCartItem cartItem,
            AttributeMatchMode attributeMatchMode,
            Dictionary<int, IList<ProductAttributeValue>> cartAttributeValuesCache,
            Dictionary<int, ProductAttributeMapping> attributeMappingCache)
        {
            ProductAttributeMapping mapping;
            var values = GetCartItemAttributeValues(cartItem, cartAttributeValuesCache);
            if (!values.Any())
                return false;

            foreach (var value in values)
            {
                if (!attributeMappingCache.TryGetValue(value.ProductAttributeMappingId, out mapping))
                {
                    mapping = _productAttributeService.GetProductAttributeMappingById(value.ProductAttributeMappingId);
                    if (mapping != null)
                        attributeMappingCache[value.ProductAttributeMappingId] = mapping;
                }

                if (mapping == null || mapping.ProductAttributeId != entry.Id)
                    continue;

                if (!entry.HasOptions)
                    return true;

                var mode = Enum.IsDefined(typeof(AttributeMatchMode), (int)attributeMatchMode)
                    ? attributeMatchMode
                    : AttributeMatchMode.Any;

                if (mode == AttributeMatchMode.Any)
                {
                    if (entry.OptionIds.Contains(value.Id))
                        return true;

                    if (!string.IsNullOrWhiteSpace(value.Name) && entry.OptionNames.Contains(value.Name))
                        return true;
                }
                else
                {
                    var mappingValues = values
                        .Where(x => x.ProductAttributeMappingId == value.ProductAttributeMappingId)
                        .ToList();
                    var mappingValueIds = mappingValues.Select(x => x.Id).ToList();
                    var mappingValueNames = mappingValues
                        .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                        .Select(x => x.Name)
                        .ToList();

                    var idsMatched = !entry.OptionIds.Any() || entry.OptionIds.All(mappingValueIds.Contains);
                    var namesMatched = !entry.OptionNames.Any() || entry.OptionNames.All(mappingValueNames.Contains);
                    return idsMatched && namesMatched;
                }
            }

            return false;
        }
    }
}
