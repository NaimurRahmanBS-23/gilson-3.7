// Ported from nopCommerce 4.9 Discount Manager Plus services. C# 6 / nopCommerce 3.70.
using System.Linq;
using System.Collections.Generic;
using System;
﻿using System.Web;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{

    /// <summary>
    /// Default implementation of promotion attention message service for generating
    /// contextual discount explanations and maximization tips.
    /// </summary>
    public class PromotionAttentionMessageService : IPromotionAttentionMessageService
    {
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;

        public PromotionAttentionMessageService(
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            ILocalizationService localizationService,
            ILogger logger,
            IWorkContext workContext)
        {
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _localizationService = localizationService;
            _logger = logger;
            _workContext = workContext;
        }

        /// <summary>
        /// Generates attention messages for dual-offer scenarios
        /// </summary>
        public IList<PromotionAttentionMessage> GenerateDualOfferMessages(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart,
            IDictionary<int, List<DiscountAllocation>> coordinatedAllocations)
        {
            var messages = new List<PromotionAttentionMessage>();

            if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
                return messages;

            // Detect if this is a dual-offer scenario
            var buyXGetYPromotions = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                .ToList();

            if (buyXGetYPromotions.Count < 2)
                return messages; // Not a dual-offer scenario

            // Detect scenario type for contextual messaging
            var scenarioType = DetectScenarioType(cart, coordinatedAllocations);

            // Generate messages for each promotion
            foreach (var promotion in buyXGetYPromotions.OrderByDescending(x => x.DiscountAmount))
            {
                var message = GeneratePromotionMessage(promotion, cart, coordinatedAllocations, scenarioType);
                if (message != null)
                {
                    message.IsDualOfferScenario = true;
                    message.ScenarioType = scenarioType;
                    message.ScenarioComplexity = cart.Select(x => x.ProductId).Distinct().Count();
                    messages.Add(message);
                }
            }

            // Generate cross-promotion coordination message
            var coordinationMessage = GenerateCoordinationMessage(
                buyXGetYPromotions, cart, coordinatedAllocations, scenarioType);
            if (coordinationMessage != null)
            {
                coordinationMessage.IsDualOfferScenario = true;
                coordinationMessage.ScenarioType = scenarioType;
                coordinationMessage.IsHighlighted = true;
                coordinationMessage.DisplayPriority = 100;
                messages.Insert(0, coordinationMessage); // Add at the beginning
            }

            return messages;
        }

        /// <summary>
        /// Generates a comprehensive dual-offer explanation
        /// </summary>
        public DualOfferExplanation GenerateDualOfferExplanation(
            IList<AppliedPromotion> appliedPromotions,
            IDictionary<int, List<DiscountAllocation>> coordinatedAllocations,
            IList<ShoppingCartItem> cart)
        {
            var explanation = new DualOfferExplanation
            {
                IsDualOfferScenario = coordinatedAllocations != null && coordinatedAllocations.Count >= 2,
                TotalProducts = cart.Select(x => x.ProductId).Distinct().Count(),
                TotalUnits = cart.Sum(x => x.Quantity)
            };

            if (!explanation.IsDualOfferScenario)
                return explanation;

            // Detect scenario type
            explanation.ScenarioType = DetectScenarioType(cart, coordinatedAllocations);

            // Build discount choice details
            foreach (var kvp in coordinatedAllocations.OrderByDescending(x => x.Value.Sum(a => a.DiscountAmount)))
            {
                var promotionRuleId = kvp.Key;
                var allocations = kvp.Value;

                var promotion = appliedPromotions.FirstOrDefault(p => p.PromotionRuleId == promotionRuleId);
                if (promotion == null) continue;

                foreach (var allocation in allocations)
                {
                    var cartItem = cart.FirstOrDefault(c => c.Id == allocation.LineId);
                    if (cartItem == null) continue;

                    var product = _productService.GetProductById(cartItem.ProductId);
                    var choiceDetail = new DiscountChoiceDetail
                    {
                        PromotionName = promotion.RuleName,
                        DiscountType = GetDiscountTypeText(promotion, allocation),
                        ProductName = product?.Name ?? $"Product {cartItem.ProductId}",
                        OriginalPrice = allocation.UnitPrice,
                        DiscountAmount = allocation.DiscountAmount,
                        FinalPrice = allocation.UnitPrice - allocation.DiscountAmount,
                        Reasoning = GenerateChoiceReasoning(promotion, allocation, cartItem, cart),
                        Priority = (int)CalculateEffectiveDiscountValue(promotion),
                        IsFreeItem = allocation.IsFreeItem,
                        RewardQuantity = allocation.Quantity
                    };

                    explanation.DiscountChoices.Add(choiceDetail);
                    explanation.TotalSavings += allocation.DiscountAmount;
                }

                explanation.AppliedPromotionNames.Add(promotion.RuleName);
            }

            // Generate explanation text based on scenario
            explanation.ExplanationText = GenerateContextualExplanationText(explanation, cart);

            // Calculate savings percentage
            var cartTotal = CalculateCartTotal(cart);
            if (cartTotal > 0)
            {
                explanation.SavingsPercentage = (explanation.TotalSavings / cartTotal) * 100;
            }

            return explanation;
        }

        /// <summary>
        /// Generates maximization tips for additional savings
        /// </summary>
        public IList<SavingsMaximizationTip> GenerateMaximizationTips(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart,
            IList<int> eligibleProductIds)
        {
            var tips = new List<SavingsMaximizationTip>();

            if (appliedPromotions == null || !appliedPromotions.Any() || cart == null || !cart.Any())
                return tips;

            // Generate tips for each applied promotion
            foreach (var promotion in appliedPromotions)
            {
                var promotionTips = GeneratePromotionMaximizationTips(promotion, cart, eligibleProductIds);
                tips.AddRange(promotionTips);
            }

            // Generate cross-promotion tips
            var crossPromotionTips = GenerateCrossPromotionTips(appliedPromotions, cart);
            tips.AddRange(crossPromotionTips);

            // Sort by priority and potential savings
            return tips.OrderByDescending(t => t.Priority).ThenByDescending(t => t.PotentialSavings).ToList();
        }

        /// <summary>
        /// Detects the scenario type based on cart composition
        /// </summary>
        public ScenarioType DetectScenarioType(
            IList<ShoppingCartItem> cart,
            IDictionary<int, List<DiscountAllocation>> allocations)
        {
            if (cart == null || !cart.Any())
                return ScenarioType.MultipleProductsAllSingleUnits;

            var distinctProducts = cart.Select(x => x.ProductId).Distinct().Count();
            var totalUnits = cart.Sum(x => x.Quantity);

            if (distinctProducts == 1)
            {
                return totalUnits == 1
                    ? ScenarioType.SingleProductSingleUnit
                    : ScenarioType.SingleProductMultipleUnits;
            }

            if (distinctProducts > 1)
            {
                var hasMultipleUnits = cart.Any(x => x.Quantity > 1);
                return hasMultipleUnits
                    ? ScenarioType.MultipleProductsMixedUnits
                    : ScenarioType.MultipleProductsAllSingleUnits;
            }

            return ScenarioType.MultipleProductsAllSingleUnits;
        }

        #region Private Helper Methods

        private PromotionAttentionMessage GeneratePromotionMessage(
            AppliedPromotion promotion,
            IList<ShoppingCartItem> cart,
            IDictionary<int, List<DiscountAllocation>> coordinatedAllocations,
            ScenarioType scenarioType)
        {
            List<DiscountAllocation> allocations;
            if (!coordinatedAllocations.TryGetValue(promotion.PromotionRuleId, out allocations) ||
                !allocations.Any())
            {
                return null;
            }

            var message = new PromotionAttentionMessage
            {
                RuleId = promotion.PromotionRuleId,
                RuleName = promotion.RuleName,
                MessageType = AttentionMessageType.Info,
                Message = GeneratePromotionMessageText(promotion, allocations, scenarioType, cart)
            };

            // Add reasoning for chosen items
            foreach (var allocation in allocations)
            {
                var cartItem = cart.FirstOrDefault(c => c.Id == allocation.LineId);
                if (cartItem != null)
                {
                    var product = _productService.GetProductById(cartItem.ProductId);
                    var reasoning = GenerateChoiceReasoning(promotion, allocation, cartItem, cart);
                    message.ChosenItemReasons.Add(reasoning);
                    message.ProductNames.Add(product?.Name ?? $"Product {cartItem.ProductId}");
                }
            }

            return message;
        }

        private string GeneratePromotionMessageText(
            AppliedPromotion promotion,
            List<DiscountAllocation> allocations,
            ScenarioType scenarioType,
            IList<ShoppingCartItem> cart)
        {
            var discountType = GetDiscountTypeText(promotion, allocations.First());
            var firstAllocation = allocations.First();

            // Get the cart item from the cart list
            var cartItem = cart.FirstOrDefault(c => c.Id == firstAllocation.LineId);
            var product = cartItem != null ? _productService.GetProductById(cartItem.ProductId) : null;

            var productName = product != null ? product.Name : null;
            switch (scenarioType)
            {
                case ScenarioType.SingleProductMultipleUnits:
                    return string.Format("Your {0} has multiple units, so we applied {1} to maximize your savings.", productName, discountType);
                case ScenarioType.MultipleProductsMixedUnits:
                    return string.Format("We analyzed all products in your cart and applied {0} to {1} as the best value.", discountType, productName);
                case ScenarioType.MultipleProductsAllSingleUnits:
                    return string.Format("With multiple different products, we applied {0} to {1} for optimal savings.", discountType, productName);
                default:
                    return string.Format("We applied {0} to {1} to maximize your discount.", discountType, productName ?? "an eligible item");
            }
        }

        private string GenerateChoiceReasoning(
            AppliedPromotion promotion,
            DiscountAllocation allocation,
            ShoppingCartItem cartItem,
            IList<ShoppingCartItem> cart)
        {
            var product = _productService.GetProductById(cartItem.ProductId);
            var productName = product?.Name ?? $"Product {cartItem.ProductId}";

            var discountType = promotion.DiscountTypeId == (int)DiscountType.FreeItem
                ? "100% discount (free item)"
                : $"{promotion.DiscountValue}% discount";

            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                return $"{productName} was chosen for the 100% discount because it's one of the cheapest eligible items in your cart at ${allocation.UnitPrice:F2}.";
            }
            else
            {
                return $"{productName} received {discountType} because it was the next cheapest eligible item after the free item was allocated.";
            }
        }

        private PromotionAttentionMessage GenerateCoordinationMessage(
            IList<AppliedPromotion> buyXGetYPromotions,
            IList<ShoppingCartItem> cart,
            IDictionary<int, List<DiscountAllocation>> coordinatedAllocations,
            ScenarioType scenarioType)
        {
            var totalSavings = coordinatedAllocations.Values.SelectMany(x => x).Sum(x => x.DiscountAmount);
            var scenarioText = GetScenarioDescriptionText(scenarioType);

            var message = new PromotionAttentionMessage
            {
                RuleId = 0, // Coordination message, not tied to specific rule
                RuleName = "Dual-Offer Coordination",
                MessageType = AttentionMessageType.Info,
                Message = $"Dual-Offer Discounts Applied: {scenarioText} You saved ${totalSavings:F2} through automatic discount coordination.",
                DiscountChoiceExplanation = GenerateCoordinationExplanation(scenarioType),
                DisplayPriority = 100,
                IsHighlighted = true,
                MessageCategory = "Coordination"
            };

            return message;
        }

        private string GenerateCoordinationExplanation(ScenarioType scenarioType)
        {
            switch (scenarioType)
            {
                case ScenarioType.SingleProductMultipleUnits:
                    return "Since you have multiple units of the same product, we applied different promotions to different units to maximize your savings.";
                case ScenarioType.MultipleProductsMixedUnits:
                    return "We analyzed all products in your cart and automatically applied your available promotions to the cheapest eligible items.";
                case ScenarioType.MultipleProductsAllSingleUnits:
                    return "With multiple different products, we applied your promotions to the cheapest items to give you the best value.";
                default:
                    return "We automatically coordinated your available promotions to maximize your total savings.";
            }
        }

        private string GetScenarioDescriptionText(ScenarioType scenarioType)
        {
            switch (scenarioType)
            {
                case ScenarioType.SingleProductMultipleUnits:
                    return "Multiple units of the same product received different promotions.";
                case ScenarioType.MultipleProductsMixedUnits:
                    return "Different promotions were applied to different products based on price.";
                case ScenarioType.MultipleProductsAllSingleUnits:
                    return "Promotions were distributed across your cart's cheapest items.";
                default:
                    return "Your available promotions were coordinated for maximum benefit.";
            }
        }

        private string GenerateContextualExplanationText(
            DualOfferExplanation explanation,
            IList<ShoppingCartItem> cart)
        {
            if (!explanation.DiscountChoices.Any())
            {
                return "We automatically applied your available discounts to maximize your savings.";
            }

            var firstChoice = explanation.DiscountChoices.First();
            var lastChoice = explanation.DiscountChoices.Last();

            switch (explanation.ScenarioType)
            {
                case ScenarioType.SingleProductMultipleUnits:
                    return string.Format("Your {0} has multiple units, so we applied different discounts to maximize savings. {1} was applied to one unit, and {2} to another.",
                        firstChoice.ProductName, firstChoice.DiscountType, lastChoice.DiscountType);
                case ScenarioType.MultipleProductsMixedUnits:
                    return string.Format("We analyzed all {0} products in your cart and applied discounts to the cheapest eligible items. {1} received {2}, while {3} received {4}.",
                        explanation.TotalProducts, firstChoice.ProductName, firstChoice.DiscountType, lastChoice.ProductName, lastChoice.DiscountType);
                case ScenarioType.MultipleProductsAllSingleUnits:
                    return string.Format("With {0} different products, we applied {1} to the cheapest item ({2}) and {3} to the next cheapest ({4}).",
                        explanation.TotalProducts, firstChoice.DiscountType, firstChoice.ProductName, lastChoice.DiscountType, lastChoice.ProductName);
                default:
                    return string.Format("We applied {0} to {1} and {2} to {3} to maximize your savings.",
                        firstChoice.DiscountType, firstChoice.ProductName, lastChoice.DiscountType, lastChoice.ProductName);
            }
        }

        private decimal CalculateCartTotal(IList<ShoppingCartItem> cart)
        {
            decimal total = 0;
            foreach (var item in cart)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(item, false);
                total += unitPrice * item.Quantity;
            }
            return total;
        }

        private string GetDiscountTypeText(AppliedPromotion promotion, DiscountAllocation allocation)
        {
            if (allocation.IsFreeItem)
                return "100% discount (free item)";

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                return $"{promotion.DiscountValue}% discount";

            if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                return $"${promotion.DiscountValue:F2} discount";

            return "discount";
        }

        private decimal CalculateEffectiveDiscountValue(AppliedPromotion promotion)
        {
            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                return 100m;

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                return promotion.DiscountValue > 0 ? promotion.DiscountValue : promotion.DiscountAmount;

            if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                return Math.Min(promotion.DiscountAmount, 50m);

            return promotion.DiscountAmount;
        }

        private IList<SavingsMaximizationTip> GeneratePromotionMaximizationTips(
            AppliedPromotion promotion,
            IList<ShoppingCartItem> cart,
            IList<int> eligibleProductIds)
        {
            var tips = new List<SavingsMaximizationTip>();

            // Generate quantity increase tips
            var quantityTips = GenerateQuantityTips(promotion, cart);
            tips.AddRange(quantityTips);

            // Generate product addition tips
            var additionTips = GenerateAdditionTips(promotion, cart, eligibleProductIds);
            tips.AddRange(additionTips);

            return tips;
        }

        private IList<SavingsMaximizationTip> GenerateQuantityTips(
            AppliedPromotion promotion,
            IList<ShoppingCartItem> cart)
        {
            var tips = new List<SavingsMaximizationTip>();

            // Check if increasing quantities could unlock better discounts
            foreach (var cartItem in cart)
            {
                var potentialSavings = CalculateQuantityIncreaseBenefit(promotion, cartItem);
                if (potentialSavings > 5) // Only if meaningful savings
                {
                    var product = _productService.GetProductById(cartItem.ProductId);
                    tips.Add(new SavingsMaximizationTip
                    {
                        TipId = $"qty_{promotion.PromotionRuleId}_{cartItem.ProductId}",
                        Message = $"Add 1 more {product?.Name} to unlock additional ${potentialSavings:F2} savings",
                        MaximizationType = MaximizationType.IncreaseQuantity,
                        PotentialSavings = potentialSavings,
                        TargetProductName = product?.Name,
                        TargetProductId = cartItem.ProductId,
                        CurrentQuantity = cartItem.Quantity,
                        RecommendedQuantity = cartItem.Quantity + 1,
                        RelatedPromotionName = promotion.RuleName,
                        Priority = (int)potentialSavings, // Higher savings = higher priority
                        IsQuickWin = true,
                        EstimatedTimeToAchieve = 2
                    });
                }
            }

            return tips;
        }

        private IList<SavingsMaximizationTip> GenerateAdditionTips(
            AppliedPromotion promotion,
            IList<ShoppingCartItem> cart,
            IList<int> eligibleProductIds)
        {
            var tips = new List<SavingsMaximizationTip>();

            // Suggest adding cheapest eligible product to maximize savings
            if (eligibleProductIds != null && eligibleProductIds.Any())
            {
                var currentProductIds = new HashSet<int>(cart.Select(x => x.ProductId));
                var missingProductIds = eligibleProductIds.Where(id => !currentProductIds.Contains(id)).ToList();

                foreach (var productId in missingProductIds.Take(3)) // Top 3 suggestions
                {
                    var product = _productService.GetProductById(productId);
                    if (product != null && !product.Deleted)
                    {
                        var potentialSavings = CalculateProductAdditionBenefit(promotion, product);
                        tips.Add(new SavingsMaximizationTip
                        {
                            TipId = $"add_{promotion.PromotionRuleId}_{productId}",
                            Message = $"Add {product.Name} to maximize your discount savings",
                            MaximizationType = MaximizationType.AddProduct,
                            PotentialSavings = potentialSavings,
                            SuggestedProducts = { product.Name },
                            SuggestedProductIds = { productId },
                            SuggestedQuantity = 1,
                            RelatedPromotionName = promotion.RuleName,
                            Priority = (int)potentialSavings,
                            IsQuickWin = true,
                            EstimatedTimeToAchieve = 5,
                            ActionUrl = $"/product/{productId}"
                        });
                    }
                }
            }

            return tips;
        }

        private IList<SavingsMaximizationTip> GenerateCrossPromotionTips(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart)
        {
            var tips = new List<SavingsMaximizationTip>();

            // Generate tips that consider multiple promotions together
            if (appliedPromotions.Count >= 2)
            {
                var totalPotentialSavings = CalculateCrossPromotionBenefit(appliedPromotions, cart);
                if (totalPotentialSavings > 10)
                {
                    tips.Add(new SavingsMaximizationTip
                    {
                        TipId = "cross_promotion_1",
                        Message = "Add 1 more eligible item to maximize your dual-offer savings",
                        MaximizationType = MaximizationType.GeneralOptimization,
                        PotentialSavings = totalPotentialSavings,
                        SuggestedQuantity = 1,
                        Priority = 100,
                        IsQuickWin = true,
                        EstimatedTimeToAchieve = 3,
                        AdditionalDetails = "Increasing your cart quantity could unlock better discount allocation across both promotions."
                    });
                }
            }

            return tips;
        }

        private decimal CalculateQuantityIncreaseBenefit(AppliedPromotion promotion, ShoppingCartItem cartItem)
        {
            // Simple calculation: if adding 1 more unit could trigger another reward
            if (promotion.RewardQuantity > 0 && cartItem.Quantity < promotion.RewardQuantity * 2)
            {
                var unitPrice = _priceCalculationService.GetUnitPrice(cartItem, false);

                if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                    return unitPrice; // Full price savings

                if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                    return unitPrice * (promotion.DiscountValue / 100m);

                if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                    return Math.Min(promotion.DiscountValue, unitPrice);
            }

            return 0;
        }

        private decimal CalculateProductAdditionBenefit(AppliedPromotion promotion, Product product)
        {
            // Estimate potential savings from adding this product
            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                return product.Price; // Assuming free item discount

            if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                return product.Price * (promotion.DiscountValue / 100m);

            if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
                return Math.Min(promotion.DiscountValue, product.Price);

            return product.Price * 0.5m; // Conservative estimate
        }

        private decimal CalculateCrossPromotionBenefit(
            IList<AppliedPromotion> appliedPromotions,
            IList<ShoppingCartItem> cart)
        {
            // Calculate potential additional savings from optimizing cross-promotion allocation
            decimal totalBenefit = 0;

            foreach (var promotion in appliedPromotions)
            {
                if (promotion.RewardQuantity > 0)
                {
                    // Estimate benefit of adding 1 more cheapest item
                    var cheapestItem = cart.OrderBy(x =>
                    {
                        return _priceCalculationService.GetUnitPrice(x, false);
                    }).FirstOrDefault();

                    if (cheapestItem != null)
                    {
                        totalBenefit += CalculateQuantityIncreaseBenefit(promotion, cheapestItem);
                    }
                }
            }

            return totalBenefit;
        }

        #endregion
    }
}
