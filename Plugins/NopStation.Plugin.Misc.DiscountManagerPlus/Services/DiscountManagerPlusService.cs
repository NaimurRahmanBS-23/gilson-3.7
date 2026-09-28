using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class DiscountManagerPlusService : IDiscountManagerPlusService
{
    private const string LastAppliedPromotionsCacheKeyPrefix = "NopStation.DiscountManagerPlus.EvaluateCart.Last";

    private readonly IPromotionEvaluationContextFactory _promotionEvaluationContextFactory;
    private readonly IPromotionConditionEvaluator _promotionConditionEvaluator;
    private readonly IPromotionRuleEvaluator _promotionRuleEvaluator;
    private readonly IPromotionDiscountAllocator _promotionDiscountAllocator;
    private readonly IRewardSynchronizationService _rewardSynchronizationService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IDiscountPluginManager _discountPluginManager;
    private readonly IDiscountService _discountService;
    private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;
    private readonly ISettingService _settingService;
    private readonly ICustomerService _customerService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IStoreMappingService _storeMappingService;
    private readonly IDiscountCoordinationService _discountCoordinationService;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStoreContext _storeContext;
    private readonly Nop.Services.Catalog.IProductService _productService;

    public DiscountManagerPlusService(
        IPromotionEvaluationContextFactory promotionEvaluationContextFactory,
        IPromotionConditionEvaluator promotionConditionEvaluator,
        IPromotionRuleEvaluator promotionRuleEvaluator,
        IPromotionDiscountAllocator promotionDiscountAllocator,
        IRewardSynchronizationService rewardSynchronizationService,
        IPromotionRuleService promotionRuleService,
        IDiscountPluginManager discountPluginManager,
        IDiscountService discountService,
        IDiscountManagerPlusRequirementService discountManagerPlusRequirementService,
        ISettingService settingService,
        ICustomerService customerService,
        IShoppingCartService shoppingCartService,
        IStoreMappingService storeMappingService,
        IDiscountCoordinationService discountCoordinationService,
        ILogger logger,
        IHttpContextAccessor httpContextAccessor,
        IStoreContext storeContext,
        Nop.Services.Catalog.IProductService productService)
    {
        _promotionEvaluationContextFactory = promotionEvaluationContextFactory;
        _promotionConditionEvaluator = promotionConditionEvaluator;
        _promotionRuleEvaluator = promotionRuleEvaluator;
        _promotionDiscountAllocator = promotionDiscountAllocator;
        _rewardSynchronizationService = rewardSynchronizationService;
        _promotionRuleService = promotionRuleService;
        _discountPluginManager = discountPluginManager;
        _discountService = discountService;
        _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
        _settingService = settingService;
        _customerService = customerService;
        _shoppingCartService = shoppingCartService;
        _storeMappingService = storeMappingService;
        _discountCoordinationService = discountCoordinationService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _storeContext = storeContext;
        _productService = productService;
    }

    public async Task<IList<AppliedPromotion>> EvaluateCartAsync(IList<ShoppingCartItem> cart, int storeId = 0)
    {
        var evaluationContext = await _promotionEvaluationContextFactory.BuildDefaultEvaluationContextAsync(cart, storeId);
        return await EvaluateCartAsync(evaluationContext);
    }

    public async Task<IList<AppliedPromotion>> EvaluateCartAsync(PromotionEvaluationContext context)
    {
        var results = new List<AppliedPromotion>();
        var cart = context?.Cart;
        if (cart == null || !cart.Any())
            return results;

        try
        {
            var storeId = context.StoreId;
            var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(storeId);
            if (!settings.IsEnabled)
                return results;

            var httpContext = _httpContextAccessor.HttpContext;
            var requestCacheKey = string.Empty;
            if (httpContext != null)
            {
                var cartKey = string.Join(",", cart.OrderBy(x => x.Id).Select(x => $"{x.Id}-{x.ProductId}-{x.Quantity}"));
                var couponKey = string.Join(",", (context.CouponCodes ?? Array.Empty<string>()).Select(NormalizeCouponCode));
                requestCacheKey = $"NopStation.DiscountManagerPlus.EvaluateCart.{storeId}.{context.CustomerId}.{cartKey}.{context.SelectedPaymentMethodSystemName}.{context.CountryIso2}.{couponKey}";
                if (httpContext.Items.TryGetValue(requestCacheKey, out var cached) && cached is IList<AppliedPromotion> cachedResults)
                    return cachedResults;
            }

            var timeoutMs = settings.MaxRuleEvaluationTimeMs > 0 ? settings.MaxRuleEvaluationTimeMs : int.MaxValue;
            var startedOn = DateTime.UtcNow;
            var activeRules = await _promotionRuleService.GetActiveRulesAsync(storeId);

            // DEBUG: Log all active rules
            await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Found {activeRules.Count} active rules");
            foreach (var rule in activeRules)
            {
                await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Active rule - '{rule.Name}' (Type: {rule.RuleType}, Priority: {rule.Priority}, Active: {rule.IsActive})");
            }

            foreach (var rule in activeRules)
            {
                if ((DateTime.UtcNow - startedOn).TotalMilliseconds > timeoutMs)
                    break;

                if (!await IsRuleRuntimeEligibleAsync(rule, context))
                    continue;

                var applied = await _promotionRuleEvaluator.EvaluateRuleAsync(rule, cart, context);
                if (applied == null)
                    continue;

                await PopulateAppliedPromotionTargetAsync(applied, cart);

                if (rule.EnableAutoUpgrade && applied.DiscountAmount > 0)
                {
                    applied.BenefitValue = applied.DiscountAmount;
                    applied.IsAutoUpgradeEligible = true;
                }

                // FIXED: Changed logic to allow zero-discount promotions for coordination
                // Both offers need to be evaluated even if one has no discount yet
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

            // FIXED: Skip auto-upgrade logic for dual-offer scenarios
            // We want both promotions to apply, not filter out the "weaker" one
            var hasMultipleBuyXGetY = results.Count(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY) >= 2;

            // DEBUG: Log auto-upgrade decision
            await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Multiple BuyXGetY detected: {hasMultipleBuyXGetY}, Total results: {results.Count}");

            if (!hasMultipleBuyXGetY)
            {
                // DEBUG: Auto-upgrade will be applied
                await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Applying auto-upgrade logic");
                // Only apply auto-upgrade logic for non-dual-offer scenarios
                results = ApplyAutoUpgradeLogic(results);
            }
            else
            {
                // DEBUG: Skipping auto-upgrade to preserve both promotions
                await _logger.InformationAsync($"DUAL_OFFER_DEBUG: SKIPPING auto-upgrade to preserve dual promotions");
            }

            if (httpContext != null && !string.IsNullOrWhiteSpace(requestCacheKey))
            {
                httpContext.Items[requestCacheKey] = results;
                httpContext.Items[PrepareLastAppliedPromotionsCacheKey(storeId, context.CustomerId)] = results;
            }
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("DiscountManagerPlusService: error evaluating cart.", ex);
        }

        return results;
    }

    /// <summary>
    /// NEW: Auto-upgrade logic - removes lower-benefit promotions when multiple rules apply
    /// </summary>
    private List<AppliedPromotion> ApplyAutoUpgradeLogic(IList<AppliedPromotion> results)
    {
        if (results == null || results.Count <= 1)
            return results?.ToList() ?? new List<AppliedPromotion>();

        var filtered = new List<AppliedPromotion>();

        // Group by eligible items to find competing promotions
        var groupedByItems = results
            .GroupBy(x => string.Join(",", x.EligibleShoppingCartItemIds.OrderBy(i => i)))
            .ToList();

        foreach (var group in groupedByItems)
        {
            if (group.Count() == 1)
            {
                // No competition, add as-is
                filtered.Add(group.First());
            }
            else
            {
                // Multiple promotions competing for same items
                // Keep only the one with highest benefit
                var autoUpgradeEligible = group.Where(x => x.IsAutoUpgradeEligible).OrderByDescending(x => x.BenefitValue).ToList();
                
                if (autoUpgradeEligible.Any())
                {
                    // Add the best auto-upgrade eligible promotion
                    var best = autoUpgradeEligible.First();
                    filtered.Add(best);
                    
                    // Add any non-competing promotions (reward selections, manual picks, etc.)
                    filtered.AddRange(group.Where(x => x.PromotionRuleId != best.PromotionRuleId && !x.IsAutoUpgradeEligible));
                }
                else
                {
                    // No auto-upgrade eligible, keep all (they don't compete)
                    filtered.AddRange(group);
                }
            }
        }

        return filtered;
    }

    private async Task PopulateAppliedPromotionTargetAsync(AppliedPromotion appliedPromotion, IList<ShoppingCartItem> cart)
    {
        if (appliedPromotion == null || cart == null || !cart.Any() || appliedPromotion.TargetProductId.HasValue)
            return;

        // FIXED: Better logic for multi-offer scenarios - prioritize cheapest items
        var targetLineId = appliedPromotion.LineDiscounts?
            .Where(x => x.Value > 0)
            .OrderBy(x =>
            {
                var item = cart.FirstOrDefault(c => c.Id == x.Key);
                if (item == null) return 0;
                // For cheapest-first allocation, we want to identify the cheapest item
                var (unitPrice, _, _) = _shoppingCartService.GetUnitPriceAsync(item, false).GetAwaiter().GetResult();
                return unitPrice; // Sort by unit price ascending
            })
            .Select(x => (int?)x.Key)
            .FirstOrDefault();

        var targetItem = targetLineId.HasValue
            ? cart.FirstOrDefault(x => x.Id == targetLineId.Value)
            : null;

        if (targetItem == null && appliedPromotion.EligibleShoppingCartItemIds.Any())
        {
            var eligibleIds = appliedPromotion.EligibleShoppingCartItemIds.ToHashSet();
            // Get the cheapest eligible item for better multi-offer support
            targetItem = cart.Where(x => eligibleIds.Contains(x.Id))
                .OrderBy(x =>
                {
                    var (unitPrice, _, _) = _shoppingCartService.GetUnitPriceAsync(x, false).GetAwaiter().GetResult();
                    return unitPrice;
                })
                .FirstOrDefault();
        }

        if (targetItem == null && appliedPromotion.RewardProductId is > 0)
            targetItem = cart.FirstOrDefault(x => x.ProductId == appliedPromotion.RewardProductId.Value);

        if (targetItem == null)
            return;

        appliedPromotion.TargetProductId = targetItem.ProductId;
        var product = await _productService.GetProductByIdAsync(targetItem.ProductId);
        appliedPromotion.TargetProductName = product?.Name ?? string.Empty;
    }

    public Task<IList<AppliedPromotion>> GetRequestAppliedPromotionsAsync(int customerId, int storeId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null || customerId <= 0 || storeId <= 0)
            return Task.FromResult<IList<AppliedPromotion>>(new List<AppliedPromotion>());

        if (httpContext.Items.TryGetValue(PrepareLastAppliedPromotionsCacheKey(storeId, customerId), out var cached) &&
            cached is IList<AppliedPromotion> cachedPromotions)
        {
            return Task.FromResult(cachedPromotions);
        }

        return Task.FromResult<IList<AppliedPromotion>>(new List<AppliedPromotion>());
    }

    public async Task<IDictionary<int, decimal>> BuildLineDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId = 0)
    {
        var evaluationContext = await _promotionEvaluationContextFactory.BuildDefaultEvaluationContextAsync(cart, storeId);
        return await BuildLineDiscountMapAsync(evaluationContext);
    }

    public async Task<IDictionary<int, decimal>> BuildLineDiscountMapAsync(PromotionEvaluationContext context)
    {
        var (lineDiscountMap, _) = await BuildDiscountMapsFromAppliedPromotionsAsync(context);
        return lineDiscountMap;
    }

    public async Task<IDictionary<int, decimal>> BuildRuleDiscountMapAsync(IList<ShoppingCartItem> cart, int storeId = 0)
    {
        var evaluationContext = await _promotionEvaluationContextFactory.BuildDefaultEvaluationContextAsync(cart, storeId);
        return await BuildRuleDiscountMapAsync(evaluationContext);
    }

    public async Task<IDictionary<int, decimal>> BuildRuleDiscountMapAsync(PromotionEvaluationContext context)
    {
        var (_, ruleDiscountMap) = await BuildDiscountMapsFromAppliedPromotionsAsync(context);
        return ruleDiscountMap;
    }

    private async Task<(Dictionary<int, decimal> LineDiscountMap, Dictionary<int, decimal> RuleDiscountMap)> BuildDiscountMapsFromAppliedPromotionsAsync(
        PromotionEvaluationContext context)
    {
        var lineDiscountMap = new Dictionary<int, decimal>();
        var ruleDiscountMap = new Dictionary<int, decimal>();
        var cart = context?.Cart;
        if (cart == null || !cart.Any())
            return (lineDiscountMap, ruleDiscountMap);

        var appliedPromotions = await EvaluateCartAsync(context);
        if (!appliedPromotions.Any())
            return (lineDiscountMap, ruleDiscountMap);

        // DEBUG: Log applied promotions for troubleshooting
        await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Total applied promotions: {appliedPromotions.Count}");
        foreach (var promo in appliedPromotions)
        {
            await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Promotion '{promo.RuleName}' - Type: {promo.RuleTypeId}, Discount: ${promo.DiscountAmount}, Exclusive: {promo.IsExclusive}");
        }

        // FIXED: Enhanced coordination detection for dual-offer scenarios
        // We need to coordinate when we have 2+ BuyXGetY promotions, regardless of current discount amounts
        var buyXGetYPromotions = appliedPromotions
            .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
            .ToList();

        await _logger.InformationAsync($"DUAL_OFFER_DEBUG: BuyXGetY promotions found: {buyXGetYPromotions.Count}");
        var needsCoordination = buyXGetYPromotions.Count >= 2;
        await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Needs coordination: {needsCoordination}");

        // FIXED: Use coordination service for dual-offer scenarios to ensure cheapest-item logic
        // This handles the case where we have both "Buy 2 Get 1 Free" and "Buy 1 Get 50% Off"
        if (needsCoordination)
        {
            await _logger.InformationAsync($"DUAL_OFFER_DEBUG: ACTIVATING COORDINATION SERVICE");
            return await BuildCoordinatedDiscountMapsAsync(appliedPromotions, cart);
        }
        else
        {
            await _logger.InformationAsync($"DUAL_OFFER_DEBUG: Using standard discount processing (no coordination)");
        }

        // Original logic for single discount or non-coordinated scenarios
        var blockedLineIds = new HashSet<int>();
        foreach (var appliedPromotion in appliedPromotions)
        {
            var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(appliedPromotion.PromotionRuleId);
            if (rule == null)
                continue;

            var ruleLineDiscounts = await BuildPromotionLineDiscountsAsync(rule, appliedPromotion, cart, blockedLineIds);
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
                return (lineDiscountMap, ruleDiscountMap);
            }

            foreach (var discount in ruleLineDiscounts)
            {
                if (lineDiscountMap.TryGetValue(discount.Key, out var existing))
                    lineDiscountMap[discount.Key] = existing + discount.Value;
                else
                    lineDiscountMap[discount.Key] = discount.Value;
            }

            if (ruleDiscountMap.TryGetValue(appliedPromotion.PromotionRuleId, out var existingRuleDiscount))
                ruleDiscountMap[appliedPromotion.PromotionRuleId] = existingRuleDiscount + ruleDiscountAmount;
            else
                ruleDiscountMap[appliedPromotion.PromotionRuleId] = ruleDiscountAmount;

            if (rule.StopFurtherRulesForMatchedLines)
            {
                foreach (var lineId in ruleLineDiscounts.Keys)
                    blockedLineIds.Add(lineId);
            }
        }

        return (lineDiscountMap, ruleDiscountMap);
    }

    /// <summary>
    /// Builds coordinated discount maps using the DiscountCoordinationService to ensure
    /// proper cheapest-item selection across multiple simultaneous discounts.
    /// </summary>
    private async Task<(Dictionary<int, decimal> LineDiscountMap, Dictionary<int, decimal> RuleDiscountMap)> BuildCoordinatedDiscountMapsAsync(
        IList<AppliedPromotion> appliedPromotions,
        IList<ShoppingCartItem> cart)
    {
        var lineDiscountMap = new Dictionary<int, decimal>();
        var ruleDiscountMap = new Dictionary<int, decimal>();

        if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
            return (lineDiscountMap, ruleDiscountMap);

        // Get coordinated allocations from the coordination service
        var coordinatedAllocations = await _discountCoordinationService.CoordinateCheapestItemDiscountsAsync(
            appliedPromotions,
            cart);

        if (!coordinatedAllocations.Any())
            return (lineDiscountMap, ruleDiscountMap);

        // Process coordinated allocations
        foreach (var kvp in coordinatedAllocations)
        {
            var promotionRuleId = kvp.Key;
            var allocations = kvp.Value;

            if (!allocations.Any())
                continue;

            // Build line discounts from coordinated allocations
            var ruleLineDiscounts = new Dictionary<int, decimal>();
            foreach (var allocation in allocations)
            {
                if (allocation.DiscountAmount > 0)
                {
                    if (ruleLineDiscounts.TryGetValue(allocation.LineId, out var existing))
                        ruleLineDiscounts[allocation.LineId] = existing + allocation.DiscountAmount;
                    else
                        ruleLineDiscounts[allocation.LineId] = allocation.DiscountAmount;
                }
            }

            if (!ruleLineDiscounts.Any())
                continue;

            // Apply coordinated discounts to the maps
            var ruleDiscountAmount = ruleLineDiscounts.Sum(x => x.Value);
            if (ruleDiscountAmount <= 0)
                continue;

            // Add to line discount map
            foreach (var discount in ruleLineDiscounts)
            {
                if (lineDiscountMap.TryGetValue(discount.Key, out var existing))
                    lineDiscountMap[discount.Key] = existing + discount.Value;
                else
                    lineDiscountMap[discount.Key] = discount.Value;
            }

            // Add to rule discount map
            if (ruleDiscountMap.TryGetValue(promotionRuleId, out var existingRuleDiscount))
                ruleDiscountMap[promotionRuleId] = existingRuleDiscount + ruleDiscountAmount;
            else
                ruleDiscountMap[promotionRuleId] = ruleDiscountAmount;
        }

        return (lineDiscountMap, ruleDiscountMap);
    }

    private async Task<Dictionary<int, decimal>> BuildPromotionLineDiscountsAsync(
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
                .Where(x => x.Value > 0 && !(blockedLineIds?.Contains(x.Key) ?? false))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        var isFreeItemReward = appliedPromotion.DiscountTypeId == (int)DiscountType.FreeItem &&
                               appliedPromotion.RewardQuantity > 0;
        if (isFreeItemReward)
        {
            var rewardItems = await _promotionDiscountAllocator.GetEligibleCartItemsForAppliedPromotionAsync(rule, appliedPromotion, cart);
            rewardItems = rewardItems
                .Where(x => !(blockedLineIds?.Contains(x.Id) ?? false))
                .ToList();
            if (!rewardItems.Any())
                return new Dictionary<int, decimal>();

            // FIXED: Use correct overload with discount type and value to properly calculate percentage discounts
            var rewardDiscountResult = await _promotionDiscountAllocator.CalculateRewardDiscountsAsync(
                rewardItems,
                appliedPromotion.RewardQuantity,
                appliedPromotion.DiscountedQuantitiesByLineId,
                (DiscountType)appliedPromotion.DiscountTypeId,
                appliedPromotion.DiscountValue);

            return rewardDiscountResult.LineDiscounts
                .Where(x => x.Value > 0 && !(blockedLineIds?.Contains(x.Key) ?? false))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        if (appliedPromotion.DiscountAmount <= 0)
            return new Dictionary<int, decimal>();

        var eligibleItems = await _promotionDiscountAllocator.GetEligibleCartItemsForAppliedPromotionAsync(rule, appliedPromotion, cart);
        eligibleItems = eligibleItems
            .Where(x => !(blockedLineIds?.Contains(x.Id) ?? false))
            .ToList();
        if (!eligibleItems.Any())
            return new Dictionary<int, decimal>();

        return await _promotionDiscountAllocator.AllocateDiscountAcrossItemsAsync(
            appliedPromotion.DiscountAmount,
            eligibleItems,
            appliedPromotion.DiscountedQuantitiesByLineId);
    }

    public async Task<AppliedPromotion> EvaluateRuleAsync(PromotionRule rule, IList<ShoppingCartItem> cart)
    {
        var storeId = cart?.FirstOrDefault()?.StoreId ?? 0;
        var context = await _promotionEvaluationContextFactory.BuildDefaultEvaluationContextAsync(cart, storeId);
        return await _promotionRuleEvaluator.EvaluateRuleAsync(rule, cart, context);
    }

    public async Task<bool> EvaluateDiscountRequirementAsync(int discountRequirementId, Customer customer, int storeId = 0)
    {
        if (discountRequirementId <= 0 || customer == null || customer.Deleted)
            return false;

        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(storeId);
        if (!settings.IsEnabled || !settings.UseDefaultDiscountPipeline)
            return false;

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
        if (!cart.Any())
            return false;

        var conditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
        if (!conditions.Any())
            return false;

        var evaluationContext = await _promotionEvaluationContextFactory.BuildDefaultEvaluationContextAsync(cart, storeId);
        return await _promotionConditionEvaluator.EvaluateRuleConditionsAsync(
            PromotionRuleType.CartCondition,
            conditions,
            cart,
            false,
            storeId,
            evaluationContext);
    }

    public async Task<bool> EvaluateLinkedDiscountRequirementAsync(int discountRequirementId, Customer customer, int storeId = 0)
    {
        if (discountRequirementId <= 0 || customer == null || customer.Deleted)
            return false;

        var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId);
        if (discountRequirement == null)
            return false;

        return !await ShouldSuppressLinkedDiscountAsync(discountRequirement.DiscountId, customer, storeId);
    }

    public async Task<bool> IsLinkedDiscountEligibleAsync(PromotionRule rule, PromotionEvaluationContext context)
    {
        var parentDiscountId = PromotionRuntimeHelper.GetParentDiscountId(rule);
        if (rule == null || parentDiscountId <= 0)
            return true;

        var discount = await _discountService.GetDiscountByIdAsync(parentDiscountId);
        if (discount == null)
            return false;

        var customerId = context?.CustomerId ?? context?.Cart?.FirstOrDefault()?.CustomerId ?? 0;
        if (customerId <= 0)
            return false;

        var customer = await _customerService.GetCustomerByIdAsync(customerId);
        if (customer == null || customer.Deleted)
            return false;

        var ignoredRequirementIds = new HashSet<int>();
        var requirements = await _discountService.GetAllDiscountRequirementsAsync(discount.Id);
        foreach (var requirement in requirements)
        {
            if (await _discountManagerPlusRequirementService.GetRequirementKindAsync(requirement.Id) == DiscountManagerPlusRequirementKind.LinkedDiscountCarry)
                ignoredRequirementIds.Add(requirement.Id);
        }

        return await ValidateLinkedDiscountAsync(
            discount,
            customer,
            context?.CouponCodes?.ToArray() ?? Array.Empty<string>(),
            context?.StoreId ?? 0,
            ignoredRequirementIds);
    }

    public async Task<bool> ShouldSuppressLinkedDiscountAsync(int discountId, Customer customer, int storeId = 0)
    {
        if (discountId <= 0 || customer == null || customer.Deleted)
            return false;

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
        if (!cart.Any())
            return false;

        var context = await _promotionEvaluationContextFactory.BuildDefaultEvaluationContextAsync(cart, storeId);
        var linkedRules = await _promotionRuleService.GetSuppressingPromotionRulesByDiscountIdAsync(discountId);
        foreach (var linkedRule in linkedRules)
        {
            if (!await IsRuleRuntimeEligibleAsync(linkedRule, context))
                continue;

            if (await _promotionRuleEvaluator.EvaluateRuleAsync(linkedRule, cart, context) != null)
                return true;
        }

        return false;
    }

    public Task<decimal> GetCartSubtotalAsync(IList<ShoppingCartItem> cart)
    {
        return _promotionDiscountAllocator.GetCartSubtotalAsync(cart);
    }

    public async Task SynchronizeAutoAddedRewardsAsync(Customer customer, int storeId = 0)
    {
        if (customer == null || customer.Id <= 0)
            return;

        try
        {
            var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(storeId);
            if (!settings.IsEnabled)
                return;

            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            if (!cart.Any())
                return;

            var appliedPromotions = await EvaluateCartAsync(cart, storeId);
            await _rewardSynchronizationService.SynchronizeAutoAddedRewardsAsync(customer, cart, appliedPromotions, storeId);
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("DiscountManagerPlusService: error synchronizing auto-added rewards.", ex);
        }
    }

    private async Task<bool> ValidateLinkedDiscountAsync(
        Nop.Core.Domain.Discounts.Discount discount,
        Customer customer,
        string[] couponCodesToValidate,
        int storeId,
        ISet<int> ignoredRequirementIds)
    {
        if (discount == null || customer == null || customer.Deleted)
            return false;

        if (!discount.IsActive)
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
            var store = await _storeContext.GetCurrentStoreAsync();
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId > 0 ? storeId : store.Id);
            if (await _productService.HasAnyGiftCardProductAsync(cart.Select(x => x.ProductId).ToArray()))
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
                var usedTimes = (await _discountService.GetAllDiscountUsageHistoryAsync(discount.Id, null, null, false, 0, 1)).TotalCount;
                if (usedTimes >= discount.LimitationTimes)
                    return false;
                break;
            }
            case Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer:
            {
                if (await _customerService.IsRegisteredAsync(customer))
                {
                    var usedTimes = (await _discountService.GetAllDiscountUsageHistoryAsync(discount.Id, customer.Id, null, false, 0, 1)).TotalCount;
                    if (usedTimes >= discount.LimitationTimes)
                        return false;
                }
                break;
            }
        }

        var requirements = await _discountService.GetAllDiscountRequirementsAsync(discount.Id);
        var topLevelGroup = requirements.FirstOrDefault(x => !x.ParentId.HasValue);
        if (topLevelGroup == null ||
            !topLevelGroup.InteractionType.HasValue ||
            (topLevelGroup.IsGroup && requirements.All(x => x.ParentId != topLevelGroup.Id)))
        {
            return true;
        }

        return await GetLinkedDiscountValidationResultAsync(
            requirements,
            topLevelGroup.InteractionType.Value,
            customer,
            storeId,
            ignoredRequirementIds ?? new HashSet<int>());
    }

    private async Task<bool> GetLinkedDiscountValidationResultAsync(
        IList<Nop.Core.Domain.Discounts.DiscountRequirement> requirements,
        Nop.Core.Domain.Discounts.RequirementGroupInteractionType groupInteractionType,
        Customer customer,
        int storeId,
        ISet<int> ignoredRequirementIds)
    {
        var result = false;
        var hasEvaluatedRequirement = false;
        var requirementsForCheck = requirements.Any(x => !x.ParentId.HasValue)
            ? requirements.Where(x => !x.ParentId.HasValue)
            : requirements;

        foreach (var requirement in requirementsForCheck)
        {
            if (ignoredRequirementIds.Contains(requirement.Id))
                continue;

            hasEvaluatedRequirement = true;
            if (requirement.IsGroup)
            {
                var childRequirements = requirements.Where(x => x.ParentId == requirement.Id).ToList();
                var interactionType = requirement.InteractionType ?? Nop.Core.Domain.Discounts.RequirementGroupInteractionType.And;
                result = await GetLinkedDiscountValidationResultAsync(
                    childRequirements,
                    interactionType,
                    customer,
                    storeId,
                    ignoredRequirementIds);
            }
            else
            {
                var requirementRulePlugin = await _discountPluginManager
                    .LoadPluginBySystemNameAsync(requirement.DiscountRequirementRuleSystemName, customer, storeId);
                if (requirementRulePlugin == null)
                    continue;

                var store = await _storeContext.GetCurrentStoreAsync();
                var ruleResult = await requirementRulePlugin.CheckRequirementAsync(new DiscountRequirementValidationRequest
                {
                    DiscountRequirementId = requirement.Id,
                    Customer = customer,
                    Store = store
                });

                result = ruleResult.IsValid;
            }

            if (!result && groupInteractionType == Nop.Core.Domain.Discounts.RequirementGroupInteractionType.And)
                return false;

            if (result && groupInteractionType == Nop.Core.Domain.Discounts.RequirementGroupInteractionType.Or)
                return true;
        }

        return !hasEvaluatedRequirement || result;
    }

    private async Task<bool> IsRuleRuntimeEligibleAsync(PromotionRule rule, PromotionEvaluationContext context)
    {
        if (rule == null || !rule.IsActive)
            return false;

        context ??= new PromotionEvaluationContext
        {
            CurrentUtc = DateTime.UtcNow
        };

        var now = context.CurrentUtc;
        if (rule.StartDateUtc.HasValue && now < rule.StartDateUtc.Value)
            return false;

        if (rule.EndDateUtc.HasValue && now > rule.EndDateUtc.Value)
            return false;

        if (!await IsRuleAvailableInStoreAsync(rule, context.StoreId))
            return false;

        var parentDiscountId = PromotionRuntimeHelper.GetParentDiscountId(rule);
        if (parentDiscountId > 0)
            return await IsLinkedDiscountEligibleAsync(rule, context);

        if (!rule.IsFlashEnabled)
            return true;

        if (rule.UsageWindowStartUtc.HasValue && now < rule.UsageWindowStartUtc.Value)
            return false;

        if (rule.UsageWindowEndUtc.HasValue && now > rule.UsageWindowEndUtc.Value)
            return false;

        var usageStartUtc = rule.UsageWindowStartUtc;
        if (rule.UsageLimitTotal > 0)
        {
            var totalUsageCount = await _promotionRuleService.GetRuleUsageCountAsync(rule.Id, usageStartUtc);
            if (totalUsageCount >= rule.UsageLimitTotal)
                return false;
        }

        if (rule.UsageLimitPerCustomer > 0)
        {
            if (context.CustomerId <= 0)
                return false;

            var customerUsageCount = await _promotionRuleService.GetRuleUsageCountAsync(rule.Id, usageStartUtc, context.CustomerId);
            if (customerUsageCount >= rule.UsageLimitPerCustomer)
                return false;
        }

        return await IsLinkedDiscountEligibleAsync(rule, context);
    }

    private async Task<bool> IsRuleAvailableInStoreAsync(PromotionRule rule, int storeId)
    {
        if (rule == null)
            return false;

        if (rule.LimitedToStores)
            return await _storeMappingService.AuthorizeAsync(rule, storeId);

        return rule.LimitedToStore <= 0 || storeId == 0 || rule.LimitedToStore == storeId;
    }

    private static string NormalizeCouponCode(string couponCode)
    {
        return string.IsNullOrWhiteSpace(couponCode) ? string.Empty : couponCode.Trim().ToUpperInvariant();
    }

    private string PrepareLastAppliedPromotionsCacheKey(int storeId, int customerId)
    {
        return $"{LastAppliedPromotionsCacheKeyPrefix}.{storeId}.{customerId}";
    }

    /// <summary>
    /// Gets coordinated discount allocations for dual-offer scenarios
    /// Returns a flat list of all discount allocations across all promotions
    /// </summary>
    public async Task<IList<DiscountAllocation>> GetCoordinatedAllocationsAsync(IList<ShoppingCartItem> cart, int storeId)
    {
        var allAllocations = new List<DiscountAllocation>();

        try
        {
            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: GetCoordinatedAllocations called for {cart?.Count ?? 0} cart items");

            if (cart == null || !cart.Any())
            {
                System.Diagnostics.Debug.WriteLine("DISCOUNT_DEBUG: No cart items, returning empty");
                return allAllocations;
            }

            var appliedPromotions = await EvaluateCartAsync(cart, storeId);
            if (appliedPromotions == null || !appliedPromotions.Any())
            {
                System.Diagnostics.Debug.WriteLine("DISCOUNT_DEBUG: No applied promotions, returning empty");
                return allAllocations;
            }

            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Found {appliedPromotions.Count} applied promotions");

            // Check if we have multiple BuyXGetY promotions that need coordination
            var buyXGetYPromotions = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Found {buyXGetYPromotions.Count} BuyXGetY promotions");

            if (buyXGetYPromotions.Count < 2)
            {
                System.Diagnostics.Debug.WriteLine("DISCOUNT_DEBUG: Less than 2 BuyXGetY promotions, no coordination needed");
                return allAllocations;
            }

            System.Diagnostics.Debug.WriteLine("DISCOUNT_DEBUG: Calling coordination service...");

            // Get coordinated allocations from the coordination service
            var coordinatedAllocations = await _discountCoordinationService.CoordinateCheapestItemDiscountsAsync(
                appliedPromotions,
                cart);

            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Got {coordinatedAllocations.Count} coordinated allocations");

            // Flatten the coordinated allocations into a single list
            foreach (var kvp in coordinatedAllocations)
            {
                System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Promotion {kvp.Key} has {kvp.Value.Count} allocations");
                foreach (var allocation in kvp.Value)
                {
                    System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG:   - Line {allocation.LineId}, Discount ${allocation.DiscountAmount}, IsFree {allocation.IsFreeItem}");
                    allAllocations.Add(allocation);
                }
            }

            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: Total flat allocations: {allAllocations.Count}");
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Error getting coordinated allocations", ex);
            System.Diagnostics.Debug.WriteLine($"DISCOUNT_DEBUG: ERROR: {ex.Message}");
        }

        return allAllocations;
    }

}
