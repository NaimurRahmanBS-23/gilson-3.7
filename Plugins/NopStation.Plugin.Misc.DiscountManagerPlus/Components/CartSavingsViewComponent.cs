using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Web.Factories;
using NopStation.Plugin.Misc.Core.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Components;

public class CartSavingsViewComponent : NopStationViewComponent
{
    private const string RenderedRequestItemKey = "NopStation.DiscountManagerPlus.CartSavings.Rendered";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISettingService _settingService;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IPromotionConditionEvaluator _promotionConditionEvaluator;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger _logger;
    private readonly IPromotionAttentionMessageService _promotionAttentionMessageService;

    public CartSavingsViewComponent(
        IHttpContextAccessor httpContextAccessor,
        ISettingService settingService,
        IWorkContext workContext,
        IStoreContext storeContext,
        IShoppingCartService shoppingCartService,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IProductModelFactory productModelFactory,
        IDiscountManagerPlusService discountManagerPlusService,
        IPromotionRuleService promotionRuleService,
        IPromotionConditionEvaluator promotionConditionEvaluator,
        ILocalizationService localizationService,
        ILogger logger,
        IPromotionAttentionMessageService promotionAttentionMessageService)
    {
        _httpContextAccessor = httpContextAccessor;
        _settingService = settingService;
        _workContext = workContext;
        _storeContext = storeContext;
        _shoppingCartService = shoppingCartService;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _productModelFactory = productModelFactory;
        _discountManagerPlusService = discountManagerPlusService;
        _promotionRuleService = promotionRuleService;
        _promotionConditionEvaluator = promotionConditionEvaluator;
        _localizationService = localizationService;
        _logger = logger;
        _promotionAttentionMessageService = promotionAttentionMessageService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (HasAlreadyRenderedForRequest())
            return Content(string.Empty);

        var store = await _storeContext.GetCurrentStoreAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled)
            return Content(string.Empty);

        var customer = await _workContext.GetCurrentCustomerAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return Content(string.Empty);

        var appliedPromotions = await _discountManagerPlusService.EvaluateCartAsync(cart, store.Id);
        var pendingRewards = appliedPromotions
            .Where(x => x.RequiresRewardSelection)
            .GroupBy(x => x.PromotionRuleId)
            .Select(g => new
            {
                PromotionRuleId = g.Key,
                RewardProductId = g
                    .Select(x => x.RewardProductId)
                    .FirstOrDefault(x => x.HasValue && x.Value > 0),
                RewardQuantity = g.Max(x => x.RewardQuantity)
            })
            .ToList();

        var reminderMessages = await BuildTierUpgradeRemindersAsync(cart, store.Id, appliedPromotions);

        var model = new CartSavingsModel();

        if (settings.EnableCartSavingsBreakdown)
        {
            var ruleDiscountMap = await _discountManagerPlusService.BuildRuleDiscountMapAsync(cart, store.Id);
            var groupedSavings = appliedPromotions
                .Select(x => new
                {
                    Promotion = x,
                    EffectiveAmount = ruleDiscountMap.TryGetValue(x.PromotionRuleId, out var amount) ? amount : 0m
                })
                .Where(x => x.EffectiveAmount > 0)
                .GroupBy(x => new { x.Promotion.PromotionRuleId, x.Promotion.RuleName })
                .Select(g => new
                {
                    g.Key.PromotionRuleId,
                    g.Key.RuleName,
                    DiscountAmount = g.Sum(x => x.EffectiveAmount),
                    RepresentativePromotion = g.Select(x => x.Promotion).FirstOrDefault()
                })
                .OrderByDescending(x => x.DiscountAmount)
                .ToList();

            if (groupedSavings.Any())
            {
                foreach (var groupedSaving in groupedSavings)
                {
                    var targetProductName = groupedSaving.RepresentativePromotion?.TargetProductName ?? string.Empty;
                    var appliedToText = !string.IsNullOrEmpty(targetProductName) ? $"(Applied to {targetProductName})" : string.Empty;

                    // NEW: Determine if this is part of a coordinated multi-discount scenario
                    var isCoordinatedDiscount = groupedSavings.Count >= 2 && groupedSaving.RepresentativePromotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY;
                    var discountPriority = isCoordinatedDiscount ? groupedSavings.Count - groupedSavings.ToList().IndexOf(groupedSaving) : 0;

                    model.Items.Add(new CartSavingsItemModel
                    {
                        PromotionRuleId = groupedSaving.PromotionRuleId,
                        RuleName = groupedSaving.RuleName,
                        BadgeText = await GetSavingsBadgeAsync(groupedSaving.RepresentativePromotion),
                        DetailText = await GetSavingsDetailAsync(groupedSaving.RepresentativePromotion),
                        IsBogoStyle = groupedSaving.RepresentativePromotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY,
                        DiscountAmount = groupedSaving.DiscountAmount,
                        DiscountAmountFormatted = await _priceFormatter.FormatPriceAsync(groupedSaving.DiscountAmount, true, false),
                        TargetProductName = targetProductName,
                        AppliedToText = appliedToText,
                        // NEW: Add discount coordination details
                        DiscountType = await GetDiscountTypeLabelAsync(groupedSaving.RepresentativePromotion),
                        TargetSelectionText = await GetCheapestItemSelectionTextAsync(groupedSaving.RepresentativePromotion),
                        IsCoordinatedDiscount = isCoordinatedDiscount,
                        DiscountPriority = discountPriority
                    });
                }

                // Set totals from the processed items
                model.Title = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Title");
                model.TotalLabel = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Total");
                model.TotalSavings = model.Items.Sum(x => x.DiscountAmount);
                model.TotalSavingsFormatted = await _priceFormatter.FormatPriceAsync(model.TotalSavings, true, false);
                var savingsSummaryTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings");
                model.SummaryText = string.Format(savingsSummaryTemplate, model.TotalSavingsFormatted);
            }
        }

        if (pendingRewards.Any())
        {
            model.PendingRewardsTitle = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Title");
            model.PendingRewardsDescription = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Description");
            model.PendingRewardsSelectText = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.SelectReward");
            model.PendingRewardsAddText = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.AddToCart");

            var index = 0;
            foreach (var pending in pendingRewards)
            {
                var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(pending.PromotionRuleId);
                if (rule == null)
                    continue;

                var rewardOptionIds = await GetPendingRewardOptionProductIdsAsync(rule, pending.RewardProductId, store.Id);
                if (!rewardOptionIds.Any())
                    continue;

                var rewardSelection = new CartRewardSelectionModel
                {
                    PromotionRuleId = pending.PromotionRuleId,
                    RuleName = appliedPromotions.FirstOrDefault(x => x.PromotionRuleId == pending.PromotionRuleId)?.RuleName ?? string.Empty,
                    RewardQuantity = pending.RewardQuantity > 0 ? pending.RewardQuantity : 1,
                    MaxSelectableQuantity = pending.RewardQuantity > 0 ? pending.RewardQuantity : 1,
                    FormId = $"promotion-reward-form-{pending.PromotionRuleId}-{index}",
                    PopupId = $"promotion-reward-popup-{pending.PromotionRuleId}-{index}",
                    AddToCartUrl = Url.Action("AddRewardToCart", "DiscountManagerPlusPublic") ?? string.Empty
                };

                foreach (var rewardOptionId in rewardOptionIds)
                {
                    var product = await _productService.GetProductByIdAsync(rewardOptionId);
                    if (product == null || product.Deleted || product.ProductType != ProductType.SimpleProduct)
                        continue;

                    var productModel = await _productModelFactory.PrepareProductDetailsModelAsync(product);
                    productModel.AddToCart.EnteredQuantity = pending.RewardQuantity > 0 ? pending.RewardQuantity : 1;

                    rewardSelection.Options.Add(new CartRewardOptionModel
                    {
                        RewardProductId = rewardOptionId,
                        RewardProductName = productModel.Name,
                        RewardProductOldPrice = productModel.ProductPrice?.OldPrice ?? string.Empty,
                        RewardProductPrice = productModel.ProductPrice?.Price ?? string.Empty,
                        ImageUrl = productModel.PictureModels?.FirstOrDefault()?.ImageUrl ?? string.Empty,
                        IsSelected = rewardSelection.Options.Count == 0,
                        Product = productModel
                    });
                }

                if (rewardSelection.Options.Any())
                {
                    model.PendingRewards.Add(rewardSelection);
                    index++;
                }
            }
        }

        if (reminderMessages.Any())
        {
            model.ReminderTitle = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Title");
            foreach (var reminder in reminderMessages)
                model.Reminders.Add(reminder);
        }

        // NEW: Build excluded product warnings
        var excludedProductWarnings = await BuildExcludedProductWarningsAsync(cart, store.Id, appliedPromotions);
        if (excludedProductWarnings.Any())
        {
            foreach (var warning in excludedProductWarnings)
                model.ExcludedProductWarnings.Add(warning);
        }

        // NEW: Build multiple discount notices
        var multipleDiscountNotices = await BuildMultipleDiscountNoticesAsync(appliedPromotions);
        if (multipleDiscountNotices.Any())
        {
            foreach (var notice in multipleDiscountNotices)
                model.MultipleDiscountNotices.Add(notice);
        }

        // NEW: Build cheapest item selection details
        var cheapestItemDetails = await BuildCheapestItemSelectionDetailsAsync(cart, appliedPromotions);
        if (cheapestItemDetails.Any())
        {
            foreach (var detail in cheapestItemDetails)
                model.CheapestItemSelectionDetails.Add(detail);
        }

        // NEW: Generate dual-offer attention messages and explanations
        await GenerateDualOfferMessagingAsync(cart, appliedPromotions, model);

        if (!model.Items.Any() && !model.PendingRewards.Any() && !model.Reminders.Any() &&
            !model.ExcludedProductWarnings.Any() && !model.MultipleDiscountNotices.Any() &&
            !model.CheapestItemSelectionDetails.Any() && !model.AttentionMessages.Any())
            return Content(string.Empty);

        MarkRenderedForRequest();
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Views/Shared/Components/CartSavings/Default.cshtml", model);
    }

    protected virtual bool HasAlreadyRenderedForRequest()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Items.ContainsKey(RenderedRequestItemKey) ?? false;
    }

    protected virtual void MarkRenderedForRequest()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
            context.Items[RenderedRequestItemKey] = true;
    }

    protected virtual async Task<IList<int>> GetPendingRewardOptionProductIdsAsync(PromotionRule rule, int? appliedRewardProductId, int storeId)
    {
        var productIds = new List<int>();

        if (appliedRewardProductId.HasValue && appliedRewardProductId.Value > 0)
            productIds.Add(appliedRewardProductId.Value);

        var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(rule.Id);
        foreach (var rewardProductId in ruleProducts
                     .Where(x => x.IsRewardProduct && x.ProductId > 0)
                     .Select(x => x.ProductId)
                     .Distinct())
        {
            if (!productIds.Contains(rewardProductId))
                productIds.Add(rewardProductId);
        }

        foreach (var rewardRuleProduct in ruleProducts.Where(x => x.IsRewardProduct))
        {
            var sourceProductIds = await GetRewardOptionProductIdsByRuleProductAsync(rewardRuleProduct, storeId);
            foreach (var sourceProductId in sourceProductIds)
            {
                if (!productIds.Contains(sourceProductId))
                    productIds.Add(sourceProductId);
            }
        }

        if (productIds.Any())
            return productIds;

        var tiers = await _promotionRuleService.GetRuleTiersByRuleIdAsync(rule.Id);
        foreach (var rewardProductId in tiers
                     .Where(x => x.RewardProductId.HasValue && x.RewardProductId.Value > 0)
                     .Select(x => x.RewardProductId!.Value)
                     .Distinct())
        {
            if (!productIds.Contains(rewardProductId))
                productIds.Add(rewardProductId);
        }

        return productIds;
    }

    protected virtual async Task<IList<int>> GetRewardOptionProductIdsByRuleProductAsync(PromotionRuleProduct rewardRuleProduct, int storeId)
    {
        if (rewardRuleProduct == null)
            return Array.Empty<int>();

        if (rewardRuleProduct.ProductId > 0)
            return new List<int> { rewardRuleProduct.ProductId };

        if (rewardRuleProduct.IsAllProducts)
        {
            var products = await _productService.SearchProductsAsync(
                storeId: storeId,
                visibleIndividuallyOnly: true,
                pageIndex: 0,
                pageSize: 2000,
                overridePublished: true);

            return products.Select(x => x.Id).Distinct().ToList();
        }

        if (rewardRuleProduct.CategoryId.HasValue && rewardRuleProduct.CategoryId.Value > 0)
        {
            var products = await _productService.SearchProductsAsync(
                categoryIds: new List<int> { rewardRuleProduct.CategoryId.Value },
                storeId: storeId,
                visibleIndividuallyOnly: true,
                pageIndex: 0,
                pageSize: 2000,
                overridePublished: true);

            return products.Select(x => x.Id).Distinct().ToList();
        }

        if (rewardRuleProduct.ManufacturerId.HasValue && rewardRuleProduct.ManufacturerId.Value > 0)
        {
            var products = await _productService.SearchProductsAsync(
                manufacturerIds: new List<int> { rewardRuleProduct.ManufacturerId.Value },
                storeId: storeId,
                visibleIndividuallyOnly: true,
                pageIndex: 0,
                pageSize: 2000,
                overridePublished: true);

            return products.Select(x => x.Id).Distinct().ToList();
        }

        if (rewardRuleProduct.VendorId.HasValue && rewardRuleProduct.VendorId.Value > 0)
        {
            var products = await _productService.SearchProductsAsync(
                vendorId: rewardRuleProduct.VendorId.Value,
                storeId: storeId,
                visibleIndividuallyOnly: true,
                pageIndex: 0,
                pageSize: 2000,
                overridePublished: true);

            return products.Select(x => x.Id).Distinct().ToList();
        }

        return Array.Empty<int>();
    }

    protected virtual async Task<string> GetSavingsBadgeAsync(AppliedPromotion promotion)
    {
        if (promotion == null)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");

        if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo");

        if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Percentage");

        if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount ||
            promotion.DiscountTypeId == (int)DiscountType.FixedBundlePrice)
        {
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Fixed");
        }

        return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
    }

    protected virtual async Task<string> GetSavingsDetailAsync(AppliedPromotion promotion)
    {
        if (promotion == null)
            return string.Empty;

        if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
        {
            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                return string.Format(
                    await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoFree"),
                    promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1);
            }

            return string.Format(
                await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoDiscounted"),
                promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1);
        }

        if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Percentage");

        if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount ||
            promotion.DiscountTypeId == (int)DiscountType.FixedBundlePrice)
        {
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Fixed");
        }

        return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Default");
    }

    protected virtual async Task<IList<string>> BuildTierUpgradeRemindersAsync(
        IList<ShoppingCartItem> cart,
        int storeId,
        IList<AppliedPromotion> appliedPromotions)
    {
        var reminders = new List<string>();
        if (cart == null || !cart.Any())
            return reminders;

        appliedPromotions ??= new List<AppliedPromotion>();
        var mandatorySelectionRuleIds = appliedPromotions
            .Where(x => x.RequiresRewardSelection)
            .Select(x => x.PromotionRuleId)
            .Distinct()
            .ToHashSet();

        var activeRules = await _promotionRuleService.GetActiveRulesAsync(storeId);
        foreach (var rule in activeRules.Where(x => x.RuleType == PromotionRuleType.BuyXGetY))
        {
            if (mandatorySelectionRuleIds.Contains(rule.Id))
                continue;

            var tiers = (await _promotionRuleService.GetRuleTiersByRuleIdAsync(rule.Id))
                .Where(x => x.MinQuantity > 0)
                .OrderBy(x => x.MinQuantity)
                .ToList();
            if (tiers.Count < 2)
                continue;

            var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(rule.Id);
            var buyProducts = ruleProducts.Where(x => !x.IsRewardProduct).ToList();
            if (!buyProducts.Any())
                continue;

            var eligibleItems = buyProducts.All(x => x.IsAllProducts)
                ? cart.Where(x => x.Quantity > 0).ToList()
                : await _promotionConditionEvaluator.GetMatchedCartItemsByRuleProductsAsync(cart, buyProducts);

            var eligibleQuantity = eligibleItems.Sum(x => Math.Max(0, x.Quantity));
            if (eligibleQuantity <= 0)
                continue;

            var reminder = await BuildTierUpgradeReminderAsync(rule, tiers, ruleProducts, eligibleQuantity);
            if (!string.IsNullOrWhiteSpace(reminder))
                reminders.Add(reminder);
        }

        return reminders.Distinct().ToList();
    }

    protected virtual async Task<string> BuildTierUpgradeReminderAsync(
        PromotionRule rule,
        IList<PromotionRuleTier> tiers,
        IList<PromotionRuleProduct> ruleProducts,
        int eligibleQuantity)
    {
        if (rule == null || tiers == null || !tiers.Any() || eligibleQuantity <= 0)
            return string.Empty;

        ruleProducts ??= new List<PromotionRuleProduct>();
        var hasExplicitRewardProducts = ruleProducts.Any(x => x.IsRewardProduct && x.ProductId > 0);
        var hasTierSpecificRewardProducts = tiers.Any(x => x.RewardProductId.HasValue && x.RewardProductId.Value > 0);
        var hasExplicitRewardScope = hasExplicitRewardProducts || hasTierSpecificRewardProducts;

        var tierSnapshots = tiers
            .Select(x => new
            {
                Tier = x,
                RewardQuantity = x.RewardQuantity > 0 ? x.RewardQuantity : 1,
                RequiredGroupSize = hasExplicitRewardScope
                    ? x.MinQuantity
                    : x.MinQuantity + (x.RewardQuantity > 0 ? x.RewardQuantity : 1)
            })
            .Where(x => x.RequiredGroupSize > 0)
            .OrderBy(x => x.RequiredGroupSize)
            .ToList();
        if (tierSnapshots.Count < 2)
            return string.Empty;

        var nextTierTarget = tierSnapshots
            .FirstOrDefault(x => eligibleQuantity < x.RequiredGroupSize);
        if (nextTierTarget == null)
            return string.Empty;

        var addQuantity = nextTierTarget.RequiredGroupSize - eligibleQuantity;
        if (addQuantity <= 0)
            return string.Empty;

        var offerText = await BuildTierOfferTextAsync(nextTierTarget.Tier);
        var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Template");
        return string.Format(template, addQuantity, offerText, rule.Name);
    }

    protected virtual async Task<string> BuildTierOfferTextAsync(PromotionRuleTier tier)
    {
        if (tier == null)
            return string.Empty;

        if (tier.DiscountType == DiscountType.FreeItem)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Free");

        if (tier.DiscountType == DiscountType.Percentage)
        {
            var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Percentage");
            return string.Format(template, tier.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture));
        }

        if (tier.DiscountType == DiscountType.FixedAmount)
        {
            var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Fixed");
            return string.Format(template, tier.DiscountValue.ToString("0.##", CultureInfo.InvariantCulture));
        }

        return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Discount");
    }

    /// <summary>
    /// Build messages about excluded products from store-wide offers
    /// </summary>
    protected virtual async Task<IList<string>> BuildExcludedProductWarningsAsync(
        IList<ShoppingCartItem> cart,
        int storeId,
        IList<AppliedPromotion> appliedPromotions)
    {
        var warnings = new List<string>();
        if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
            return warnings;

        try
        {
            // Get all active rules
            var activeRules = await _promotionRuleService.GetActiveRulesAsync(storeId);
            if (!activeRules.Any())
                return warnings;

            // Check for store-wide rules with product exclusions
            foreach (var rule in activeRules.Where(x => x.IsActive))
            {
                var ruleConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(rule.Id);
                var excludedProductIds = ruleConditions?
                    .Where(x => x.ExcludedProductId.HasValue && x.ExcludedProductId.Value > 0)
                    .Select(x => x.ExcludedProductId!.Value)
                    .Distinct()
                    .ToList();

                if (excludedProductIds == null || !excludedProductIds.Any())
                    continue;

                // Check if cart contains excluded products
                var cartExcludedProducts = cart
                    .Where(item => excludedProductIds.Contains(item.ProductId))
                    .Select(item => item.ProductId)
                    .Distinct()
                    .ToList();

                if (!cartExcludedProducts.Any())
                    continue;

                // Get product names for excluded items
                var excludedProductNames = new List<string>();
                foreach (var productId in cartExcludedProducts)
                {
                    var product = await _productService.GetProductByIdAsync(productId);
                    if (product != null && !string.IsNullOrEmpty(product.Name))
                        excludedProductNames.Add(product.Name);
                }

                if (excludedProductNames.Any())
                {
                    var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.ExcludedProducts");
                    var message = string.Format(template, string.Join(", ", excludedProductNames), rule.Name);
                    warnings.Add(message);
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Error building excluded product warnings", ex);
        }

        return warnings;
    }

    /// <summary>
    /// Build messages about multiple simultaneous discounts being applied
    /// </summary>
    protected virtual async Task<IList<string>> BuildMultipleDiscountNoticesAsync(IList<AppliedPromotion> appliedPromotions)
    {
        var notices = new List<string>();
        if (appliedPromotions == null || !appliedPromotions.Any())
            return notices;

        // Count BuyXGetY promotions that were actually applied
        var appliedBuyXGetYRules = appliedPromotions
            .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && x.DiscountAmount > 0)
            .ToList();

        if (appliedBuyXGetYRules.Count >= 2)
        {
            // ENHANCED: Provide more detailed information about the coordinated discounts
            var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.MultipleDiscounts");
            var message = string.Format(template, appliedBuyXGetYRules.Count);
            notices.Add(message);

            // NEW: Add explanation about cheapest-item selection
            var coordinationTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CoordinatedDiscounts");
            var coordinationMessage = string.Format(coordinationTemplate, appliedBuyXGetYRules.Count);
            notices.Add(coordinationMessage);

            // NEW: Add specific details about which discounts are being applied
            foreach (var promotion in appliedBuyXGetYRules.OrderByDescending(x => x.DiscountAmount))
            {
                var discountTypeText = promotion.DiscountTypeId == (int)DiscountType.FreeItem
                    ? await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo")
                    : promotion.DiscountTypeId == (int)DiscountType.Percentage
                        ? $"{promotion.DiscountValue.ToString("0.##")}% {await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount")}"
                        : await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");

                var targetProductText = !string.IsNullOrEmpty(promotion.TargetProductName)
                    ? $"{await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedTo")} {promotion.TargetProductName}"
                    : await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.AppliedToCheapest");

                var promotionDetail = $"{promotion.RuleName}: {discountTypeText} - {targetProductText}";
                notices.Add(promotionDetail);
            }
        }

        return notices;
    }

    /// <summary>
    /// Build details about which items were selected for cheapest-item discounts
    /// </summary>
    protected virtual async Task<IList<string>> BuildCheapestItemSelectionDetailsAsync(
        IList<ShoppingCartItem> cart,
        IList<AppliedPromotion> appliedPromotions)
    {
        var details = new List<string>();
        if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
            return details;

        try
        {
            // Find BuyXGetY promotions that use cheapest-item logic
            var cheapestItemPromotions = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY &&
                           x.LineDiscounts != null &&
                           x.LineDiscounts.Any(d => d.Value > 0))
                .ToList();

            // ENHANCED: Check if this is a dual-offer scenario for better messaging
            var isDualOfferScenario = cheapestItemPromotions.Count >= 2;

            foreach (var promotion in cheapestItemPromotions)
            {
                if (promotion.DiscountAmount <= 0)
                    continue;

                // FIXED: Use LineDiscounts to determine which products got discounts
                if (promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
                {
                    foreach (var lineDiscount in promotion.LineDiscounts.Where(x => x.Value > 0))
                    {
                        var discountedItem = cart.FirstOrDefault(x => x.Id == lineDiscount.Key);
                        if (discountedItem != null)
                        {
                            var product = await _productService.GetProductByIdAsync(discountedItem.ProductId);
                            if (product != null)
                            {
                                // ENHANCED: Build clearer detail message for this discounted item
                                if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                                {
                                    var freeItemTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.FreeItemApplied");
                                    var discountAmount = await _priceFormatter.FormatPriceAsync(lineDiscount.Value, true, false);
                                    var message = string.Format(freeItemTemplate, product.Name, discountAmount);
                                    details.Add(message);
                                }
                                else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                                {
                                    var percentageTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.PercentageDiscountApplied");
                                    var discountAmount = await _priceFormatter.FormatPriceAsync(lineDiscount.Value, true, false);
                                    var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
                                    var message = string.Format(percentageTemplate, product.Name, percentage, discountAmount);
                                    details.Add(message);
                                }
                                else
                                {
                                    var discountType = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
                                    var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemDetail");
                                    var discountAmount = await _priceFormatter.FormatPriceAsync(lineDiscount.Value, true, false);
                                    var message = string.Format(template, discountType, product.Name, discountAmount);
                                    details.Add(message);
                                }
                            }
                        }
                    }
                }
                // Fallback to TargetProductName if LineDiscounts don't have product info
                else if (!string.IsNullOrEmpty(promotion.TargetProductName))
                {
                    if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
                    {
                        var freeItemTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.FreeItemApplied");
                        var discountAmount = await _priceFormatter.FormatPriceAsync(promotion.DiscountAmount, true, false);
                        var message = string.Format(freeItemTemplate, promotion.TargetProductName, discountAmount);
                        details.Add(message);
                    }
                    else if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
                    {
                        var percentageTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.PercentageDiscountApplied");
                        var discountAmount = await _priceFormatter.FormatPriceAsync(promotion.DiscountAmount, true, false);
                        var percentage = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
                        var message = string.Format(percentageTemplate, promotion.TargetProductName, percentage, discountAmount);
                        details.Add(message);
                    }
                    else
                    {
                        var discountType = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount");
                        var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemDetail");
                        var discountAmount = await _priceFormatter.FormatPriceAsync(promotion.DiscountAmount, true, false);
                        var message = string.Format(template, discountType, promotion.TargetProductName, discountAmount);
                        details.Add(message);
                    }
                }
            }

            // NEW: Add a summary message for dual-offer scenarios explaining the coordination
            if (isDualOfferScenario && details.Any())
            {
                var coordinationSummary = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.DualOfferSummary");
                details.Insert(0, coordinationSummary); // Add at the beginning for prominence
            }
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Error building cheapest item selection details", ex);
        }

        return details;
    }

    /// <summary>
    /// Get a human-readable label for the discount type
    /// </summary>
    protected virtual async Task<string> GetDiscountTypeLabelAsync(AppliedPromotion promotion)
    {
        if (promotion == null)
            return string.Empty;

        if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
        {
            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo");
            }

            // FIXED: Use DiscountValue for percentage discounts, not DiscountAmount
            var discountPercent = promotion.DiscountValue > 0 ? promotion.DiscountValue : 50;
            return string.Format(
                await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoDiscounted"),
                promotion.RewardQuantity > 0 ? promotion.RewardQuantity : 1, discountPercent);
        }

        if (promotion.DiscountTypeId == (int)DiscountType.Percentage)
            return string.Format(await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Percentage"), promotion.DiscountValue.ToString("0.##"));

        if (promotion.DiscountTypeId == (int)DiscountType.FixedAmount)
            return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Fixed");

        return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Default");
    }

    /// <summary>
    /// Get text describing how the cheapest item was selected
    /// </summary>
    protected virtual async Task<string> GetCheapestItemSelectionTextAsync(AppliedPromotion promotion)
    {
        if (promotion == null)
            return string.Empty;

        if (promotion.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
        {
            // FIXED: Better logic to determine the target product name
            var targetProduct = promotion.TargetProductName;

            // If TargetProductName is not set but we have LineDiscounts, try to get it from there
            if (string.IsNullOrEmpty(targetProduct) && promotion.LineDiscounts != null && promotion.LineDiscounts.Any())
            {
                // For multi-offer scenarios, we need to identify which product received the discount
                // This will be handled in BuildCheapestItemSelectionDetailsAsync instead
                return await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.MultipleOfferApplied");
            }

            if (string.IsNullOrEmpty(targetProduct))
                return string.Empty;

            if (promotion.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                var template = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemSelected");
                return string.Format(template, targetProduct);
            }

            var discountTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.CartSavings.CheapestItemSelectedDiscount");
            var discountPercent = promotion.DiscountValue > 0 ? promotion.DiscountValue.ToString("0.##") : "50";
            return string.Format(discountTemplate, targetProduct, discountPercent);
        }

        return string.Empty;
    }

    /// <summary>
    /// Generates dual-offer attention messages, explanations, and maximization tips
    /// </summary>
    protected virtual async Task GenerateDualOfferMessagingAsync(
        IList<ShoppingCartItem> cart,
        IList<AppliedPromotion> appliedPromotions,
        CartSavingsModel model)
    {
        if (cart == null || !cart.Any() || appliedPromotions == null || !appliedPromotions.Any())
            return;

        try
        {
            // Check if we have coordinated discount allocations
            var buyXGetYPromotions = appliedPromotions
                .Where(x => x.RuleTypeId == (int)PromotionRuleType.BuyXGetY && !x.IsExclusive)
                .ToList();

            if (buyXGetYPromotions.Count >= 2)
            {
                // Get coordinated allocations from the discount manager service
                var evaluationContext = await _discountManagerPlusService
                    .GetRequestAppliedPromotionsAsync(
                        (await _workContext.GetCurrentCustomerAsync()).Id,
                        (await _storeContext.GetCurrentStoreAsync()).Id);

                // Generate attention messages for dual-offer scenarios
                var attentionMessages = await _promotionAttentionMessageService.GenerateDualOfferMessagesAsync(
                    appliedPromotions,
                    cart,
                    new Dictionary<int, List<DiscountAllocation>>()); // Empty for now - coordination service handles this

                if (attentionMessages.Any())
                {
                    foreach (var message in attentionMessages)
                    {
                        model.AttentionMessages.Add(message);
                    }
                }

                // Generate dual-offer explanation
                var dualOfferExplanation = await _promotionAttentionMessageService.GenerateDualOfferExplanationAsync(
                    appliedPromotions,
                    new Dictionary<int, List<DiscountAllocation>>(), // Will be populated by coordination service
                    cart);

                if (dualOfferExplanation != null && dualOfferExplanation.IsDualOfferScenario)
                {
                    model.DualOfferExplanation = dualOfferExplanation;
                }

                // Generate maximization tips
                var eligibleProductIds = cart.Select(x => x.ProductId).ToList();
                var maximizationTips = await _promotionAttentionMessageService.GenerateMaximizationTipsAsync(
                    appliedPromotions,
                    cart,
                    eligibleProductIds);

                if (maximizationTips.Any())
                {
                    foreach (var tip in maximizationTips.Take(5)) // Limit to top 5 tips
                    {
                        model.MaximizationTips.Add(tip);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync("Error generating dual-offer messaging", ex);
        }
    }
}
