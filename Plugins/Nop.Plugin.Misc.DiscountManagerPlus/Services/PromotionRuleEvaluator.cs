// Ported from nopCommerce 4.9 Discount Manager Plus services. C# 6 / nopCommerce 3.70.
using System.Linq;
using System.Collections.Generic;
using System;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{

    public class PromotionRuleEvaluator : IPromotionRuleEvaluator
    {
        private readonly IPromotionConditionEvaluator _promotionConditionEvaluator;
        private readonly IPromotionDiscountAllocator _promotionDiscountAllocator;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IProductService _productService;
        private readonly IRewardSynchronizationService _rewardSynchronizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IDiscountCoordinationService _discountCoordinationService;
        private readonly IPromotionRuleExcludedProductService _excludedProductService;
        private readonly ILogger _logger;

        public PromotionRuleEvaluator(
            IPromotionConditionEvaluator promotionConditionEvaluator,
            IPromotionDiscountAllocator promotionDiscountAllocator,
            IPromotionRuleService promotionRuleService,
            IProductService productService,
            IRewardSynchronizationService rewardSynchronizationService,
            IPriceCalculationService priceCalculationService,
            IDiscountCoordinationService discountCoordinationService,
            IPromotionRuleExcludedProductService excludedProductService,
            ILogger logger)
        {
            _promotionConditionEvaluator = promotionConditionEvaluator;
            _promotionDiscountAllocator = promotionDiscountAllocator;
            _promotionRuleService = promotionRuleService;
            _productService = productService;
            _rewardSynchronizationService = rewardSynchronizationService;
            _priceCalculationService = priceCalculationService;
            _discountCoordinationService = discountCoordinationService;
            _excludedProductService = excludedProductService;
            _logger = logger;
        }

        public AppliedPromotion EvaluateRule(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionEvaluationContext context)
        {
            if (rule == null || !rule.IsActive || cart == null || !cart.Any())
                return null;

            try
            {
                switch (rule.RuleType)
                {
                    case PromotionRuleType.ProductBased:
                        return EvaluateProductBasedRuleInternal(rule, cart, context);

                    case PromotionRuleType.ComboPricing:
                        return EvaluateComboPricingRule(rule, cart, context);

                    case PromotionRuleType.BuyXGetY:
                        return EvaluateBuyXGetYRule(rule, cart, context);

                    case PromotionRuleType.CartCondition:
                        return EvaluateCartConditionRule(rule, cart, context);

                    case PromotionRuleType.SubtotalBased:
                        return EvaluateSubtotalRuleInternal(rule, cart, context);

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"DiscountManagerPlusService: error evaluating rule {rule.Id}.", ex);
                return null;
            }
        }

        private AppliedPromotion EvaluateProductBasedRuleInternal(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context)
        {
            var ruleConditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
            var excludedProductIds = GetAllExcludedProductIds(rule.Id, ruleConditions);
            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
            if (!EvaluateOptionalRuleConditions(rule, cart, context, ruleConditions, ignoreExcludedProducts: true))
                return null;

            var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
            var qualifiedBuyProducts = _promotionConditionEvaluator.GetQualifiedProductBasedRuleProducts(cart, buyProducts);
            if (!qualifiedBuyProducts.Any())
                return null;

            var matchedBuyItems = FilterExcludedCartItems(
                _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, qualifiedBuyProducts),
                excludedProductIds);
            if (!matchedBuyItems.Any())
                return null;

            var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            var tierMappings = _promotionRuleService.GetRuleTierProductMappingsByRuleId(rule.Id);
            var qualifiedBuyProductIds = qualifiedBuyProducts.Select(x => x.Id).ToList();

            if (rule.DiscountType != DiscountType.FreeItem)
            {
                var filteredTiers = _promotionConditionEvaluator.FilterTiersByMatchedRuleProducts(tiers, tierMappings, qualifiedBuyProductIds);
                if (tiers.Any() && tierMappings.Any() && !filteredTiers.Any())
                    return null;

                PromotionRuleTier matchingTier = null;
                IList<ShoppingCartItem> discountTargetItems;

                if (filteredTiers.Any())
                {
                    discountTargetItems = new ShoppingCartItem[0];
                    foreach (var candidateTier in filteredTiers.OrderByDescending(x => x.MinQuantity))
                    {
                        var candidateTierProducts = _promotionConditionEvaluator.FilterRuleProductsByTierMapping(qualifiedBuyProducts, candidateTier, tierMappings);
                        if (!candidateTierProducts.Any())
                            continue;

                        var candidateTierItems = FilterExcludedCartItems(
                            _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, candidateTierProducts),
                            excludedProductIds);
                        if (!candidateTierItems.Any())
                            continue;

                        var candidateMetric = candidateTierItems.Sum(x => x.Quantity);
                        if (candidateMetric < candidateTier.MinQuantity ||
                            (candidateTier.MaxQuantity > 0 && candidateMetric > candidateTier.MaxQuantity))
                        {
                            continue;
                        }

                        matchingTier = candidateTier;
                        discountTargetItems = candidateTierItems;
                        break;
                    }

                    if (matchingTier == null)
                        return null;
                }
                else
                {
                    discountTargetItems = matchedBuyItems;
                }

                if (!discountTargetItems.Any())
                    return null;

                var discountType = matchingTier?.DiscountType ?? rule.DiscountType;
                var discountValue = matchingTier?.DiscountValue ?? rule.DiscountValue;

                decimal calculatedDiscount;
                if (discountType == DiscountType.Percentage)
                {
                    var matchedSubtotal = _promotionDiscountAllocator.GetItemsSubtotal(discountTargetItems);
                    calculatedDiscount = matchedSubtotal * (discountValue / 100m);
                }
                else if (discountType == DiscountType.FixedAmount)
                {
                    calculatedDiscount = discountValue;
                }
                else
                {
                    return null;
                }

                if (calculatedDiscount <= 0)
                    return null;

                var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, calculatedDiscount);
                var lineDiscounts = _promotionDiscountAllocator.AllocateDiscountAcrossItems(calculatedDiscount, discountTargetItems);
                if (!lineDiscounts.Any())
                    return null;

                appliedPromotion.LineDiscounts = lineDiscounts;
                appliedPromotion.DiscountAmount = lineDiscounts.Sum(x => x.Value);
                PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, discountTargetItems);
                return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, appliedPromotion, cart);
            }

            var rewardProducts = ruleProducts.Where(x => x.IsRewardProduct).ToList();
            if (!rewardProducts.Any())
                return null;

            PromotionRuleTier freeItemTier = null;
            if (tiers.Any())
            {
                var filteredTiers = _promotionConditionEvaluator.FilterTiersByMatchedRuleProducts(tiers, tierMappings, qualifiedBuyProductIds);
                if (tierMappings.Any() && !filteredTiers.Any())
                    return null;

                if (filteredTiers.Any())
                {
                    IList<ShoppingCartItem> tierMatchedBuyItems = new ShoppingCartItem[0];
                    foreach (var candidateTier in filteredTiers.OrderByDescending(x => x.MinQuantity))
                    {
                        var candidateTierProducts = _promotionConditionEvaluator.FilterRuleProductsByTierMapping(qualifiedBuyProducts, candidateTier, tierMappings);
                        if (!candidateTierProducts.Any())
                            continue;

                        var candidateTierItems = FilterExcludedCartItems(
                            _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, candidateTierProducts),
                            excludedProductIds);
                        if (!candidateTierItems.Any())
                            continue;

                        var candidateMetric = candidateTierItems.Sum(x => x.Quantity);
                        if (candidateMetric < candidateTier.MinQuantity ||
                            (candidateTier.MaxQuantity > 0 && candidateMetric > candidateTier.MaxQuantity))
                        {
                            continue;
                        }

                        freeItemTier = candidateTier;
                        tierMatchedBuyItems = candidateTierItems;
                        break;
                    }

                    if (freeItemTier == null)
                        return null;

                    matchedBuyItems = tierMatchedBuyItems;
                }
            }

            var rewardItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, rewardProducts);
            var rewardRuleProduct = rewardProducts.FirstOrDefault();
            var rewardQuantity = freeItemTier?.RewardQuantity > 0 ? freeItemTier.RewardQuantity : 1;
            if (rewardItems.Any())
            {
                var rewardDiscountResult = _promotionDiscountAllocator.CalculateRewardDiscounts(rewardItems, rewardQuantity);
                var rewardDiscount = rewardDiscountResult.DiscountAmount;
                if (rewardDiscount <= 0)
                    return null;

                var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, freeItemTier, rewardDiscount);
                appliedPromotion.RewardQuantity = rewardQuantity;
                PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, rewardItems);
                return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, appliedPromotion, cart);
            }

            if (rewardRuleProduct == null)
                return null;

            var rewardProductId = rewardRuleProduct.ProductId;
            if (rewardProductId <= 0)
            {
                var pendingSelectionPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, freeItemTier, 0);
                pendingSelectionPromotion.RewardQuantity = rewardQuantity;
                pendingSelectionPromotion.RequiresRewardSelection = true;
                PromotionRuntimeHelper.SetEligibleCartItems(pendingSelectionPromotion, matchedBuyItems);
                return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, pendingSelectionPromotion, cart);
            }

            var rewardProduct = _productService.GetProductById(rewardProductId);
            if (_rewardSynchronizationService.RequiresRewardSelection(rewardRuleProduct, rewardProduct))
            {
                var pendingPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, freeItemTier, 0);
                pendingPromotion.RewardQuantity = rewardQuantity;
                pendingPromotion.RewardProductId = rewardProductId;
                pendingPromotion.RequiresRewardSelection = true;
                PromotionRuntimeHelper.SetEligibleCartItems(pendingPromotion, matchedBuyItems);
                return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, pendingPromotion, cart);
            }

            var autoAddPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, freeItemTier, 0);
            autoAddPromotion.RewardQuantity = rewardQuantity;
            autoAddPromotion.RewardProductId = rewardProductId;
            autoAddPromotion.AutoAddReward = true;
            PromotionRuntimeHelper.SetEligibleCartItems(autoAddPromotion, matchedBuyItems);
            return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, autoAddPromotion, cart);
        }

        private AppliedPromotion EvaluateComboPricingRule(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context)
        {
            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
            var buyProducts = ruleProducts.ToList();

            // Get excluded product IDs for this rule
            var ruleConditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
            var excludedProductIds = GetAllExcludedProductIds(rule.Id, ruleConditions);

            if (!EvaluateOptionalRuleConditions(rule, cart, context, ruleConditions, ignoreExcludedProducts: true))
                return null;

            if (!EvaluateProductBasedRule(rule, cart, ruleProducts))
                return null;

            var comboSummary = _promotionConditionEvaluator.GetComboSetMatchSummary(cart, buyProducts);
            var setCount = comboSummary.SetCount;
            var matchedSubtotal = comboSummary.MatchedSubtotal;
            if (setCount <= 0 || matchedSubtotal <= 0)
                return null;

            var matchedItems = FilterExcludedCartItems(
                _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts),
                excludedProductIds);
            if (!matchedItems.Any())
                return null;

            var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            var tierMappings = _promotionRuleService.GetRuleTierProductMappingsByRuleId(rule.Id);
            var qualifiedBuyProducts = _promotionConditionEvaluator.GetQualifiedProductBasedRuleProducts(cart, buyProducts);
            var qualifiedBuyProductIds = qualifiedBuyProducts.Select(x => x.Id).ToList();
            var filteredTiers = _promotionConditionEvaluator.FilterTiersByMatchedRuleProducts(tiers, tierMappings, qualifiedBuyProductIds);
            if (tiers.Any() && tierMappings.Any() && !filteredTiers.Any())
                return null;

            var discountType = rule.DiscountType;
            var discountValue = rule.DiscountValue;
            PromotionRuleTier matchingTier = null;
            if (filteredTiers.Any())
            {
                matchingTier = filteredTiers
                    .Where(t => setCount >= t.MinQuantity && (t.MaxQuantity == 0 || setCount <= t.MaxQuantity))
                    .OrderByDescending(t => t.MinQuantity)
                    .FirstOrDefault();
                if (matchingTier == null)
                {
                    var totalMatchedQty = matchedItems.Sum(x => x.Quantity);
                    matchingTier = filteredTiers
                        .Where(t => totalMatchedQty >= t.MinQuantity && (t.MaxQuantity == 0 || totalMatchedQty <= t.MaxQuantity))
                        .OrderByDescending(t => t.MinQuantity)
                        .FirstOrDefault();
                }

                if (matchingTier == null)
                    return null;

                discountType = matchingTier.DiscountType;
                discountValue = matchingTier.DiscountValue;
            }
            else if (discountValue <= 0)
            {
                return null;
            }

            var discountBuyProducts = _promotionConditionEvaluator.FilterRuleProductsByTierMapping(buyProducts, matchingTier, tierMappings);
            if (!discountBuyProducts.Any())
                return null;

            comboSummary = _promotionConditionEvaluator.GetComboSetMatchSummary(cart, discountBuyProducts);
            setCount = comboSummary.SetCount;
            matchedSubtotal = comboSummary.MatchedSubtotal;
            matchedItems = FilterExcludedCartItems(
                _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, discountBuyProducts),
                excludedProductIds);
            if (setCount <= 0 || matchedSubtotal <= 0 || !matchedItems.Any())
                return null;

            decimal calculatedDiscount;
            if (discountType == DiscountType.FixedBundlePrice)
            {
                if (discountValue <= 0)
                    return null;

                calculatedDiscount = matchedSubtotal - (discountValue * setCount);
            }
            else if (discountType == DiscountType.Percentage)
            {
                calculatedDiscount = matchedSubtotal * (discountValue / 100m);
            }
            else if (discountType == DiscountType.FixedAmount)
            {
                calculatedDiscount = discountValue * setCount;
            }
            else
            {
                calculatedDiscount = 0m;
            }

            if (calculatedDiscount <= 0)
            {
                if (discountType != DiscountType.FixedBundlePrice && discountType != DiscountType.Percentage && discountType != DiscountType.FixedAmount)
                {
                    var fallbackAppliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier);
                    PromotionRuntimeHelper.SetEligibleCartItems(fallbackAppliedPromotion, matchedItems);
                    return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, fallbackAppliedPromotion, cart);
                }

                return null;
            }

            var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, calculatedDiscount);
            PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, matchedItems);
            appliedPromotion.DiscountedQuantitiesByLineId = comboSummary.QuantitiesByLineId;
            return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, appliedPromotion, cart);
        }

        private AppliedPromotion EvaluateBuyXGetYRule(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context)
        {
            var tiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
            var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
            var tierMappings = _promotionRuleService.GetRuleTierProductMappingsByRuleId(rule.Id);

            // Get excluded product IDs for this rule
            var ruleConditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
            var excludedProductIds = GetAllExcludedProductIds(rule.Id, ruleConditions);

            if (!EvaluateOptionalRuleConditions(rule, cart, context, ruleConditions, ignoreExcludedProducts: true))
                return null;

            if (!tiers.Any())
                return null;

            if (buyProducts.Any(x => x.IsAllProducts))
                return EvaluateProgressiveTieredBuyXGetY(rule, cart, tiers, buyProducts);

            if (!EvaluateProductBasedRule(rule, cart, ruleProducts))
                return null;

            var qualifiedBuyProducts = _promotionConditionEvaluator.GetQualifiedProductBasedRuleProducts(cart, buyProducts);
            var qualifiedBuyProductIds = qualifiedBuyProducts.Select(x => x.Id).ToList();
            var filteredTiers = _promotionConditionEvaluator.FilterTiersByMatchedRuleProducts(tiers, tierMappings, qualifiedBuyProductIds);
            if (!filteredTiers.Any())
                return null;

            if (rule.EnableStackedCumulativeMode && !tierMappings.Any())
            {
                var stackedAppliedPromotion = BuildStackedCumulativeBuyXGetYAppliedPromotion(
                    rule,
                    cart,
                    ruleProducts,
                    buyProducts,
                    filteredTiers);
                if (stackedAppliedPromotion != null)
                    return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, stackedAppliedPromotion, cart);
            }

            if (tierMappings.Any())
            {
                var mappedTierMatches = GetBuyXGetYTierMatches(cart, buyProducts, ruleProducts, filteredTiers, tierMappings);
                var mappedAppliedPromotion = BuildMultiTierBuyXGetYAppliedPromotion(rule, cart, ruleProducts, mappedTierMatches);
                if (mappedAppliedPromotion != null)
                    return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, mappedAppliedPromotion, cart);
            }

            PromotionRuleTier matchingTier = null;
            IList<ShoppingCartItem> matchedBuyItems = new ShoppingCartItem[0];
            BuyXGetYRewardContext rewardContext = null;

            foreach (var candidateTier in filteredTiers.OrderByDescending(t => t.MinQuantity))
            {
                var tierScopedBuyProducts = _promotionConditionEvaluator.FilterRuleProductsByTierMapping(buyProducts, candidateTier, tierMappings);
                if (!tierScopedBuyProducts.Any())
                    continue;

                var candidateMatchedBuyItems = FilterExcludedCartItems(
                    _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, tierScopedBuyProducts),
                    excludedProductIds);
                if (!candidateMatchedBuyItems.Any())
                    continue;

                var candidateQualifyingBuyQuantity = candidateMatchedBuyItems.Sum(x => x.Quantity);
                if (tierScopedBuyProducts.Count > 1)
                {
                    var buySummary = _promotionConditionEvaluator.GetComboSetMatchSummary(cart, tierScopedBuyProducts);
                    candidateQualifyingBuyQuantity = buySummary.SetCount;
                }

                if (candidateQualifyingBuyQuantity <= 0)
                    continue;

                var hasExplicitRewardScope = HasExplicitRewardScope(ruleProducts, candidateTier);
                var candidateRewardQuantity = ResolveBuyXGetYRewardQuantity(candidateTier, candidateQualifyingBuyQuantity, hasExplicitRewardScope);
                if (candidateRewardQuantity <= 0)
                    continue;

                var candidateRewardContext = BuildBuyXGetYRewardContext(
                    cart,
                    ruleProducts,
                    candidateMatchedBuyItems,
                    candidateTier,
                    candidateRewardQuantity,
                    candidateQualifyingBuyQuantity,
                    tierScopedBuyProducts.Count > 1,
                    hasExplicitRewardScope);

                if (candidateRewardContext.TotalEligibleQuantity < candidateTier.MinQuantity ||
                    (candidateTier.MaxQuantity > 0 && candidateRewardContext.TotalEligibleQuantity > candidateTier.MaxQuantity))
                {
                    continue;
                }

                matchingTier = candidateTier;
                matchedBuyItems = candidateMatchedBuyItems;
                rewardContext = candidateRewardContext;
                break;
            }

            if (matchingTier == null || rewardContext == null)
                return null;

            if (matchingTier.DiscountType == DiscountType.FreeItem)
                return BuildFreeItemBuyXGetYPromotion(rule, cart, ruleProducts, matchedBuyItems, matchingTier, rewardContext);

            return BuildDiscountedRewardBuyXGetYPromotion(rule, cart, ruleProducts, matchingTier, rewardContext);
        }

        private AppliedPromotion EvaluateCartConditionRule(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context)
        {
            var cartConditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);

            // Get excluded product IDs for this rule
            var excludedProductIds = GetAllExcludedProductIds(rule.Id, cartConditions);

            var cartConditionResult = _promotionConditionEvaluator.EvaluateRuleConditionsWithQuantity(
                rule.RuleType,
                cartConditions,
                cart,
                true,
                ResolveEvaluationStoreId(cart, context),
                context);
            if (!cartConditionResult.IsMatched)
                return null;

            var cartTiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            decimal? cartMetric = null;
            if (cartConditionResult.HasQuantityMetric)
            {
                cartMetric = cartConditionResult.MatchedQuantity;
            }
            else if (!_promotionConditionEvaluator.HasSpecificCartConditionQuantityCriteria(cartConditions))
            {
                cartMetric = cart.Sum(x => x.Quantity);
            }
            else if (cartTiers.Any())
            {
                cartMetric = cart.Sum(x => x.Quantity);
            }

            var cartTier = cartMetric.HasValue
                ? _promotionConditionEvaluator.GetMatchingTierByMetric(cartTiers, cartMetric.Value)
                : null;
            if (cartTiers.Any() && cartTier == null)
                return null;

            var eligibleCartItems = GetCartConditionEligibleItems(rule, cartConditionResult, cart, excludedProductIds);
            var cartAppliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, eligibleCartItems, cartTier);
            PromotionRuntimeHelper.SetEligibleCartItems(cartAppliedPromotion, eligibleCartItems);
            return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, cartAppliedPromotion, cart);
        }

        private static IList<ShoppingCartItem> GetCartConditionEligibleItems(
            PromotionRule rule,
            ConditionMatchResult conditionResult,
            IList<ShoppingCartItem> cart,
            IList<int> excludedProductIds)
        {
            if (cart == null || !cart.Any())
                return new ShoppingCartItem[0];

            var eligibleItems = rule?.DiscountScope == DiscountScope.MatchedItemsOnly &&
                conditionResult?.MatchedShoppingCartItemIds != null &&
                conditionResult.MatchedShoppingCartItemIds.Any()
                    ? cart.Where(x => x.Quantity > 0 && conditionResult.MatchedShoppingCartItemIds.Contains(x.Id)).ToList()
                    : cart.Where(x => x.Quantity > 0).ToList();

            // Filter out excluded products
            return FilterExcludedCartItems(eligibleItems, excludedProductIds);
        }

        private AppliedPromotion EvaluateSubtotalRuleInternal(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context)
        {
            // Get excluded product IDs for this rule
            var ruleConditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
            var excludedProductIds = GetAllExcludedProductIds(rule.Id, ruleConditions);

            if (!EvaluateOptionalRuleConditions(rule, cart, context, ruleConditions, ignoreExcludedProducts: true))
                return null;

            var subtotalTiers = _promotionRuleService.GetRuleTiersByRuleId(rule.Id);
            var subtotalMetric = _promotionDiscountAllocator.GetCartSubtotal(cart);
            var subtotalTier = _promotionConditionEvaluator.GetMatchingTierByMetric(subtotalTiers, subtotalMetric);
            if (subtotalTiers.Any() && subtotalTier == null)
                return null;

            var eligibleCartItems = FilterExcludedCartItems(
                cart.Where(x => x.Quantity > 0).ToList(),
                excludedProductIds);
            var subtotalAppliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, eligibleCartItems, subtotalTier);
            PromotionRuntimeHelper.SetEligibleCartItems(subtotalAppliedPromotion, eligibleCartItems);
            return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, subtotalAppliedPromotion, cart);
        }

        private AppliedPromotion BuildFreeItemBuyXGetYPromotion(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts,
            IList<ShoppingCartItem> matchedBuyItems,
            PromotionRuleTier matchingTier,
            BuyXGetYRewardContext rewardContext)
        {
            if (rewardContext.RewardItems.Any())
            {
                var freeRewardDiscountResult = CalculateScopedRewardDiscounts(
                    rewardContext.RewardItems,
                    rewardContext.RewardQuantity,
                    rewardContext.RewardAvailableQuantitiesByLineId,
                    DiscountType.FreeItem,
                    100m);
                var rewardDiscount = freeRewardDiscountResult.DiscountAmount;
                var freeRewardLineDiscounts = freeRewardDiscountResult.LineDiscounts;
                var freeRewardQuantities = freeRewardDiscountResult.DiscountedQuantitiesByLineId;
                if (rewardDiscount <= 0 || !freeRewardLineDiscounts.Any())
                    return null;

                var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, rewardDiscount);
                appliedPromotion.RewardQuantity = rewardContext.RewardQuantity;
                appliedPromotion.RewardProductId = matchingTier.RewardProductId
                    ?? ruleProducts.Where(x => x.IsRewardProduct).Select(x => (int?)x.ProductId).FirstOrDefault()
                    ?? ResolveRewardProductIdFromLineDiscounts(cart, freeRewardLineDiscounts);
                appliedPromotion.LineDiscounts = freeRewardLineDiscounts;
                appliedPromotion.DiscountedQuantitiesByLineId = freeRewardQuantities;
                var rewardLineIds = freeRewardLineDiscounts.Keys.ToList();
                PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, rewardContext.RewardItems.Where(x => rewardLineIds.Contains(x.Id)).ToList());
                return appliedPromotion;
            }

            var rewardProducts = ruleProducts.Where(x => x.IsRewardProduct).ToList();
            var rewardProductId = matchingTier.RewardProductId ?? rewardProducts.FirstOrDefault()?.ProductId;
            if (!rewardProductId.HasValue || rewardProductId.Value <= 0)
                return null;

            var rewardRuleProduct = rewardProducts.FirstOrDefault(x => x.ProductId == rewardProductId.Value);
            var rewardProduct = _productService.GetProductById(rewardProductId.Value);
            var requiresSelection = _rewardSynchronizationService.RequiresRewardSelection(rewardRuleProduct, rewardProduct);

            if (matchingTier.AutoAddReward)
            {
                if (requiresSelection)
                {
                    var pendingPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, 0);
                    pendingPromotion.RewardQuantity = rewardContext.RewardQuantity;
                    pendingPromotion.RewardProductId = rewardProductId.Value;
                    pendingPromotion.RequiresRewardSelection = true;
                    if (!rewardContext.HasExplicitRewardScope)
                        PromotionRuntimeHelper.SetEligibleCartItems(pendingPromotion, matchedBuyItems);
                    return pendingPromotion;
                }

                var autoAddPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, 0);
                autoAddPromotion.RewardQuantity = rewardContext.RewardQuantity;
                autoAddPromotion.RewardProductId = rewardProductId.Value;
                autoAddPromotion.AutoAddReward = true;
                if (!rewardContext.HasExplicitRewardScope)
                    PromotionRuntimeHelper.SetEligibleCartItems(autoAddPromotion, matchedBuyItems);
                return autoAddPromotion;
            }

            var manualRewardPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, 0);
            manualRewardPromotion.RewardQuantity = rewardContext.RewardQuantity;
            manualRewardPromotion.RewardProductId = rewardProductId.Value;
            manualRewardPromotion.RequiresRewardSelection = true;
            if (!rewardContext.HasExplicitRewardScope)
                PromotionRuntimeHelper.SetEligibleCartItems(manualRewardPromotion, matchedBuyItems);
            return manualRewardPromotion;
        }

        private AppliedPromotion BuildDiscountedRewardBuyXGetYPromotion(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts,
            PromotionRuleTier matchingTier,
            BuyXGetYRewardContext rewardContext)
        {
            if (!rewardContext.RewardItems.Any())
                return null;

            var rewardDiscountResult = CalculateScopedRewardDiscounts(
                rewardContext.RewardItems,
                rewardContext.RewardQuantity,
                rewardContext.RewardAvailableQuantitiesByLineId,
                matchingTier.DiscountType,
                matchingTier.DiscountValue);
            var rewardDiscountAmount = rewardDiscountResult.DiscountAmount;
            var rewardLineDiscounts = rewardDiscountResult.LineDiscounts;
            var rewardLineQuantities = rewardDiscountResult.DiscountedQuantitiesByLineId;
            if (rewardDiscountAmount <= 0 || !rewardLineDiscounts.Any())
                return null;

            var discountedRewardProducts = ruleProducts.Where(x => x.IsRewardProduct).ToList();
            var rewardPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, matchingTier, rewardDiscountAmount);
            rewardPromotion.RewardQuantity = rewardContext.RewardQuantity;
            rewardPromotion.RewardProductId = matchingTier.RewardProductId ?? discountedRewardProducts.FirstOrDefault()?.ProductId;
            rewardPromotion.LineDiscounts = rewardLineDiscounts;
            rewardPromotion.DiscountedQuantitiesByLineId = rewardLineQuantities;
            var rewardLineIds = rewardLineDiscounts.Keys.ToList();
            PromotionRuntimeHelper.SetEligibleCartItems(rewardPromotion, rewardContext.RewardItems.Where(x => rewardLineIds.Contains(x.Id)).ToList());
            return rewardPromotion;
        }

        private bool EvaluateProductBasedRule(PromotionRule rule, IList<ShoppingCartItem> cart, IList<PromotionRuleProduct> ruleProducts = null)
        {
            if (ruleProducts == null)
            ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(rule.Id);
            var buyProducts = rule.RuleType == PromotionRuleType.ComboPricing
                ? ruleProducts.ToList()
                : ruleProducts.Where(x => !x.IsRewardProduct).ToList();

            if (!buyProducts.Any())
                return false;

            if (rule.RuleType == PromotionRuleType.ProductBased)
                return (_promotionConditionEvaluator.GetQualifiedProductBasedRuleProducts(cart, buyProducts)).Any();

            foreach (var ruleProduct in buyProducts)
            {
                var matchedQuantity = (_promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, new List<PromotionRuleProduct> { ruleProduct }))
                    .Sum(x => x.Quantity);
                var requiredQuantity = ruleProduct.MinQuantity > 0 ? ruleProduct.MinQuantity : 1;
                if (matchedQuantity < requiredQuantity)
                    return false;

                if (ruleProduct.MaxQuantity > 0 && matchedQuantity > ruleProduct.MaxQuantity)
                    return false;
            }

            return true;
        }

        private bool EvaluateSubtotalRule(PromotionRule rule, IList<ShoppingCartItem> cart, PromotionEvaluationContext context = null)
        {
            var conditions = _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
            return _promotionConditionEvaluator.EvaluateRuleConditions(
                rule.RuleType,
                conditions,
                cart,
                false,
                ResolveEvaluationStoreId(cart, context),
                context);
        }

        private bool EvaluateOptionalRuleConditions(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            PromotionEvaluationContext context = null,
            IList<PromotionRuleCondition> prefetchedConditions = null,
            bool ignoreExcludedProducts = false)
        {
            var conditions = prefetchedConditions ?? _promotionRuleService.GetRuleConditionsByRuleId(rule.Id);
            if (ignoreExcludedProducts)
                conditions = CloneConditionsWithoutExcludedProducts(conditions);

            return _promotionConditionEvaluator.EvaluateRuleConditions(
                rule.RuleType,
                conditions,
                cart,
                true,
                ResolveEvaluationStoreId(cart, context),
                context);
        }

        private static IList<int> GetExcludedProductIds(IList<PromotionRuleCondition> conditions)
        {
            return conditions?
                .Where(x => x.ExcludedProductId.HasValue && x.ExcludedProductId.Value > 0)
                .Select(x => x.ExcludedProductId.Value)
                .Distinct()
                .ToList() ?? new List<int>();
        }

        /// <summary>
        /// Gets all excluded product IDs from both the new excluded products table and legacy conditions
        /// </summary>
        private IList<int> GetAllExcludedProductIds(int ruleId, IList<PromotionRuleCondition> conditions)
        {
            // Get excluded products from new table (primary source)
            var tableExcludedIds = _excludedProductService.GetExcludedProductIdsByRuleId(ruleId);

            // Get excluded products from conditions (legacy source for backwards compatibility)
            var conditionExcludedIds = GetExcludedProductIds(conditions);

            // Combine both sources and remove duplicates
            return tableExcludedIds.Union(conditionExcludedIds).Distinct().ToList();
        }

        private static IList<ShoppingCartItem> FilterExcludedCartItems(IList<ShoppingCartItem> items, IList<int> excludedProductIds)
        {
            if (items == null || !items.Any() || excludedProductIds == null || !excludedProductIds.Any())
                return items ?? new ShoppingCartItem[0];

            var excludedSet = excludedProductIds.ToList();
            return items
                .Where(x => !excludedSet.Contains(x.ProductId))
                .ToList();
        }

        private static IList<PromotionRuleCondition> CloneConditionsWithoutExcludedProducts(IList<PromotionRuleCondition> conditions)
        {
            if (conditions == null || !conditions.Any())
                return conditions ?? new PromotionRuleCondition[0];

            return conditions.Select(condition => new PromotionRuleCondition
            {
                Id = condition.Id,
                PromotionRuleId = condition.PromotionRuleId,
                DiscountRequirementId = condition.DiscountRequirementId,
                ConditionGroup = condition.ConditionGroup,
                ParentConditionId = condition.ParentConditionId,
                LogicalOperatorId = condition.LogicalOperatorId,
                ConditionOperatorId = condition.ConditionOperatorId,
                MinValue = condition.MinValue,
                MaxValue = condition.MaxValue,
                RequiredProductId = condition.RequiredProductId,
                ExcludedProductId = null,
                RequiredCategoryId = condition.RequiredCategoryId,
                RequiredVendorId = condition.RequiredVendorId,
                RequiredCustomerRoleId = condition.RequiredCustomerRoleId,
                IsFirstOrderOnly = condition.IsFirstOrderOnly,
                IsNewCustomerOnly = condition.IsNewCustomerOnly,
                ConditionRestrictionTypeId = condition.ConditionRestrictionTypeId,
                ConditionSourceTypeId = condition.ConditionSourceTypeId,
                ConditionSourceData = condition.ConditionSourceData,
                QuantityMin = condition.QuantityMin,
                QuantityMax = condition.QuantityMax,
                RequiredCountryCodesCsv = condition.RequiredCountryCodesCsv,
                RequiredPaymentMethodsCsv = condition.RequiredPaymentMethodsCsv,
                RequiredCouponCodesCsv = condition.RequiredCouponCodesCsv,
                RequiredOrderCountMin = condition.RequiredOrderCountMin,
                RequiredOrderCountMax = condition.RequiredOrderCountMax,
                RequireSameLineMatch = condition.RequireSameLineMatch,
                AttributeMatchModeId = condition.AttributeMatchModeId
            }).ToList();
        }

        private IList<BuyXGetYTierMatch> GetBuyXGetYTierMatches(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> buyProducts,
            IList<PromotionRuleProduct> ruleProducts,
            IList<PromotionRuleTier> tiers,
            IList<PromotionRuleTierProductMapping> tierMappings)
        {
            var tierMatches = new List<BuyXGetYTierMatch>();
            if (cart == null || !cart.Any() || buyProducts == null || !buyProducts.Any() || tiers == null || !tiers.Any())
                return tierMatches;

            foreach (var tier in tiers.OrderByDescending(x => x.MinQuantity))
            {
                var tierBuyProducts = _promotionConditionEvaluator.FilterRuleProductsByTierMapping(buyProducts, tier, tierMappings);
                if (!tierBuyProducts.Any())
                    continue;

                var matchedBuyItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, tierBuyProducts);
                if (!matchedBuyItems.Any())
                    continue;

                var qualifyingBuyQuantity = matchedBuyItems.Sum(x => x.Quantity);
                if (tierBuyProducts.Count > 1)
                {
                    var comboCount = _promotionConditionEvaluator.GetComboSetCountAndSubtotal(cart, tierBuyProducts);
                    var buySetCount = comboCount.SetCount;
                    qualifyingBuyQuantity = buySetCount;
                }

                if (qualifyingBuyQuantity <= 0)
                    continue;

                var hasExplicitRewardScope = HasExplicitRewardScope(ruleProducts, tier);
                var rewardQuantity = ResolveBuyXGetYRewardQuantity(tier, qualifyingBuyQuantity, hasExplicitRewardScope);
                if (rewardQuantity <= 0)
                    continue;

                var rewardContext = BuildBuyXGetYRewardContext(
                    cart,
                    ruleProducts,
                    matchedBuyItems,
                    tier,
                    rewardQuantity,
                    qualifyingBuyQuantity,
                    tierBuyProducts.Count > 1,
                    hasExplicitRewardScope);

                if (rewardContext.TotalEligibleQuantity < tier.MinQuantity ||
                    (tier.MaxQuantity > 0 && rewardContext.TotalEligibleQuantity > tier.MaxQuantity))
                {
                    continue;
                }

                tierMatches.Add(new BuyXGetYTierMatch
                {
                    Tier = tier,
                    BuyProducts = tierBuyProducts,
                    MatchedBuyItems = matchedBuyItems,
                    RewardItems = rewardContext.RewardItems,
                    RewardAvailableQuantitiesByLineId = rewardContext.RewardAvailableQuantitiesByLineId,
                    QualifyingBuyQuantity = qualifyingBuyQuantity,
                    TotalEligibleQuantity = rewardContext.TotalEligibleQuantity,
                    RewardQuantity = rewardContext.RewardQuantity,
                    HasExplicitRewardScope = rewardContext.HasExplicitRewardScope
                });
            }

            if (!tierMatches.Any())
                return tierMatches;

            return tierMatches
                .GroupBy(x => string.Join(",", x.BuyProducts.Select(p => p.Id).OrderBy(id => id)))
                .Select(group => group.OrderByDescending(x => x.Tier.MinQuantity).First())
                .OrderByDescending(x => x.Tier.MinQuantity)
                .ToList();
        }

        private AppliedPromotion BuildMultiTierBuyXGetYAppliedPromotion(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts,
            IList<BuyXGetYTierMatch> tierMatches)
        {
            if (rule == null ||
                cart == null || !cart.Any() ||
                ruleProducts == null || !ruleProducts.Any() ||
                tierMatches == null || !tierMatches.Any())
            {
                return null;
            }

            var combinedLineDiscounts = new Dictionary<int, decimal>();
            var combinedDiscountedQuantitiesByLineId = new Dictionary<int, int>();
            var lineSubtotalCache = new Dictionary<int, decimal>();
            var totalRewardQuantity = 0;

            foreach (var tierMatch in tierMatches)
            {
                var rewardItems = tierMatch.RewardItems;
                if (!rewardItems.Any() && tierMatch.Tier.DiscountType == DiscountType.FreeItem && !tierMatch.HasExplicitRewardScope)
                    rewardItems = tierMatch.MatchedBuyItems.Where(x => x.Quantity > 0).ToList();

                if (!rewardItems.Any())
                    continue;

                RewardDiscountQuantizedResult rewardDiscountResult;
                if (tierMatch.Tier.DiscountType == DiscountType.FreeItem)
                {
                    rewardDiscountResult = CalculateScopedRewardDiscounts(
                        rewardItems,
                        tierMatch.RewardQuantity,
                        tierMatch.RewardAvailableQuantitiesByLineId,
                        DiscountType.FreeItem,
                        100m);
                }
                else
                {
                    rewardDiscountResult = CalculateScopedRewardDiscounts(
                        rewardItems,
                        tierMatch.RewardQuantity,
                        tierMatch.RewardAvailableQuantitiesByLineId,
                        tierMatch.Tier.DiscountType,
                        tierMatch.Tier.DiscountValue);
                }

                var rewardDiscountAmount = rewardDiscountResult.DiscountAmount;
                var rewardLineDiscounts = rewardDiscountResult.LineDiscounts;
                var rewardLineQuantities = rewardDiscountResult.DiscountedQuantitiesByLineId;

                if (rewardDiscountAmount <= 0 || !rewardLineDiscounts.Any())
                    continue;

                var appliedAmount = _promotionDiscountAllocator.MergeLineDiscountsWithCap(
                    cart,
                    combinedLineDiscounts,
                    rewardLineDiscounts,
                    lineSubtotalCache);

                if (appliedAmount > 0)
                {
                    totalRewardQuantity += tierMatch.RewardQuantity;
                    foreach (var quantityEntry in rewardLineQuantities)
                    {
                        int existingQuantity;
                        if (combinedDiscountedQuantitiesByLineId.TryGetValue(quantityEntry.Key, out existingQuantity))
                            combinedDiscountedQuantitiesByLineId[quantityEntry.Key] = existingQuantity + quantityEntry.Value;
                        else
                            combinedDiscountedQuantitiesByLineId[quantityEntry.Key] = quantityEntry.Value;
                    }
                }
            }

            if (!combinedLineDiscounts.Any())
                return null;

            var totalDiscountAmount = combinedLineDiscounts.Sum(x => x.Value);
            if (totalDiscountAmount <= 0)
                return null;

            var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, null, totalDiscountAmount);
            appliedPromotion.LineDiscounts = combinedLineDiscounts;
            appliedPromotion.RewardQuantity = totalRewardQuantity;
            appliedPromotion.RewardProductId = ResolveRewardProductIdFromLineDiscounts(cart, combinedLineDiscounts);
            appliedPromotion.DiscountedQuantitiesByLineId = combinedDiscountedQuantitiesByLineId;
            var rewardLineIds = combinedLineDiscounts.Keys.ToList();
            PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, cart.Where(x => rewardLineIds.Contains(x.Id)).ToList());
            return appliedPromotion;
        }

        private static int ResolveBuyXGetYRewardQuantity(PromotionRuleTier tier, int qualifyingBuyQuantity, bool hasExplicitRewardScope)
        {
            if (tier == null || qualifyingBuyQuantity <= 0)
                return 0;

            var rewardQuantityPerCycle = tier.RewardQuantity > 0 ? tier.RewardQuantity : 1;
            if (tier.MaxQuantity > 0)
                return qualifyingBuyQuantity >= tier.MinQuantity ? rewardQuantityPerCycle : 0;

            var cycleSize = hasExplicitRewardScope
                ? tier.MinQuantity
                : tier.MinQuantity + rewardQuantityPerCycle;
            if (cycleSize <= 0)
                return 0;

            var cycleCount = qualifyingBuyQuantity / cycleSize;
            if (cycleCount <= 0)
                return 0;

            return rewardQuantityPerCycle * cycleCount;
        }

        private BuyXGetYRewardContext BuildBuyXGetYRewardContext(
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts,
            IList<ShoppingCartItem> matchedBuyItems,
            PromotionRuleTier tier,
            int rewardQuantity,
            int qualifyingBuyQuantity,
            bool useComboBuyMetric,
            bool hasExplicitRewardScope)
        {
            var rewardProducts = ruleProducts.Where(x => x.IsRewardProduct).ToList();
            var useCheapestQualifiedReward = tier.RewardProductId.HasValue &&
                                             tier.RewardProductId.Value == 0 &&
                                             !rewardProducts.Any();
            var useCheapestCartReward = tier.RewardProductId.HasValue &&
                                        tier.RewardProductId.Value == -1 &&
                                        !rewardProducts.Any();
            var useFallbackCheapestCartReward = !tier.RewardProductId.HasValue &&
                                                !rewardProducts.Any() &&
                                                (tier.DiscountType == DiscountType.FreeItem ||
                                                 tier.DiscountType == DiscountType.Percentage ||
                                                 tier.DiscountType == DiscountType.FixedAmount);

            IList<ShoppingCartItem> rewardItems = new ShoppingCartItem[0];
            if (tier.RewardProductId.HasValue && tier.RewardProductId.Value > 0)
            {
                rewardItems = cart.Where(x => x.ProductId == tier.RewardProductId.Value && x.Quantity > 0).ToList();
            }
            else if (rewardProducts.Any())
            {
                rewardItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, rewardProducts);
            }
            else if (useCheapestQualifiedReward)
            {
                rewardItems = matchedBuyItems.Where(x => x.Quantity > 0).ToList();
                rewardItems = GetLowestPricedItems(rewardItems);
            }
            else if (useCheapestCartReward)
            {
                rewardItems = cart.Where(x => x.Quantity > 0).ToList();
                rewardItems = GetLowestPricedItems(rewardItems);
            }
            else if (useFallbackCheapestCartReward)
            {
                rewardItems = cart.Where(x => x.Quantity > 0).ToList();
                rewardItems = GetLowestPricedItems(rewardItems);
            }
            else if (tier.DiscountType == DiscountType.FreeItem ||
                     tier.DiscountType == DiscountType.Percentage ||
                     tier.DiscountType == DiscountType.FixedAmount)
            {
                rewardItems = matchedBuyItems.Where(x => x.Quantity > 0).ToList();
            }

            var buyLineIds = matchedBuyItems
                .Where(x => x != null)
                .Select(x => x.Id)
                .ToList();
            var hasOverlappingRewardPool = rewardItems
                .Where(x => x != null)
                .Any(x => buyLineIds.Contains(x.Id));

            var effectiveHasExplicitRewardScope = hasExplicitRewardScope && !hasOverlappingRewardPool;
            var effectiveRewardQuantity = ResolveBuyXGetYRewardQuantity(tier, qualifyingBuyQuantity, effectiveHasExplicitRewardScope);
            if (effectiveRewardQuantity <= 0)
                return new BuyXGetYRewardContext();

            var availableRewardQuantitiesByLineId = BuildAvailableRewardQuantitiesByLineId(
                rewardItems,
                matchedBuyItems,
                effectiveRewardQuantity,
                qualifyingBuyQuantity,
                useComboBuyMetric,
                effectiveHasExplicitRewardScope);

            var totalEligibleQuantity = rewardItems.Any()
                ? CalculateDistinctEligibleQuantity(matchedBuyItems, rewardItems, useComboBuyMetric, qualifyingBuyQuantity)
                : qualifyingBuyQuantity + effectiveRewardQuantity;

            return new BuyXGetYRewardContext
            {
                RewardItems = rewardItems,
                RewardAvailableQuantitiesByLineId = availableRewardQuantitiesByLineId,
                RewardQuantity = effectiveRewardQuantity,
                TotalEligibleQuantity = totalEligibleQuantity,
                HasExplicitRewardScope = effectiveHasExplicitRewardScope
            };
        }

        private IList<ShoppingCartItem> GetLowestPricedItems(IList<ShoppingCartItem> items)
        {
            if (items == null || !items.Any())
                return new ShoppingCartItem[0];

            var pricedItems = new List<PricedCartLine>();
            foreach (var item in items.Where(x => x != null && x.Quantity > 0))
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                pricedItems.Add(new PricedCartLine { Item = item, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            if (!pricedItems.Any())
                return new ShoppingCartItem[0];

            var minPrice = pricedItems.Min(x => x.UnitPrice);
            return pricedItems
                .Where(x => x.UnitPrice == minPrice)
                .Select(x => x.Item)
                .ToList();
        }

        private static bool HasExplicitRewardScope(IList<PromotionRuleProduct> ruleProducts, PromotionRuleTier tier)
        {
            return (ruleProducts?.Any(x => x.IsRewardProduct) ?? false) ||
                   (tier?.RewardProductId.HasValue == true && tier.RewardProductId.Value > 0);
        }

        private Dictionary<int, int> BuildAvailableRewardQuantitiesByLineId(
            IList<ShoppingCartItem> rewardItems,
            IList<ShoppingCartItem> matchedBuyItems,
            int rewardQuantity,
            int qualifyingBuyQuantity,
            bool useComboBuyMetric,
            bool hasExplicitRewardScope)
        {
            int availableQuantity;
            var available = rewardItems?
                .Where(x => x != null && x.Quantity > 0)
                .ToDictionary(x => x.Id, x => x.Quantity) ?? new Dictionary<int, int>();

            if (!available.Any() || matchedBuyItems == null || !matchedBuyItems.Any())
                return available;

            var overlappingBuyItems = matchedBuyItems
                .Where(x => x != null && x.Quantity > 0 && available.ContainsKey(x.Id))
                .ToList();
            if (!overlappingBuyItems.Any())
                return available;

            var reservedBuyQuantity = hasExplicitRewardScope
                ? 0
                : Math.Max(0, qualifyingBuyQuantity - rewardQuantity);
            if (useComboBuyMetric && reservedBuyQuantity <= 0)
                reservedBuyQuantity = 1;
            if (reservedBuyQuantity <= 0)
                return available;

            var buyItemsByPrice = new List<PricedCartLine>();
            foreach (var buyItem in overlappingBuyItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(buyItem, false);
                buyItemsByPrice.Add(new PricedCartLine { Item = buyItem, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            var remainingReservation = reservedBuyQuantity;

            foreach (var buyItem in buyItemsByPrice
                         .OrderByDescending(x => x.UnitPrice)
                         .ThenByDescending(x => x.Item.Quantity)
                         .Select(x => x.Item))
            {
                if (remainingReservation <= 0)
                    break;

                if (!available.TryGetValue(buyItem.Id, out availableQuantity) || availableQuantity <= 0)
                    continue;

                var reservedOnLine = Math.Min(availableQuantity, remainingReservation);
                available[buyItem.Id] = Math.Max(0, availableQuantity - reservedOnLine);
                remainingReservation -= reservedOnLine;
            }

            return available;
        }

        private static int CalculateDistinctEligibleQuantity(
            IList<ShoppingCartItem> matchedBuyItems,
            IList<ShoppingCartItem> rewardItems,
            bool useComboBuyMetric,
            int qualifyingBuyQuantity)
        {
            if (useComboBuyMetric)
            {
                var rewardQuantity = rewardItems?
                    .Where(x => x != null && x.Quantity > 0)
                    .Sum(x => x.Quantity) ?? 0;
                return qualifyingBuyQuantity + rewardQuantity;
            }

            var distinctItemIds = new HashSet<int>();
            var totalEligibleQuantity = 0;

            foreach (var item in matchedBuyItems.Where(x => x != null && x.Quantity > 0))
            {
                if (distinctItemIds.Add(item.Id))
                    totalEligibleQuantity += item.Quantity;
            }

            foreach (var item in rewardItems.Where(x => x != null && x.Quantity > 0))
            {
                if (distinctItemIds.Add(item.Id))
                    totalEligibleQuantity += item.Quantity;
            }

            return totalEligibleQuantity;
        }

        private static int? ResolveRewardProductIdFromLineDiscounts(
            IList<ShoppingCartItem> cart,
            IDictionary<int, decimal> rewardLineDiscounts)
        {
            if (cart == null || !cart.Any() || rewardLineDiscounts == null || !rewardLineDiscounts.Any())
                return null;

            var rewardedLineId = rewardLineDiscounts
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .FirstOrDefault();
            if (rewardedLineId <= 0)
                return null;

            return cart.FirstOrDefault(x => x.Id == rewardedLineId)?.ProductId;
        }

        private static int ResolveEvaluationStoreId(IList<ShoppingCartItem> cart, PromotionEvaluationContext context)
        {
            if (context?.StoreId > 0)
                return context.StoreId;

            return cart?.FirstOrDefault()?.StoreId ?? 0;
        }

        private AppliedPromotion EvaluateProgressiveTieredBuyXGetY(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleTier> tiers,
            IList<PromotionRuleProduct> buyProducts)
        {
            decimal existing;
            IList<ShoppingCartItem> eligibleItems;
            if (buyProducts.All(x => x.IsAllProducts))
            {
                eligibleItems = cart.Where(x => x.Quantity > 0).ToList();
            }
            else
            {
                eligibleItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);
            }

            if (!eligibleItems.Any())
                return null;

            var sortedTiers = tiers.OrderBy(x => x.MinQuantity).ToList();
            var expandedUnits = new List<PricedCartLine>();
            foreach (var item in eligibleItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                var normalizedUnitPrice = unitPrice < 0 ? 0 : unitPrice;
                for (var unitIndex = 0; unitIndex < item.Quantity; unitIndex++)
                    expandedUnits.Add(new PricedCartLine { Item = item, UnitPrice = normalizedUnitPrice });
            }

            var orderedUnits = expandedUnits
                .OrderByDescending(x => x.UnitPrice)
                .ThenBy(x => x.Item.Id)
                .ToList();

            if (orderedUnits.Count < 2)
                return null;

            var highestBuyQuantity = sortedTiers.Max(t => t.MinQuantity);
            var cycleLength = highestBuyQuantity + 1;
            var combinedLineDiscounts = new Dictionary<int, decimal>();
            var totalDiscount = 0m;
            var index = 0;
            var rewardCursor = orderedUnits.Count - 1;

            while (index < orderedUnits.Count)
            {
                var remaining = orderedUnits.Count - index;
                var groupSize = Math.Min(cycleLength, remaining);

                if (groupSize < 2)
                {
                    index++;
                    continue;
                }

                var buyCount = groupSize - 1;
                var totalParticipatingQuantity = groupSize;
                if (rewardCursor < 0)
                    break;

                var rewardLine = orderedUnits[rewardCursor];
                var rewardItem = rewardLine.Item;
                var rewardUnitPrice = rewardLine.UnitPrice;
                rewardCursor--;

                var matchingTier = sortedTiers
                    .Where(t =>
                        (buyCount >= t.MinQuantity || totalParticipatingQuantity >= t.MinQuantity) &&
                        (t.MaxQuantity == 0 || totalParticipatingQuantity <= t.MaxQuantity))
                    .OrderByDescending(t => t.MinQuantity)
                    .FirstOrDefault();

                if (matchingTier != null)
                {
                    decimal lineDiscount;
                    if (matchingTier.DiscountType == DiscountType.FreeItem || matchingTier.DiscountValue >= 100m)
                        lineDiscount = rewardUnitPrice;
                    else if (matchingTier.DiscountType == DiscountType.Percentage && matchingTier.DiscountValue > 0)
                        lineDiscount = rewardUnitPrice * (matchingTier.DiscountValue / 100m);
                    else if (matchingTier.DiscountType == DiscountType.FixedAmount && matchingTier.DiscountValue > 0)
                        lineDiscount = Math.Min(matchingTier.DiscountValue, rewardUnitPrice);
                    else
                        lineDiscount = 0m;

                    if (lineDiscount > 0)
                    {
                        if (combinedLineDiscounts.TryGetValue(rewardItem.Id, out existing))
                            combinedLineDiscounts[rewardItem.Id] = existing + lineDiscount;
                        else
                            combinedLineDiscounts[rewardItem.Id] = lineDiscount;

                        totalDiscount += lineDiscount;
                    }
                }

                index += groupSize;
            }

            if (!combinedLineDiscounts.Any() || totalDiscount <= 0)
                return null;

            var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, null, totalDiscount);
            appliedPromotion.LineDiscounts = combinedLineDiscounts;

            var discountedLineIds = combinedLineDiscounts.Keys.ToList();
            PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, eligibleItems.Where(x => discountedLineIds.Contains(x.Id)).ToList());
            return _promotionDiscountAllocator.ApplyLinkedMaximumDiscountedQuantity(rule, appliedPromotion, cart);
        }

        private AppliedPromotion BuildStackedCumulativeBuyXGetYAppliedPromotion(
            PromotionRule rule,
            IList<ShoppingCartItem> cart,
            IList<PromotionRuleProduct> ruleProducts,
            IList<PromotionRuleProduct> buyProducts,
            IList<PromotionRuleTier> tiers)
        {
            int availableQuantity;
            decimal existingLineDiscount;
            int existingDiscountedQuantity;
            if (rule == null || cart == null || !cart.Any() || buyProducts == null || !buyProducts.Any() || tiers == null || tiers.Count < 2)
                return null;

            if (tiers.Any(x => x.MaxQuantity > 0 || x.MinQuantity <= 0))
                return null;

            if (tiers.Any(x => x.DiscountType != DiscountType.FreeItem && x.DiscountType != DiscountType.Percentage))
                return null;

            var matchedBuyItems = _promotionConditionEvaluator.GetMatchedCartItemsByRuleProducts(cart, buyProducts);
            matchedBuyItems = matchedBuyItems.Where(x => x.Quantity > 0).ToList();
            if (!matchedBuyItems.Any())
                return null;

            var matchedBuyProductIds = matchedBuyItems
                .Select(x => x.ProductId)
                .ToList();

            var explicitRuleRewardProductIds = ruleProducts
                .Where(x => x.IsRewardProduct && x.ProductId > 0)
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();
            if (explicitRuleRewardProductIds.Any(x => !matchedBuyProductIds.Contains(x)))
                return null;

            var explicitTierRewardProductIds = tiers
                .Where(x => x.RewardProductId.HasValue && x.RewardProductId.Value > 0)
                .Select(x => x.RewardProductId.Value)
                .Distinct()
                .ToList();
            if (explicitTierRewardProductIds.Any(x => !matchedBuyProductIds.Contains(x)))
                return null;

            var totalQuantity = matchedBuyItems.Sum(x => x.Quantity);
            if (totalQuantity <= 1)
                return null;

            var orderedTiers = tiers
                .Select(x => new
                {
                    Tier = x,
                    RewardQuantity = x.RewardQuantity > 0 ? x.RewardQuantity : 1,
                    GroupSize = x.MinQuantity + (x.RewardQuantity > 0 ? x.RewardQuantity : 1)
                })
                .Where(x => x.GroupSize > 1)
                .OrderByDescending(x => x.GroupSize)
                .ThenByDescending(x => x.Tier.DiscountType == DiscountType.FreeItem ? 1m : x.Tier.DiscountValue / 100m)
                .ToList();
            if (!orderedTiers.Any())
                return null;

            var remainingQuantity = totalQuantity;
            var allocations = new List<StackedTierAllocation>();

            foreach (var tierInfo in orderedTiers)
            {
                var cycleCount = remainingQuantity / tierInfo.GroupSize;
                if (cycleCount <= 0)
                    continue;

                var discountedUnits = cycleCount * tierInfo.RewardQuantity;
                if (discountedUnits <= 0)
                    continue;

                allocations.Add(new StackedTierAllocation
                {
                    Tier = tierInfo.Tier,
                    DiscountedUnits = discountedUnits
                });

                remainingQuantity -= cycleCount * tierInfo.GroupSize;
                if (remainingQuantity <= 1)
                    break;
            }

            if (!allocations.Any())
                return null;

            var lineItemsByPrice = new List<PricedCartLine>();
            foreach (var item in matchedBuyItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                lineItemsByPrice.Add(new PricedCartLine { Item = item, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            var availableByLineId = matchedBuyItems.ToDictionary(x => x.Id, x => Math.Max(0, x.Quantity));
            var lineDiscounts = new Dictionary<int, decimal>();
            var discountedQuantitiesByLineId = new Dictionary<int, int>();

            foreach (var allocation in allocations)
            {
                var remainingDiscountedUnits = allocation.DiscountedUnits;
                if (remainingDiscountedUnits <= 0)
                    continue;

                foreach (var pricedLine in lineItemsByPrice.OrderBy(x => x.UnitPrice).ThenBy(x => x.Item.Id))
                {
                    var item = pricedLine.Item;
                    var unitPrice = pricedLine.UnitPrice;
                    if (remainingDiscountedUnits <= 0)
                        break;

                    if (!availableByLineId.TryGetValue(item.Id, out availableQuantity) || availableQuantity <= 0)
                        continue;

                    var takeQuantity = Math.Min(availableQuantity, remainingDiscountedUnits);
                    if (takeQuantity <= 0)
                        continue;

                    decimal perUnitDiscount;
                    if (allocation.Tier.DiscountType == DiscountType.FreeItem)
                        perUnitDiscount = unitPrice;
                    else
                        perUnitDiscount = unitPrice * (allocation.Tier.DiscountValue / 100m);

                    var lineDiscount = perUnitDiscount * takeQuantity;
                    if (lineDiscount > 0)
                    {
                        if (lineDiscounts.TryGetValue(item.Id, out existingLineDiscount))
                            lineDiscounts[item.Id] = existingLineDiscount + lineDiscount;
                        else
                            lineDiscounts[item.Id] = lineDiscount;

                        if (discountedQuantitiesByLineId.TryGetValue(item.Id, out existingDiscountedQuantity))
                            discountedQuantitiesByLineId[item.Id] = existingDiscountedQuantity + takeQuantity;
                        else
                            discountedQuantitiesByLineId[item.Id] = takeQuantity;
                    }

                    availableByLineId[item.Id] = availableQuantity - takeQuantity;
                    remainingDiscountedUnits -= takeQuantity;
                }
            }

            if (!lineDiscounts.Any())
                return null;

            var totalDiscountAmount = lineDiscounts.Sum(x => x.Value);
            if (totalDiscountAmount <= 0)
                return null;

            var appliedPromotion = _promotionDiscountAllocator.BuildAppliedPromotion(rule, cart, null, totalDiscountAmount);
            appliedPromotion.LineDiscounts = lineDiscounts;
            appliedPromotion.RewardQuantity = allocations.Sum(x => x.DiscountedUnits);
            appliedPromotion.RewardProductId = ResolveRewardProductIdFromLineDiscounts(cart, lineDiscounts);
            appliedPromotion.DiscountedQuantitiesByLineId = discountedQuantitiesByLineId;

            var discountedLineIds = lineDiscounts.Keys.ToList();
            PromotionRuntimeHelper.SetEligibleCartItems(appliedPromotion, matchedBuyItems.Where(x => discountedLineIds.Contains(x.Id)).ToList());
            return appliedPromotion;
        }

        private RewardDiscountQuantizedResult CalculateScopedRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId,
            DiscountType discountType,
            decimal discountValue)
        {
            var baseRewardDiscountResult = CalculateScopedRewardDiscounts(
                rewardItems,
                rewardQuantity,
                availableQuantitiesByLineId);
            var baseDiscountAmount = baseRewardDiscountResult.DiscountAmount;
            var baseLineDiscounts = baseRewardDiscountResult.LineDiscounts;

            if (baseDiscountAmount <= 0 || !baseLineDiscounts.Any())
                return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

            if (discountType == DiscountType.FreeItem)
                return new RewardDiscountQuantizedResult(baseDiscountAmount, baseLineDiscounts, baseRewardDiscountResult.DiscountedQuantitiesByLineId);

            if (discountType == DiscountType.Percentage)
            {
                if (discountValue <= 0)
                    return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

                var scale = discountValue / 100m;
                if (scale <= 0)
                    return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

                var lineDiscounts = baseLineDiscounts.ToDictionary(x => x.Key, x => x.Value * scale);
                return new RewardDiscountQuantizedResult(lineDiscounts.Sum(x => x.Value), lineDiscounts, baseRewardDiscountResult.DiscountedQuantitiesByLineId);
            }

            if (discountType == DiscountType.FixedAmount)
            {
                if (discountValue <= 0)
                    return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());

                var applied = Math.Min(discountValue, baseDiscountAmount);
                var lineDiscounts = new Dictionary<int, decimal>();
                var remaining = applied;

                foreach (var line in baseLineDiscounts.OrderByDescending(x => x.Value))
                {
                    if (remaining <= 0)
                        break;

                    var value = Math.Min(line.Value, remaining);
                    if (value <= 0)
                        continue;

                    lineDiscounts[line.Key] = value;
                    remaining -= value;
                }

                return new RewardDiscountQuantizedResult(lineDiscounts.Sum(x => x.Value), lineDiscounts, baseRewardDiscountResult.DiscountedQuantitiesByLineId);
            }

            return new RewardDiscountQuantizedResult(0m, new Dictionary<int, decimal>(), new Dictionary<int, int>());
        }

        private RewardDiscountQuantizedResult CalculateScopedRewardDiscounts(
            IList<ShoppingCartItem> rewardItems,
            int rewardQuantity,
            IDictionary<int, int> availableQuantitiesByLineId)
        {
            int overriddenQuantity;
            decimal existingDiscount;
            int existingQuantity;
            var discounts = new Dictionary<int, decimal>();
            var discountedQuantities = new Dictionary<int, int>();
            if (rewardQuantity <= 0 || rewardItems == null || rewardItems.Count == 0)
                return new RewardDiscountQuantizedResult(0m, discounts, discountedQuantities);

            var normalizedQuantities = NormalizeAvailableRewardQuantities(rewardItems, availableQuantitiesByLineId);
            var itemsWithPrice = new List<PricedCartLine>();

            foreach (var item in rewardItems)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                itemsWithPrice.Add(new PricedCartLine { Item = item, UnitPrice = unitPrice < 0 ? 0 : unitPrice });
            }

            var orderedItems = itemsWithPrice
                .OrderByDescending(x => x.UnitPrice)
                .ThenByDescending(x => x.Item.Quantity)
                .ThenBy(x => x.Item.Id)
                .ToList();

            var remaining = rewardQuantity;
            for (var index = orderedItems.Count - 1; index >= 0 && remaining > 0; index--)
            {
                var item = orderedItems[index];

                var availableQuantity = normalizedQuantities.TryGetValue(item.Item.Id, out overriddenQuantity)
                    ? overriddenQuantity
                    : item.Item.Quantity;
                if (availableQuantity <= 0)
                    continue;

                var takeQuantity = Math.Min(availableQuantity, remaining);
                if (takeQuantity <= 0)
                    continue;

                var discount = item.UnitPrice * takeQuantity;
                if (discount > 0)
                {
                    if (discounts.TryGetValue(item.Item.Id, out existingDiscount))
                        discounts[item.Item.Id] = existingDiscount + discount;
                    else
                        discounts[item.Item.Id] = discount;
                }

                if (discountedQuantities.TryGetValue(item.Item.Id, out existingQuantity))
                    discountedQuantities[item.Item.Id] = existingQuantity + takeQuantity;
                else
                    discountedQuantities[item.Item.Id] = takeQuantity;

                remaining -= takeQuantity;
            }

            return new RewardDiscountQuantizedResult(discounts.Sum(x => x.Value), discounts, discountedQuantities);
        }

        private static Dictionary<int, int> NormalizeAvailableRewardQuantities(
            IList<ShoppingCartItem> rewardItems,
            IDictionary<int, int> availableQuantitiesByLineId)
        {
            int availableQuantity;
            var normalized = rewardItems
                .Where(x => x != null)
                .ToDictionary(x => x.Id, x => Math.Max(0, x.Quantity));

            if (availableQuantitiesByLineId == null || !availableQuantitiesByLineId.Any())
                return normalized;

            foreach (var item in rewardItems.Where(x => x != null))
            {
                if (!availableQuantitiesByLineId.TryGetValue(item.Id, out availableQuantity))
                    continue;

                normalized[item.Id] = Math.Max(0, Math.Min(item.Quantity, availableQuantity));
            }

            return normalized;
        }

        /// <summary>
        /// Calculate the benefit value for a tier (used for auto-upgrade comparison)
        /// </summary>
        public decimal CalculateTierBenefitValue(PromotionRuleTier tier, IList<ShoppingCartItem> matchedItems, IList<ShoppingCartItem> cart, PromotionRule rule)
        {
            if (tier == null || matchedItems == null || !matchedItems.Any())
                return 0m;

            try
            {
                var quantity = matchedItems.Sum(x => x.Quantity);

                // Calculate benefit based on discount type
                switch (tier.DiscountType)
                {
                    case DiscountType.Percentage:
                        // Percentage benefit: (total price of matched items) * (percentage/100)
                        // Note: Price not directly available, use discount value as approximation
                        return (decimal)quantity * tier.DiscountValue;

                    case DiscountType.FixedAmount:
                        // Fixed amount benefit: amount * quantity of matched items
                        return tier.DiscountValue * quantity;

                    case DiscountType.FreeItem:
                        // Free item benefit: reward quantity * estimated item value
                        var rewardQty = tier.RewardQuantity > 0 ? tier.RewardQuantity : 1;
                        return rewardQty * 100; // Approximate value for benefit calculation

                    default:
                        return 0m;
                }
            }
            catch
            {
                return 0m;
            }
        }

        /// <summary>
        /// Determine if first promotion's benefit is better than second (for auto-upgrade)
        /// </summary>
        public bool IsBetterBenefit(AppliedPromotion first, AppliedPromotion second)
        {
            if (first == null && second == null)
                return false;
            if (first == null)
                return false;
            if (second == null)
                return true;

            // Compare benefit values
            var firstBenefit = first.BenefitValue > 0 ? first.BenefitValue : first.DiscountAmount;
            var secondBenefit = second.BenefitValue > 0 ? second.BenefitValue : second.DiscountAmount;

            return firstBenefit > secondBenefit;
        }

        private sealed class BuyXGetYRewardContext
        {
            public IList<ShoppingCartItem> RewardItems { get; set; } = new List<ShoppingCartItem>();
            public IDictionary<int, int> RewardAvailableQuantitiesByLineId { get; set; } = new Dictionary<int, int>();
            public int RewardQuantity { get; set; }
            public int TotalEligibleQuantity { get; set; }
            public bool HasExplicitRewardScope { get; set; }
        }

        private sealed class StackedTierAllocation
        {
            public PromotionRuleTier Tier { get; set; }
            public int DiscountedUnits { get; set; }
        }
    }
}
