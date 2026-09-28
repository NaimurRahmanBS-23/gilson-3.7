using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Controllers;

public class DiscountManagerPlusPublicController : BasePluginController
{
    private readonly ICategoryService _categoryService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductService _productService;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IProductAttributeParser _productAttributeParser;
    private readonly ISettingService _settingService;
    private readonly ILocalizationService _localizationService;
    private readonly IRewardSynchronizationService _rewardSynchronizationService;

    public DiscountManagerPlusPublicController(
        ICategoryService categoryService,
        IPromotionRuleService promotionRuleService,
        IDiscountManagerPlusService discountManagerPlusService,
        IManufacturerService manufacturerService,
        IProductService productService,
        IWorkContext workContext,
        IStoreContext storeContext,
        IShoppingCartService shoppingCartService,
        IProductAttributeParser productAttributeParser,
        ISettingService settingService,
        ILocalizationService localizationService,
        IRewardSynchronizationService rewardSynchronizationService)
    {
        _categoryService = categoryService;
        _promotionRuleService = promotionRuleService;
        _discountManagerPlusService = discountManagerPlusService;
        _manufacturerService = manufacturerService;
        _productService = productService;
        _workContext = workContext;
        _storeContext = storeContext;
        _shoppingCartService = shoppingCartService;
        _productAttributeParser = productAttributeParser;
        _settingService = settingService;
        _localizationService = localizationService;
        _rewardSynchronizationService = rewardSynchronizationService;
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> AddRewardToCart(int promotionRuleId, int rewardProductId, int rewardQuantity, IFormCollection form)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled)
            return Json(new { success = false, message = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        if (rule == null || !rule.IsActive)
            return Json(new { success = false, message = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

        var product = await _productService.GetProductByIdAsync(rewardProductId);
        if (product == null || product.Deleted)
            return Json(new { success = false, message = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

        if (!await IsRewardProductAllowedAsync(rule, rewardProductId))
            return Json(new { success = false, message = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var appliedPromotion = await _discountManagerPlusService.EvaluateRuleAsync(rule, cart);
        if (appliedPromotion == null)
            return Json(new { success = false, message = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible") });

        var quantityToAdd = appliedPromotion.RewardQuantity > 0 ? appliedPromotion.RewardQuantity : rewardQuantity;
        if (quantityToAdd <= 0)
            quantityToAdd = 1;

        var warnings = new List<string>();
        var customerEnteredPriceConverted = await _productAttributeParser.ParseCustomerEnteredPriceAsync(product, form);
        var attributes = await _productAttributeParser.ParseProductAttributesAsync(product, form, warnings);
        _productAttributeParser.ParseRentalDates(product, form, out var rentalStartDate, out var rentalEndDate);

        if (warnings.Any())
        {
            var message = string.Join(" ", warnings.Distinct());
            return Json(new { success = false, message });
        }

        var addWarnings = await _shoppingCartService.AddToCartAsync(
            customer,
            product,
            ShoppingCartType.ShoppingCart,
            store.Id,
            attributes,
            customerEnteredPriceConverted,
            rentalStartDate,
            rentalEndDate,
            quantityToAdd,
            false);

        if (addWarnings.Any())
        {
            var message = string.Join(" ", addWarnings.Distinct());
            if (string.IsNullOrWhiteSpace(message))
                message = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.AddFailed");
            return Json(new { success = false, message });
        }

        await _rewardSynchronizationService.TrackManualRewardAsync(
            customer,
            store.Id,
            rule.Id,
            product.Id,
            attributes,
            quantityToAdd);

        return Json(new { success = true });
    }

    private async Task<bool> IsRewardProductAllowedAsync(PromotionRule rule, int rewardProductId)
    {
        if (rewardProductId <= 0)
            return false;

        if (rule.RuleType == PromotionRuleType.BuyXGetY)
        {
            var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(rule.Id);
            var rewardRuleProducts = ruleProducts.Where(x => x.IsRewardProduct).ToList();
            foreach (var rewardRuleProduct in rewardRuleProducts)
            {
                if (await MatchesRewardRuleProductSourceAsync(rewardRuleProduct, rewardProductId))
                    return true;
            }

            var tiers = await _promotionRuleService.GetRuleTiersByRuleIdAsync(rule.Id);
            return tiers.Any(x => x.RewardProductId.HasValue && x.RewardProductId.Value == rewardProductId);
        }

        if (rule.RuleType == PromotionRuleType.ProductBased && rule.DiscountType == DiscountType.FreeItem)
        {
            var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(rule.Id);
            foreach (var rewardRuleProduct in ruleProducts.Where(x => x.IsRewardProduct))
            {
                if (await MatchesRewardRuleProductSourceAsync(rewardRuleProduct, rewardProductId))
                    return true;
            }

            return false;
        }

        return false;
    }

    private async Task<bool> MatchesRewardRuleProductSourceAsync(PromotionRuleProduct rewardRuleProduct, int rewardProductId)
    {
        if (rewardRuleProduct == null || rewardProductId <= 0)
            return false;

        if (rewardRuleProduct.ProductId > 0)
            return rewardRuleProduct.ProductId == rewardProductId;

        if (rewardRuleProduct.IsAllProducts)
            return true;

        var product = await _productService.GetProductByIdAsync(rewardProductId);
        if (product == null || product.Deleted)
            return false;

        if (rewardRuleProduct.VendorId.HasValue && rewardRuleProduct.VendorId.Value > 0)
            return product.VendorId == rewardRuleProduct.VendorId.Value;

        if (rewardRuleProduct.ManufacturerId.HasValue && rewardRuleProduct.ManufacturerId.Value > 0)
        {
            var manufacturerMappings = await _manufacturerService.GetProductManufacturersByProductIdAsync(rewardProductId, true);
            return manufacturerMappings.Any(x => x.ManufacturerId == rewardRuleProduct.ManufacturerId.Value);
        }

        if (rewardRuleProduct.CategoryId.HasValue && rewardRuleProduct.CategoryId.Value > 0)
        {
            var productCategoryIds = (await _categoryService.GetProductCategoriesByProductIdAsync(rewardProductId, true))
                .Select(x => x.CategoryId)
                .ToHashSet();
            if (!productCategoryIds.Any())
                return false;

            var rewardCategoryIds = (await _categoryService.GetChildCategoryIdsAsync(rewardRuleProduct.CategoryId.Value, 0, true))
                .ToHashSet();
            rewardCategoryIds.Add(rewardRuleProduct.CategoryId.Value);
            return productCategoryIds.Overlaps(rewardCategoryIds);
        }

        return false;
    }
}
