using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Stores;
using PluginDiscountType = Nop.Plugin.Misc.DiscountManagerPlus.Domain.DiscountType;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class DiscountManagerPlusService : IDiscountManagerPlusService
    {
        private const string LastAppliedPromotionsCacheKeyPrefix = "Nop.DiscountManagerPlus.EvaluateCart.Last";

        private readonly IPromotionEvaluationContextFactory _promotionEvaluationContextFactory;
        private readonly IPromotionConditionEvaluator _promotionConditionEvaluator;
        private readonly IPromotionRuleEvaluator _promotionRuleEvaluator;
        private readonly IPromotionDiscountAllocator _promotionDiscountAllocator;
        private readonly IRewardSynchronizationService _rewardSynchronizationService;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IDiscountService _discountService;
        private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;
        private readonly ISettingService _settingService;
        private readonly ICustomerService _customerService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IDiscountCoordinationService _discountCoordinationService;
        private readonly ILogger _logger;
        private readonly IStoreContext _storeContext;
        private readonly IProductService _productService;

        public DiscountManagerPlusService(
            IPromotionEvaluationContextFactory promotionEvaluationContextFactory,
            IPromotionConditionEvaluator promotionConditionEvaluator,
            IPromotionRuleEvaluator promotionRuleEvaluator,
            IPromotionDiscountAllocator promotionDiscountAllocator,
            IRewardSynchronizationService rewardSynchronizationService,
            IPromotionRuleService promotionRuleService,
            IDiscountService discountService,
            IDiscountManagerPlusRequirementService discountManagerPlusRequirementService,
            ISettingService settingService,
            ICustomerService customerService,
            IPriceCalculationService priceCalculationService,
            IStoreMappingService storeMappingService,
            IDiscountCoordinationService discountCoordinationService,
            ILogger logger,
            IStoreContext storeContext,
            IProductService productService)
        {
            _promotionEvaluationContextFactory = promotionEvaluationContextFactory;
            _promotionConditionEvaluator = promotionConditionEvaluator;
            _promotionRuleEvaluator = promotionRuleEvaluator;
            _promotionDiscountAllocator = promotionDiscountAllocator;
            _rewardSynchronizationService = rewardSynchronizationService;
            _promotionRuleService = promotionRuleService;
            _discountService = discountService;
            _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
            _settingService = settingService;
            _customerService = customerService;
            _priceCalculationService = priceCalculationService;
            _storeMappingService = storeMappingService;
            _discountCoordinationService = discountCoordinationService;
            _logger = logger;
            _storeContext = storeContext;
            _productService = productService;
        }

        public IList<AppliedPromotion> EvaluateCart(IList<ShoppingCartItem> cart, int storeId = 0)
        {
            var evaluationContext = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);
            return EvaluateCart(evaluationContext);
        }

        public IList<AppliedPromotion> EvaluateCart(PromotionEvaluationContext context)
        {
            var results = new List<AppliedPromotion>();
            var cart = context != null ? context.Cart : null;
            if (cart == null || !cart.Any())
                return results;

            try
            {
                var storeId = context.StoreId;
                var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(storeId);
                if (!settings.IsEnabled)
                    return results;

                var httpContext = HttpContext.Current;
                var requestCacheKey = string.Empty;
                if (httpContext != null)
                {
                    var cartKey = string.Join(",", cart.OrderBy(x => x.Id).Select(x => string.Format("{0}-{1}-{2}", x.Id, x.ProductId, x.Quantity)));
                    var couponCodes = context.CouponCodes ?? new string[0];
                    var couponKey = string.Join(",", couponCodes.Select(NormalizeCouponCode));
                    requestCacheKey = string.Format("Nop.DiscountManagerPlus.EvaluateCart.{0}.{1}.{2}.{3}.{4}.{5}",
                        storeId, context.CustomerId, cartKey, context.SelectedPaymentMethodSystemName, context.CountryIso2, couponKey);
                    if (httpContext.Items.Contains(requestCacheKey))
                    {
                        var cachedResults = httpContext.Items[requestCacheKey] as IList<AppliedPromotion>;
                        if (cachedResults != null)
                            return cachedResults;
                    }
                }

                var timeoutMs = settings.MaxRuleEvaluationTimeMs > 0 ? settings.MaxRuleEvaluationTimeMs : int.MaxValue;
                var startedOn = DateTime.UtcNow;
                var activeRules = _promotionRuleService.GetActiveRules(storeId);

                DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: Found {0} active rules", activeRules.Count));
                foreach (var rule in activeRules)
                {
                    DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: Active rule - '{0}' (Type: {1}, Priority: {2}, Active: {3})",
                        rule.Name, rule.RuleType, rule.Priority, rule.IsActive));
                }

                foreach (var rule in activeRules)
                {
                    if ((DateTime.UtcNow - startedOn).TotalMilliseconds > timeoutMs)
                        break;

                    if (!IsRuleRuntimeEligible(rule, context))
                        continue;

                    var applied = _promotionRuleEvaluator.EvaluateRule(rule, cart, context);
                    if (applied == null)
                        continue;

                    PopulateAppliedPromotionTarget(applied, cart);

                    if (rule.EnableAutoUpgrade && applied.DiscountAmount > 0)
                    {
                        applied.BenefitValue = applied.DiscountAmount;
                        applied.IsAutoUpgradeEligible = true;
                    }

                    var shouldAdd = applied.DiscountAmount >= 0 || applied.RequiresRewardSelection || applied.AutoAddReward;

                    if (applied.IsExclusive)
                    {
                        results.Clear();
                        if (shouldAdd)
                            results.Add(applied);
                        break;
                    }

                    if (shouldAdd)
                        results.Add(applied);
                }

                var hasMultipleBuyXGetY = results.Count(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY) >= 2;

                DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: Multiple BuyXGetY detected: {0}, Total results: {1}", hasMultipleBuyXGetY, results.Count));

                if (!hasMultipleBuyXGetY)
                {
                    DiscountManagerPlusLog.Information(_logger, "DUAL_OFFER_DEBUG: Applying auto-upgrade logic");
                    results = ApplyAutoUpgradeLogic(results);
                }
                else
                {
                    DiscountManagerPlusLog.Information(_logger, "DUAL_OFFER_DEBUG: SKIPPING auto-upgrade to preserve dual promotions");
                }

                if (httpContext != null && !string.IsNullOrWhiteSpace(requestCacheKey))
                {
                    httpContext.Items[requestCacheKey] = results;
                    httpContext.Items[PrepareLastAppliedPromotionsCacheKey(storeId, context.CustomerId)] = results;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("DiscountManagerPlusService: error evaluating cart.", ex);
            }

            return results;
        }

        private List<AppliedPromotion> ApplyAutoUpgradeLogic(IList<AppliedPromotion> results)
        {
            if (results == null || results.Count <= 1)
                return results != null ? results.ToList() : new List<AppliedPromotion>();

            var filtered = new List<AppliedPromotion>();

            var groupedByItems = results
                .GroupBy(x => string.Join(",", x.EligibleShoppingCartItemIds.OrderBy(i => i)))
                .ToList();

            foreach (var group in groupedByItems)
            {
                if (group.Count() == 1)
                {
                    filtered.Add(group.First());
                }
                else
                {
                    var autoUpgradeEligible = group.Where(x => x.IsAutoUpgradeEligible).OrderByDescending(x => x.BenefitValue).ToList();

                    if (autoUpgradeEligible.Any())
                    {
                        var best = autoUpgradeEligible.First();
                        filtered.Add(best);
                        filtered.AddRange(group.Where(x => x.PromotionRuleId != best.PromotionRuleId && !x.IsAutoUpgradeEligible));
                    }
                    else
                    {
                        filtered.AddRange(group);
                    }
                }
            }

            return filtered;
        }

        private void PopulateAppliedPromotionTarget(AppliedPromotion appliedPromotion, IList<ShoppingCartItem> cart)
        {
            if (appliedPromotion == null || cart == null || !cart.Any() || appliedPromotion.TargetProductId.HasValue)
                return;

            int? targetLineId = null;
            if (appliedPromotion.LineDiscounts != null)
            {
                var cheapest = appliedPromotion.LineDiscounts
                    .Where(x => x.Value > 0)
                    .OrderBy(x =>
                    {
                        var item = cart.FirstOrDefault(c => c.Id == x.Key);
                        if (item == null)
                            return 0m;
                        return _priceCalculationService.GetUnitPrice(item, false);
                    })
                    .Select(x => (int?)x.Key)
                    .FirstOrDefault();
                targetLineId = cheapest;
            }

            var targetItem = targetLineId.HasValue
                ? cart.FirstOrDefault(x => x.Id == targetLineId.Value)
                : null;

            if (targetItem == null && appliedPromotion.EligibleShoppingCartItemIds.Any())
            {
                var eligibleIds = new HashSet<int>(appliedPromotion.EligibleShoppingCartItemIds);
                targetItem = cart.Where(x => eligibleIds.Contains(x.Id))
                    .OrderBy(x => _priceCalculationService.GetUnitPrice(x, false))
                    .FirstOrDefault();
            }

            if (targetItem == null && appliedPromotion.RewardProductId.HasValue && appliedPromotion.RewardProductId.Value > 0)
                targetItem = cart.FirstOrDefault(x => x.ProductId == appliedPromotion.RewardProductId.Value);

            if (targetItem == null)
                return;

            appliedPromotion.TargetProductId = targetItem.ProductId;
            var product = _productService.GetProductById(targetItem.ProductId);
            appliedPromotion.TargetProductName = product != null ? product.Name : string.Empty;
        }

        public IList<AppliedPromotion> GetRequestAppliedPromotions(int customerId, int storeId)
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null || customerId <= 0 || storeId <= 0)
                return new List<AppliedPromotion>();

            var key = PrepareLastAppliedPromotionsCacheKey(storeId, customerId);
            if (httpContext.Items.Contains(key))
            {
                var cachedPromotions = httpContext.Items[key] as IList<AppliedPromotion>;
                if (cachedPromotions != null)
                    return cachedPromotions;
            }

            return new List<AppliedPromotion>();
        }

        public IDictionary<int, decimal> BuildLineDiscountMap(IList<ShoppingCartItem> cart, int storeId = 0)
        {
            var evaluationContext = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);
            return BuildLineDiscountMap(evaluationContext);
        }

        public IDictionary<int, decimal> BuildLineDiscountMap(PromotionEvaluationContext context)
        {
            return BuildDiscountMapsFromAppliedPromotions(context).LineDiscountMap;
        }

        public IDictionary<int, decimal> BuildRuleDiscountMap(IList<ShoppingCartItem> cart, int storeId = 0)
        {
            var evaluationContext = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);
            return BuildRuleDiscountMap(evaluationContext);
        }

        public IDictionary<int, decimal> BuildRuleDiscountMap(PromotionEvaluationContext context)
        {
            return BuildDiscountMapsFromAppliedPromotions(context).RuleDiscountMap;
        }

        private DiscountMapsResult BuildDiscountMapsFromAppliedPromotions(PromotionEvaluationContext context)
        {
            var lineDiscountMap = new Dictionary<int, decimal>();
            var ruleDiscountMap = new Dictionary<int, decimal>();
            var cart = context != null ? context.Cart : null;
            if (cart == null || !cart.Any())
                return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);

            var appliedPromotions = EvaluateCart(context);
            if (!appliedPromotions.Any())
                return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);

            DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: Total applied promotions: {0}", appliedPromotions.Count));
            foreach (var promo in appliedPromotions)
            {
                DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: Promotion '{0}' - Type: {1}, Discount: ${2}, Exclusive: {3}",
                    promo.RuleName, promo.RuleTypeId, promo.DiscountAmount, promo.IsExclusive));
            }

            var buyXGetYPromotions = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                .ToList();

            DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: BuyXGetY promotions found: {0}", buyXGetYPromotions.Count));
            var needsCoordination = buyXGetYPromotions.Count >= 2;
            DiscountManagerPlusLog.Information(_logger, string.Format("DUAL_OFFER_DEBUG: Needs coordination: {0}", needsCoordination));

            if (needsCoordination)
            {
                DiscountManagerPlusLog.Information(_logger, "DUAL_OFFER_DEBUG: ACTIVATING COORDINATION SERVICE");
                return BuildCoordinatedDiscountMaps(appliedPromotions, cart);
            }

            DiscountManagerPlusLog.Information(_logger, "DUAL_OFFER_DEBUG: Using standard discount processing (no coordination)");

            var blockedLineIds = new HashSet<int>();
            foreach (var appliedPromotion in appliedPromotions)
            {
                var rule = _promotionRuleService.GetPromotionRuleById(appliedPromotion.PromotionRuleId);
                if (rule == null)
                    continue;

                var ruleLineDiscounts = BuildPromotionLineDiscounts(rule, appliedPromotion, cart, blockedLineIds);
                if (!ruleLineDiscounts.Any())
                    continue;

                var ruleDiscountAmount = ruleLineDiscounts.Sum(x => x.Value);
                if (ruleDiscountAmount <= 0)
                    continue;

                if (appliedPromotion.IsExclusive)
                {
                    lineDiscountMap.Clear();
                    ruleDiscountMap.Clear();
                    foreach (var discount in ruleLineDiscounts)
                        lineDiscountMap[discount.Key] = discount.Value;

                    ruleDiscountMap[appliedPromotion.PromotionRuleId] = ruleDiscountAmount;
                    return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);
                }

                foreach (var discount in ruleLineDiscounts)
                {
                    decimal existing;
                    if (lineDiscountMap.TryGetValue(discount.Key, out existing))
                        lineDiscountMap[discount.Key] = existing + discount.Value;
                    else
                        lineDiscountMap[discount.Key] = discount.Value;
                }

                decimal existingRuleDiscount;
                if (ruleDiscountMap.TryGetValue(appliedPromotion.PromotionRuleId, out existingRuleDiscount))
                    ruleDiscountMap[appliedPromotion.PromotionRuleId] = existingRuleDiscount + ruleDiscountAmount;
                else
                    ruleDiscountMap[appliedPromotion.PromotionRuleId] = ruleDiscountAmount;

                if (rule.StopFurtherRulesForMatchedLines)
                {
                    foreach (var lineId in ruleLineDiscounts.Keys)
                        blockedLineIds.Add(lineId);
                }
            }

            return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);
        }

        private DiscountMapsResult BuildCoordinatedDiscountMaps(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart)
        {
            var lineDiscountMap = new Dictionary<int, decimal>();
            var ruleDiscountMap = new Dictionary<int, decimal>();

            if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
                return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);

            var coordinatedAllocations = _discountCoordinationService.CoordinateCheapestItemDiscounts(
                appliedPromotions,
                cart);

            if (!coordinatedAllocations.Any())
                return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);

            foreach (var kvp in coordinatedAllocations)
            {
                var promotionRuleId = kvp.Key;
                var allocations = kvp.Value;

                if (!allocations.Any())
                    continue;

                var ruleLineDiscounts = new Dictionary<int, decimal>();
                foreach (var allocation in allocations)
                {
                    if (allocation.DiscountAmount > 0)
                    {
                        decimal existing;
                        if (ruleLineDiscounts.TryGetValue(allocation.LineId, out existing))
                            ruleLineDiscounts[allocation.LineId] = existing + allocation.DiscountAmount;
                        else
                            ruleLineDiscounts[allocation.LineId] = allocation.DiscountAmount;
                    }
                }

                if (!ruleLineDiscounts.Any())
                    continue;

                var ruleDiscountAmount = ruleLineDiscounts.Sum(x => x.Value);
                if (ruleDiscountAmount <= 0)
                    continue;

                foreach (var discount in ruleLineDiscounts)
                {
                    decimal existing;
                    if (lineDiscountMap.TryGetValue(discount.Key, out existing))
                        lineDiscountMap[discount.Key] = existing + discount.Value;
                    else
                        lineDiscountMap[discount.Key] = discount.Value;
                }

                decimal existingRuleDiscount;
                if (ruleDiscountMap.TryGetValue(promotionRuleId, out existingRuleDiscount))
                    ruleDiscountMap[promotionRuleId] = existingRuleDiscount + ruleDiscountAmount;
                else
                    ruleDiscountMap[promotionRuleId] = ruleDiscountAmount;
            }

            return new DiscountMapsResult(lineDiscountMap, ruleDiscountMap);
        }

        private Dictionary<int, decimal> BuildPromotionLineDiscounts(
            PromotionRule rule,
            AppliedPromotion appliedPromotion,
            IList<ShoppingCartItem> cart,
            ISet<int> blockedLineIds)
        {
            if (rule == null || appliedPromotion == null || cart == null || !cart.Any())
                return new Dictionary<int, decimal>();

            if (appliedPromotion.LineDiscounts != null && appliedPromotion.LineDiscounts.Any())
            {
                return appliedPromotion.LineDiscounts
                    .Where(x => x.Value > 0 && (blockedLineIds == null || !blockedLineIds.Contains(x.Key)))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            var isFreeItemReward = appliedPromotion.DiscountTypeId == (int)PluginDiscountType.FreeItem &&
                                   appliedPromotion.RewardQuantity > 0;
            if (isFreeItemReward)
            {
                var rewardItems = _promotionDiscountAllocator.GetEligibleCartItemsForAppliedPromotion(rule, appliedPromotion, cart);
                rewardItems = rewardItems
                    .Where(x => blockedLineIds == null || !blockedLineIds.Contains(x.Id))
                    .ToList();
                if (!rewardItems.Any())
                    return new Dictionary<int, decimal>();

                var rewardDiscountResult = _promotionDiscountAllocator.CalculateRewardDiscounts(
                    rewardItems,
                    appliedPromotion.RewardQuantity,
                    appliedPromotion.DiscountedQuantitiesByLineId,
                    (PluginDiscountType)appliedPromotion.DiscountTypeId,
                    appliedPromotion.DiscountValue);

                return rewardDiscountResult.LineDiscounts
                    .Where(x => x.Value > 0 && (blockedLineIds == null || !blockedLineIds.Contains(x.Key)))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            if (appliedPromotion.DiscountAmount <= 0)
                return new Dictionary<int, decimal>();

            var eligibleItems = _promotionDiscountAllocator.GetEligibleCartItemsForAppliedPromotion(rule, appliedPromotion, cart);
            eligibleItems = eligibleItems
                .Where(x => blockedLineIds == null || !blockedLineIds.Contains(x.Id))
                .ToList();
            if (!eligibleItems.Any())
                return new Dictionary<int, decimal>();

            return _promotionDiscountAllocator.AllocateDiscountAcrossItems(
                appliedPromotion.DiscountAmount,
                eligibleItems,
                appliedPromotion.DiscountedQuantitiesByLineId);
        }

        public AppliedPromotion EvaluateRule(PromotionRule rule, IList<ShoppingCartItem> cart)
        {
            var storeId = 0;
            var first = cart != null ? cart.FirstOrDefault() : null;
            if (first != null)
                storeId = first.StoreId;
            var context = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);
            return _promotionRuleEvaluator.EvaluateRule(rule, cart, context);
        }

        public bool EvaluateDiscountRequirement(int discountRequirementId, Customer customer, int storeId = 0)
        {
            if (discountRequirementId <= 0 || customer == null || customer.Deleted)
                return false;

            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(storeId);
            if (!settings.IsEnabled || !settings.UseDefaultDiscountPipeline)
                return false;

            var cart = GetCustomerCart(customer, storeId);
            if (!cart.Any())
                return false;

            var conditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
            if (!conditions.Any())
                return false;

            var evaluationContext = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);
            return _promotionConditionEvaluator.EvaluateRuleConditions(
                PromotionRuleType.CartCondition,
                conditions,
                cart,
                false,
                storeId,
                evaluationContext);
        }

        public bool EvaluateLinkedDiscountRequirement(int discountRequirementId, Customer customer, int storeId = 0)
        {
            if (discountRequirementId <= 0 || customer == null || customer.Deleted)
                return false;

            var discountRequirement = FindDiscountRequirementById(discountRequirementId);
            if (discountRequirement == null)
                return false;

            return !ShouldSuppressLinkedDiscount(discountRequirement.DiscountId, customer, storeId);
        }

        public bool IsLinkedDiscountEligible(PromotionRule rule, PromotionEvaluationContext context)
        {
            var parentDiscountId = PromotionRuntimeHelper.GetParentDiscountId(rule);
            if (rule == null || parentDiscountId <= 0)
                return true;

            var discount = _discountService.GetDiscountById(parentDiscountId);
            if (discount == null)
                return false;

            var customerId = 0;
            if (context != null && context.CustomerId > 0)
                customerId = context.CustomerId;
            else if (context != null && context.Cart != null)
            {
                var first = context.Cart.FirstOrDefault();
                if (first != null)
                    customerId = first.CustomerId;
            }
            if (customerId <= 0)
                return false;

            var customer = _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted)
                return false;

            var ignoredRequirementIds = new HashSet<int>();
            var requirements = GetDiscountRequirements(discount);
            foreach (var requirement in requirements)
            {
                if (_discountManagerPlusRequirementService.GetRequirementKind(requirement.Id) == DiscountManagerPlusRequirementKind.LinkedDiscountCarry)
                    ignoredRequirementIds.Add(requirement.Id);
            }

            var couponCodes = context != null && context.CouponCodes != null
                ? context.CouponCodes.ToArray()
                : new string[0];

            return ValidateLinkedDiscount(
                discount,
                customer,
                couponCodes,
                context != null ? context.StoreId : 0,
                ignoredRequirementIds);
        }

        public bool ShouldSuppressLinkedDiscount(int discountId, Customer customer, int storeId = 0)
        {
            if (discountId <= 0 || customer == null || customer.Deleted)
                return false;

            var cart = GetCustomerCart(customer, storeId);
            if (!cart.Any())
                return false;

            var context = _promotionEvaluationContextFactory.BuildDefaultEvaluationContext(cart, storeId);
            var linkedRules = _promotionRuleService.GetSuppressingPromotionRulesByDiscountId(discountId);
            foreach (var linkedRule in linkedRules)
            {
                if (!IsRuleRuntimeEligible(linkedRule, context))
                    continue;

                if (_promotionRuleEvaluator.EvaluateRule(linkedRule, cart, context) != null)
                    return true;
            }

            return false;
        }

        public decimal GetCartSubtotal(IList<ShoppingCartItem> cart)
        {
            return _promotionDiscountAllocator.GetCartSubtotal(cart);
        }

        public void SynchronizeAutoAddedRewards(Customer customer, int storeId = 0)
        {
            if (customer == null || customer.Id <= 0)
                return;

            try
            {
                var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>(storeId);
                if (!settings.IsEnabled)
                    return;

                var cart = GetCustomerCart(customer, storeId);
                if (!cart.Any())
                    return;

                var appliedPromotions = EvaluateCart(cart, storeId);
                _rewardSynchronizationService.SynchronizeAutoAddedRewards(customer, cart, appliedPromotions, storeId);
            }
            catch (Exception ex)
            {
                _logger.Error("DiscountManagerPlusService: error synchronizing auto-added rewards.", ex);
            }
        }

        private bool ValidateLinkedDiscount(
            Nop.Core.Domain.Discounts.Discount discount,
            Customer customer,
            string[] couponCodesToValidate,
            int storeId,
            ISet<int> ignoredRequirementIds)
        {
            if (discount == null || customer == null || customer.Deleted)
                return false;

            if (discount.RequiresCouponCode)
            {
                if (string.IsNullOrWhiteSpace(discount.CouponCode))
                    return false;

                if (couponCodesToValidate == null || !couponCodesToValidate.Any(x => x.Equals(discount.CouponCode, StringComparison.InvariantCultureIgnoreCase)))
                    return false;
            }

            if (discount.DiscountType == Nop.Core.Domain.Discounts.DiscountType.AssignedToOrderSubTotal ||
                discount.DiscountType == Nop.Core.Domain.Discounts.DiscountType.AssignedToOrderTotal)
            {
                var store = _storeContext.CurrentStore;
                var cart = GetCustomerCart(customer, storeId > 0 ? storeId : store.Id);
                if (cart.Any(x => x.Product != null && x.Product.IsGiftCard))
                    return false;
            }

            var now = DateTime.UtcNow;
            if (discount.StartDateUtc.HasValue && DateTime.SpecifyKind(discount.StartDateUtc.Value, DateTimeKind.Utc) > now)
                return false;

            if (discount.EndDateUtc.HasValue && DateTime.SpecifyKind(discount.EndDateUtc.Value, DateTimeKind.Utc) < now)
                return false;

            switch (discount.DiscountLimitation)
            {
                case Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesOnly:
                    {
                        var usedTimes = _discountService.GetAllDiscountUsageHistory(discount.Id, null, null, 0, 1).TotalCount;
                        if (usedTimes >= discount.LimitationTimes)
                            return false;
                        break;
                    }
                case Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer:
                    {
                        if (customer.IsRegistered())
                        {
                            var usedTimes = _discountService.GetAllDiscountUsageHistory(discount.Id, customer.Id, null, 0, 1).TotalCount;
                            if (usedTimes >= discount.LimitationTimes)
                                return false;
                        }
                        break;
                    }
            }

            var requirements = GetDiscountRequirements(discount);
            if (!requirements.Any())
                return true;

            return GetLinkedDiscountValidationResult(
                requirements,
                customer,
                storeId,
                ignoredRequirementIds ?? new HashSet<int>());
        }

        private bool GetLinkedDiscountValidationResult(
            IList<Nop.Core.Domain.Discounts.DiscountRequirement> requirements,
            Customer customer,
            int storeId,
            ISet<int> ignoredRequirementIds)
        {
            var result = false;
            var hasEvaluatedRequirement = false;

            foreach (var requirement in requirements)
            {
                if (ignoredRequirementIds.Contains(requirement.Id))
                    continue;

                if (string.Equals(requirement.DiscountRequirementRuleSystemName, DiscountManagerPlusDefaults.LinkedDiscountWrapperSystemName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                hasEvaluatedRequirement = true;
                var requirementRulePlugin = _discountService.LoadDiscountRequirementRuleBySystemName(requirement.DiscountRequirementRuleSystemName);
                if (requirementRulePlugin == null)
                    continue;

                var store = _storeContext.CurrentStore;
                var ruleResult = requirementRulePlugin.CheckRequirement(new DiscountRequirementValidationRequest
                {
                    DiscountRequirementId = requirement.Id,
                    Customer = customer,
                    Store = store
                });

                result = ruleResult.IsValid;
                if (!result)
                    return false;
            }

            return !hasEvaluatedRequirement || result;
        }

        private bool IsRuleRuntimeEligible(PromotionRule rule, PromotionEvaluationContext context)
        {
            if (rule == null || !rule.IsActive)
                return false;

            if (context == null)
            {
                context = new PromotionEvaluationContext
                {
                    CurrentUtc = DateTime.UtcNow
                };
            }

            var now = context.CurrentUtc;
            if (rule.StartDateUtc.HasValue && now < rule.StartDateUtc.Value)
                return false;

            if (rule.EndDateUtc.HasValue && now > rule.EndDateUtc.Value)
                return false;

            if (!IsRuleAvailableInStore(rule, context.StoreId))
                return false;

            var parentDiscountId = PromotionRuntimeHelper.GetParentDiscountId(rule);
            if (parentDiscountId > 0)
                return IsLinkedDiscountEligible(rule, context);

            if (!rule.IsFlashEnabled)
                return true;

            if (rule.UsageWindowStartUtc.HasValue && now < rule.UsageWindowStartUtc.Value)
                return false;

            if (rule.UsageWindowEndUtc.HasValue && now > rule.UsageWindowEndUtc.Value)
                return false;

            var usageStartUtc = rule.UsageWindowStartUtc;
            if (rule.UsageLimitTotal > 0)
            {
                var totalUsageCount = _promotionRuleService.GetRuleUsageCount(rule.Id, usageStartUtc);
                if (totalUsageCount >= rule.UsageLimitTotal)
                    return false;
            }

            if (rule.UsageLimitPerCustomer > 0)
            {
                if (context.CustomerId <= 0)
                    return false;

                var customerUsageCount = _promotionRuleService.GetRuleUsageCount(rule.Id, usageStartUtc, context.CustomerId);
                if (customerUsageCount >= rule.UsageLimitPerCustomer)
                    return false;
            }

            return IsLinkedDiscountEligible(rule, context);
        }

        private bool IsRuleAvailableInStore(PromotionRule rule, int storeId)
        {
            if (rule == null)
                return false;

            if (rule.LimitedToStores)
                return _storeMappingService.Authorize(rule, storeId);

            return rule.LimitedToStore <= 0 || storeId == 0 || rule.LimitedToStore == storeId;
        }

        private static string NormalizeCouponCode(string couponCode)
        {
            return string.IsNullOrWhiteSpace(couponCode) ? string.Empty : couponCode.Trim().ToUpperInvariant();
        }

        private string PrepareLastAppliedPromotionsCacheKey(int storeId, int customerId)
        {
            return string.Format("{0}.{1}.{2}", LastAppliedPromotionsCacheKeyPrefix, storeId, customerId);
        }

        public IList<DiscountAllocation> GetCoordinatedAllocations(IList<ShoppingCartItem> cart, int storeId)
        {
            var allAllocations = new List<DiscountAllocation>();

            try
            {
                System.Diagnostics.Debug.WriteLine(string.Format("DISCOUNT_DEBUG: GetCoordinatedAllocations called for {0} cart items", cart != null ? cart.Count : 0));

                if (cart == null || !cart.Any())
                    return allAllocations;

                var appliedPromotions = EvaluateCart(cart, storeId);
                if (appliedPromotions == null || !appliedPromotions.Any())
                    return allAllocations;

                var buyXGetYPromotions = appliedPromotions
                    .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                    .ToList();

                if (buyXGetYPromotions.Count < 2)
                    return allAllocations;

                var coordinatedAllocations = _discountCoordinationService.CoordinateCheapestItemDiscounts(
                    appliedPromotions,
                    cart);

                foreach (var kvp in coordinatedAllocations)
                {
                    foreach (var allocation in kvp.Value)
                        allAllocations.Add(allocation);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting coordinated allocations", ex);
            }

            return allAllocations;
        }

        private static IList<ShoppingCartItem> GetCustomerCart(Customer customer, int storeId)
        {
            if (customer == null || customer.ShoppingCartItems == null)
                return new List<ShoppingCartItem>();

            return customer.ShoppingCartItems
                .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(storeId)
                .ToList();
        }

        private Nop.Core.Domain.Discounts.DiscountRequirement FindDiscountRequirementById(int discountRequirementId)
        {
            var discounts = _discountService.GetAllDiscounts(null, "", "", true);
            foreach (var discount in discounts)
            {
                if (discount.DiscountRequirements == null)
                    continue;

                foreach (var requirement in discount.DiscountRequirements)
                {
                    if (requirement.Id == discountRequirementId)
                        return requirement;
                }
            }

            return null;
        }

        private static IList<Nop.Core.Domain.Discounts.DiscountRequirement> GetDiscountRequirements(Nop.Core.Domain.Discounts.Discount discount)
        {
            if (discount == null || discount.DiscountRequirements == null)
                return new List<Nop.Core.Domain.Discounts.DiscountRequirement>();

            return discount.DiscountRequirements.ToList();
        }
    }
}
