#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models.Extensions;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;
using NopStation.Plugin.Misc.Core.Controllers;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Controllers;

public class PromotionRuleController : NopStationAdminController
{
    #region Fields

    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly IDiscountService _discountService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger _logger;
    private readonly IManufacturerService _manufacturerService;
    private readonly INotificationService _notificationService;
    private readonly ICategoryService _categoryService;
    private readonly ICustomerService _customerService;
    private readonly IProductAttributeParser _productAttributeParser;
    private readonly IProductService _productService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;
    private readonly IPromotionRuleModelFactory _promotionRuleModelFactory;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IStoreMappingService _storeMappingService;
    private readonly IVendorService _vendorService;
    private readonly IPromotionRuleExcludedProductService _excludedProductService;

    #endregion

    #region Ctor

    public PromotionRuleController(
        IDiscountManagerPlusService discountManagerPlusService,
        IDiscountService discountService,
        ILocalizationService localizationService,
        ILogger logger,
        IManufacturerService manufacturerService,
        INotificationService notificationService,
        ICategoryService categoryService,
        ICustomerService customerService,
        IProductAttributeParser productAttributeParser,
        IProductService productService,
        IProductModelFactory productModelFactory,
        IDiscountManagerPlusRequirementService discountManagerPlusRequirementService,
        IPromotionRuleModelFactory promotionRuleModelFactory,
        IPromotionRuleService promotionRuleService,
        IStoreMappingService storeMappingService,
        IVendorService vendorService,
        IPromotionRuleExcludedProductService excludedProductService)
    {
        _discountManagerPlusService = discountManagerPlusService;
        _discountService = discountService;
        _localizationService = localizationService;
        _logger = logger;
        _manufacturerService = manufacturerService;
        _notificationService = notificationService;
        _categoryService = categoryService;
        _customerService = customerService;
        _productAttributeParser = productAttributeParser;
        _productService = productService;
        _productModelFactory = productModelFactory;
        _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
        _promotionRuleModelFactory = promotionRuleModelFactory;
        _promotionRuleService = promotionRuleService;
        _storeMappingService = storeMappingService;
        _vendorService = vendorService;
        _excludedProductService = excludedProductService;
    }

    #endregion

    #region Methods

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> List()
    {
        var searchModel = await _promotionRuleModelFactory.PreparePromotionRuleSearchModelAsync(new PromotionRuleSearchModel());
        return View(searchModel);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> List(PromotionRuleSearchModel searchModel)
    {
        var model = await _promotionRuleModelFactory.PreparePromotionRuleListModelAsync(searchModel);
        return Json(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> Create()
    {
        var model = await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(new PromotionRuleModel(), null);
        return View(model);
    }

    [HttpPost]
    [ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> Create(PromotionRuleModel model, bool continueEditing)
    {
        await ValidateParentDiscountAsync(model);

        if (ModelState.IsValid)
        {
            NormalizeDiscountConfiguration(model);
            await ValidateRuleSetupForActivationAsync(model);
        }

        if (!ModelState.IsValid)
            return View(await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(model, null));

        var rule = model.ToEntity<PromotionRule>();
        rule.DiscountId = ResolveParentDiscountId(model);
        rule.IsCumulativeWithDefaultDiscounts = false;
        ApplyStoreScopeState(rule, model);
        await _promotionRuleService.InsertPromotionRuleAsync(rule);
        await SaveStoreMappingsAsync(rule, model);
        await SyncLinkedDiscountRequirementsAsync(null, GetParentDiscountId(rule));

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Added"));

        if (continueEditing)
            return RedirectToAction(nameof(Edit), new { id = rule.Id });

        return RedirectToAction(nameof(List));
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> Edit(int id)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(id);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var model = await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(null, rule);
        return View(model);
    }

    [HttpPost]
    [ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> Edit(PromotionRuleModel model, bool continueEditing)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(model.Id);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var previousLinkedDiscountId = GetParentDiscountId(rule);
        await ValidateParentDiscountAsync(model);

        if (ModelState.IsValid)
        {
            NormalizeDiscountConfiguration(model);
            await ValidateRuleSetupForActivationAsync(model);
        }

        if (!ModelState.IsValid)
            return View(await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(model, rule));

        rule = model.ToEntity(rule);
        rule.DiscountId = ResolveParentDiscountId(model);
        rule.IsCumulativeWithDefaultDiscounts = false;
        ApplyStoreScopeState(rule, model);
        await _promotionRuleService.UpdatePromotionRuleAsync(rule);
        await SaveStoreMappingsAsync(rule, model);
        await SyncLinkedDiscountRequirementsAsync(previousLinkedDiscountId, GetParentDiscountId(rule));

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Updated"));

        if (continueEditing)
            return RedirectToAction(nameof(Edit), new { id = rule.Id });

        return RedirectToAction(nameof(List));
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> CreateParentDiscountRulePopup(int discountId)
    {
        var discount = await _discountService.GetDiscountByIdAsync(discountId);
        if (discount == null)
            return RedirectToAction(nameof(List));

        var model = await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(new PromotionRuleModel
        {
            DiscountId = discountId,
            IsDiscountBound = true,
            ParentDiscountName = $"{discount.Name} (#{discount.Id})",
            IsActive = true
        }, null);

        return View("ParentDiscountRulePopup", model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> CreateParentDiscountRulePopup(PromotionRuleModel model)
    {
        model.IsDiscountBound = true;
        await ValidateParentDiscountAsync(model);

        if (ModelState.IsValid)
            NormalizeDiscountConfiguration(model);

        if (!ModelState.IsValid)
            return View("ParentDiscountRulePopup", await PrepareDiscountBoundPopupModelAsync(model, null));

        var rule = model.ToEntity<PromotionRule>();
        rule.DiscountId = ResolveParentDiscountId(model);
        rule.IsCumulativeWithDefaultDiscounts = false;
        ApplyStoreScopeState(rule, model);
        await _promotionRuleService.InsertPromotionRuleAsync(rule);
        await SaveStoreMappingsAsync(rule, model);
        await SyncLinkedDiscountRequirementsAsync(null, GetParentDiscountId(rule));

        ViewBag.RefreshPage = true;
        return View("ParentDiscountRulePopup", await PrepareDiscountBoundPopupModelAsync(model, rule));
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> EditParentDiscountRulePopup(int id, int discountId = 0)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(id);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var model = await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(null, rule);
        if (model.DiscountId <= 0 && discountId > 0)
            model.DiscountId = discountId;
        model.IsDiscountBound = true;
        return View("ParentDiscountRulePopup", await PrepareDiscountBoundPopupModelAsync(model, rule));
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> EditParentDiscountRulePopup(PromotionRuleModel model)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(model.Id);
        if (rule == null)
            return RedirectToAction(nameof(List));

        model.IsDiscountBound = true;
        var previousLinkedDiscountId = GetParentDiscountId(rule);
        await ValidateParentDiscountAsync(model);

        if (ModelState.IsValid)
            NormalizeDiscountConfiguration(model);

        if (!ModelState.IsValid)
            return View("ParentDiscountRulePopup", await PrepareDiscountBoundPopupModelAsync(model, rule));

        rule = model.ToEntity(rule);
        rule.DiscountId = ResolveParentDiscountId(model);
        rule.IsCumulativeWithDefaultDiscounts = false;
        ApplyStoreScopeState(rule, model);
        await _promotionRuleService.UpdatePromotionRuleAsync(rule);
        await SaveStoreMappingsAsync(rule, model);
        await SyncLinkedDiscountRequirementsAsync(previousLinkedDiscountId, GetParentDiscountId(rule));

        ViewBag.RefreshPage = true;
        return View("ParentDiscountRulePopup", await PrepareDiscountBoundPopupModelAsync(model, rule));
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> DeleteParentDiscountRule(int id, int discountId)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(id);
        if (rule != null)
        {
            var parentDiscountId = GetParentDiscountId(rule);

            var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(rule.Id);
            foreach (var ruleProduct in ruleProducts)
                await _promotionRuleService.DeleteRuleProductAsync(ruleProduct);

            var ruleTiers = await _promotionRuleService.GetRuleTiersByRuleIdAsync(rule.Id);
            foreach (var ruleTier in ruleTiers)
                await _promotionRuleService.DeleteRuleTierAsync(ruleTier);

            var ruleConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(rule.Id);
            foreach (var ruleCondition in ruleConditions)
                await _promotionRuleService.DeleteRuleConditionAsync(ruleCondition);

            await _promotionRuleService.DeletePromotionRuleAsync(rule);
            await SyncLinkedDiscountRequirementsAsync(parentDiscountId > 0 ? parentDiscountId : discountId, null);
        }

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> ParentDiscountRuleList(PromotionRuleSearchModel searchModel)
    {
        var model = await _promotionRuleModelFactory.PreparePromotionRuleListModelAsync(searchModel);
        return Json(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(id);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var linkedDiscountId = GetParentDiscountId(rule);

        var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(rule.Id);
        foreach (var ruleProduct in ruleProducts)
            await _promotionRuleService.DeleteRuleProductAsync(ruleProduct);

        var ruleTiers = await _promotionRuleService.GetRuleTiersByRuleIdAsync(rule.Id);
        foreach (var ruleTier in ruleTiers)
            await _promotionRuleService.DeleteRuleTierAsync(ruleTier);

        var ruleConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(rule.Id);
        foreach (var ruleCondition in ruleConditions)
            await _promotionRuleService.DeleteRuleConditionAsync(ruleCondition);

        await _promotionRuleService.DeletePromotionRuleAsync(rule);
        await SyncLinkedDiscountRequirementsAsync(linkedDiscountId, null);

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Deleted"));

        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductList(PromotionRuleProductSearchModel searchModel)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(searchModel.PromotionRuleId);
        if (rule == null)
            return Json(new { });

        var model = await _promotionRuleModelFactory.PrepareRuleProductListModelAsync(searchModel);
        return Json(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductAddPopup(int promotionRuleId)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var model = new PromotionRuleProductAddModel
        {
            PromotionRuleId = promotionRuleId,
            MinQuantity = 1
        };

        await PrepareRuleProductSourceTypesAsync(model);
        await PrepareRuleProductSourceListsAsync(model);
        await PrepareRewardAttributeSelectionTypesAsync(model);
        ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
        return View(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductAddMultiplePopup(int promotionRuleId)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var model = await _productModelFactory.PrepareAddRelatedProductSearchModelAsync(new AddRelatedProductSearchModel());
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductAddMultiplePopupList(AddRelatedProductSearchModel searchModel)
    {
        var model = await _productModelFactory.PrepareAddRelatedProductListModelAsync(searchModel);
        return Json(model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductAddMultiplePopup(AddProductsToPromotionRuleModel model)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(model.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var selectedProductIds = (model.SelectedProductIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (selectedProductIds.Any())
        {
            var existingRuleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(model.PromotionRuleId);
            var existingProductIds = existingRuleProducts
                .Where(x => !x.IsAllProducts && x.ProductId > 0 && !x.IsRewardProduct)
                .Select(x => x.ProductId)
                .ToHashSet();

            foreach (var productId in selectedProductIds)
            {
                if (existingProductIds.Contains(productId))
                    continue;

                await _promotionRuleService.InsertRuleProductAsync(new PromotionRuleProduct
                {
                    PromotionRuleId = model.PromotionRuleId,
                    ProductId = productId,
                    MinQuantity = 1,
                    MaxQuantity = 0,
                    IsRewardProduct = false,
                    RewardAttributeSelectionTypeId = (int)RewardAttributeSelectionType.Any
                });
            }
        }

        ViewBag.RefreshPage = true;
        return View(await _productModelFactory.PrepareAddRelatedProductSearchModelAsync(new AddRelatedProductSearchModel()));
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductAddPopup(PromotionRuleProductAddModel model)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(model.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        await ValidateRuleProductSourceAsync(model);

        if (model.MaxQuantity > 0 && model.MaxQuantity < model.MinQuantity)
            ModelState.AddModelError(nameof(model.MaxQuantity), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity.Invalid"));

        if (model.SourceTypeId == (int)RuleProductSourceType.Product &&
            model.RewardAttributeSelectionTypeId == (int)RewardAttributeSelectionType.SpecificValues &&
            string.IsNullOrWhiteSpace(model.RewardAttributeValueIds))
        {
            ModelState.AddModelError(nameof(model.RewardAttributeValueIds),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Required"));
        }

        if (!ModelState.IsValid)
        {
            await PrepareRuleProductSelectedProductNameAsync(model);
            await PrepareRuleProductSourceTypesAsync(model);
            await PrepareRuleProductSourceListsAsync(model);
            await PrepareRewardAttributeSelectionTypesAsync(model);
            ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
            return View(model);
        }

        NormalizeRuleProductSource(model);

        var isRewardProduct = IsRewardProductAllowed(rule) && model.IsRewardProduct;

        var ruleProduct = new PromotionRuleProduct
        {
            PromotionRuleId = model.PromotionRuleId,
            ProductId = model.ProductId,
            CategoryId = model.CategoryId,
            ManufacturerId = model.ManufacturerId,
            VendorId = model.VendorId,
            MinQuantity = model.MinQuantity > 0 ? model.MinQuantity : 1,
            MaxQuantity = model.MaxQuantity > 0 ? model.MaxQuantity : 0,
            IsAllProducts = model.IsAllProducts,
            IsRewardProduct = isRewardProduct,
            RewardAttributeSelectionTypeId = model.RewardAttributeSelectionTypeId,
            RewardAttributeValueIds = model.RewardAttributeValueIds
        };

        await _promotionRuleService.InsertRuleProductAsync(ruleProduct);

        ViewBag.RefreshPage = true;
        return View(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductEditPopup(int id)
    {
        var ruleProduct = await _promotionRuleService.GetRuleProductByIdAsync(id);
        if (ruleProduct == null)
            return RedirectToAction(nameof(List));

        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(ruleProduct.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var model = new PromotionRuleProductAddModel
        {
            Id = ruleProduct.Id,
            PromotionRuleId = ruleProduct.PromotionRuleId,
            ProductId = ruleProduct.ProductId,
            CategoryId = ruleProduct.CategoryId,
            ManufacturerId = ruleProduct.ManufacturerId,
            VendorId = ruleProduct.VendorId,
            MinQuantity = ruleProduct.MinQuantity,
            MaxQuantity = ruleProduct.MaxQuantity,
            IsAllProducts = ruleProduct.IsAllProducts,
            IsRewardProduct = IsRewardProductAllowed(rule) && ruleProduct.IsRewardProduct,
            RewardAttributeSelectionTypeId = ruleProduct.RewardAttributeSelectionTypeId,
            RewardAttributeValueIds = ruleProduct.RewardAttributeValueIds
        };
        model.SourceTypeId = GetRuleProductSourceTypeId(ruleProduct);
        await PrepareRuleProductSourceTypesAsync(model);
        await PrepareRuleProductSourceListsAsync(model);
        await PrepareRewardAttributeSelectionTypesAsync(model);
        await PrepareRuleProductSelectedProductNameAsync(model);

        ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductEditPopup(PromotionRuleProductAddModel model)
    {
        var ruleProduct = await _promotionRuleService.GetRuleProductByIdAsync(model.Id);
        if (ruleProduct == null)
            return RedirectToAction(nameof(List));

        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(ruleProduct.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        await ValidateRuleProductSourceAsync(model);

        if (model.MaxQuantity > 0 && model.MaxQuantity < model.MinQuantity)
            ModelState.AddModelError(nameof(model.MaxQuantity), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity.Invalid"));

        if (model.SourceTypeId == (int)RuleProductSourceType.Product &&
            model.RewardAttributeSelectionTypeId == (int)RewardAttributeSelectionType.SpecificValues &&
            string.IsNullOrWhiteSpace(model.RewardAttributeValueIds))
        {
            ModelState.AddModelError(nameof(model.RewardAttributeValueIds),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Required"));
        }

        if (!ModelState.IsValid)
        {
            await PrepareRuleProductSourceTypesAsync(model);
            await PrepareRuleProductSourceListsAsync(model);
            await PrepareRewardAttributeSelectionTypesAsync(model);
            await PrepareRuleProductSelectedProductNameAsync(model);
            ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
            return View(model);
        }

        NormalizeRuleProductSource(model);

        var isRewardProduct = IsRewardProductAllowed(rule) && model.IsRewardProduct;

        ruleProduct.ProductId = model.ProductId;
        ruleProduct.CategoryId = model.CategoryId;
        ruleProduct.ManufacturerId = model.ManufacturerId;
        ruleProduct.VendorId = model.VendorId;
        ruleProduct.MinQuantity = model.MinQuantity > 0 ? model.MinQuantity : 1;
        ruleProduct.MaxQuantity = model.MaxQuantity > 0 ? model.MaxQuantity : 0;
        ruleProduct.IsAllProducts = model.IsAllProducts;
        ruleProduct.IsRewardProduct = isRewardProduct;
        ruleProduct.RewardAttributeSelectionTypeId = model.RewardAttributeSelectionTypeId;
        ruleProduct.RewardAttributeValueIds = model.RewardAttributeValueIds;

        await _promotionRuleService.UpdateRuleProductAsync(ruleProduct);

        ViewBag.RefreshPage = true;
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductAdd(int promotionRuleId, int productId, int minQuantity, int maxQuantity = 0, bool isRewardProduct = false)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        if (rule == null)
            return Json(new { Result = false });

        var ruleProduct = new PromotionRuleProduct
        {
            PromotionRuleId = promotionRuleId,
            ProductId = productId,
            MinQuantity = minQuantity > 0 ? minQuantity : 1,
            MaxQuantity = maxQuantity > 0 ? maxQuantity : 0,
            IsRewardProduct = IsRewardProductAllowed(rule) && isRewardProduct
        };

        await _promotionRuleService.InsertRuleProductAsync(ruleProduct);
        return Json(new { Result = true });
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductUpdate(int id, int productId, int minQuantity, int maxQuantity = 0, bool isRewardProduct = false)
    {
        var ruleProduct = await _promotionRuleService.GetRuleProductByIdAsync(id);
        if (ruleProduct == null)
            return Json(new { Result = false });

        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(ruleProduct.PromotionRuleId);
        if (rule == null)
            return Json(new { Result = false });

        if (productId > 0)
            ruleProduct.ProductId = productId;

        ruleProduct.MinQuantity = minQuantity > 0 ? minQuantity : 1;
        ruleProduct.MaxQuantity = maxQuantity > 0 ? maxQuantity : 0;
        ruleProduct.IsRewardProduct = IsRewardProductAllowed(rule) && isRewardProduct;

        await _promotionRuleService.UpdateRuleProductAsync(ruleProduct);
        return Json(new { Result = true });
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleProductDelete(int id)
    {
        var ruleProduct = await _promotionRuleService.GetRuleProductByIdAsync(id);
        if (ruleProduct != null)
            await _promotionRuleService.DeleteRuleProductAsync(ruleProduct);

        return new NullJsonResult();
    }

    #region Excluded Products

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public virtual async Task<IActionResult> ExcludedProductList(PromotionRuleExcludedProductSearchModel searchModel)
    {
        var model = await _promotionRuleModelFactory.PrepareExcludedProductListModelAsync(searchModel);
        return Json(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public virtual async Task<IActionResult> ExcludedProductAddMultiplePopup(int promotionRuleId)
    {
        var model = await _productModelFactory.PrepareAddRelatedProductSearchModelAsync(new AddRelatedProductSearchModel());
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public virtual async Task<IActionResult> ExcludedProductAddMultiplePopupList(AddRelatedProductSearchModel searchModel)
    {
        var model = await _productModelFactory.PrepareAddRelatedProductListModelAsync(searchModel);
        return Json(model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public virtual async Task<IActionResult> ExcludedProductAddMultiplePopup(AddExcludedProductsToPromotionRuleModel model)
    {
        if (!model.SelectedProductIds.Any())
            return ErrorJson(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectProducts"));

        await _excludedProductService.AddExcludedProductsAsync(model.PromotionRuleId, model.SelectedProductIds);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Added"));

        ViewBag.RefreshPage = true;
        return View(await _productModelFactory.PrepareAddRelatedProductSearchModelAsync(new AddRelatedProductSearchModel()));
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public virtual async Task<IActionResult> ExcludedProductDelete(int id)
    {
        var excludedProduct = await _excludedProductService.GetExcludedProductByIdAsync(id);
        if (excludedProduct != null)
            await _excludedProductService.DeleteExcludedProductAsync(excludedProduct);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public virtual async Task<IActionResult> ExcludedProductBulkDelete(IList<int> selectedIds)
    {
        if (selectedIds == null || !selectedIds.Any())
            return ErrorJson(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectExcludedProducts"));

        var excludedProducts = new List<PromotionRuleExcludedProduct>();
        foreach (var id in selectedIds)
        {
            var excludedProduct = await _excludedProductService.GetExcludedProductByIdAsync(id);
            if (excludedProduct != null)
                excludedProducts.Add(excludedProduct);
        }

        foreach (var excludedProduct in excludedProducts)
        {
            await _excludedProductService.DeleteExcludedProductAsync(excludedProduct);
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Deleted"));
        return Json(new { Result = true });
    }

    #endregion

    private static bool IsRewardProductAllowed(PromotionRule rule)
    {
        if (rule == null)
            return false;

        return rule.RuleType == PromotionRuleType.BuyXGetY ||
               (rule.RuleType == PromotionRuleType.ProductBased && rule.DiscountType == DiscountType.FreeItem);
    }

    private static void NormalizeDiscountConfiguration(PromotionRuleModel model)
    {
        if (model == null)
            return;

        if (model.DiscountId > 0)
        {
            model.IsActive = true;
            model.StartDate = null;
            model.EndDate = null;
            model.LinkedDiscountId = null;
            model.IsFlashEnabled = false;
            model.UsageLimitTotal = 0;
            model.UsageLimitPerCustomer = 0;
            model.UsageWindowStartUtc = null;
            model.UsageWindowEndUtc = null;
        }
        else if (!model.LinkedDiscountId.HasValue || model.LinkedDiscountId.Value <= 0)
        {
            model.LinkedDiscountId = null;
            model.CarryDefaultDiscount = false;
        }

        if (!Enum.IsDefined(typeof(DiscountScope), model.DiscountScopeId))
            model.DiscountScopeId = (int)DiscountScope.MatchedItemsOnly;

        if (model.RuleTypeId == (int)PromotionRuleType.BuyXGetY)
        {
            model.DiscountTypeId = 0;
            model.DiscountValue = 0;
            return;
        }

        if (model.DiscountTypeId == (int)DiscountType.FreeItem)
            model.DiscountValue = 0;
    }

    private static void ApplyStoreScopeState(PromotionRule rule, PromotionRuleModel model)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(model);

        var selectedStoreIds = (model.SelectedStoreIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        rule.LimitedToStores = selectedStoreIds.Any();
        rule.LimitedToStore = selectedStoreIds.Count == 1 ? selectedStoreIds[0] : 0;
    }

    private async Task SaveStoreMappingsAsync(PromotionRule rule, PromotionRuleModel model)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(model);

        var selectedStoreIds = (model.SelectedStoreIds ?? Array.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        await _storeMappingService.SaveStoreMappingsAsync(rule, selectedStoreIds);
    }

    private async Task SyncLinkedDiscountRequirementsAsync(int? previousLinkedDiscountId, int? currentLinkedDiscountId)
    {
        var linkedDiscountIds = new[] { previousLinkedDiscountId, currentLinkedDiscountId }
            .Where(x => x.HasValue && x.Value > 0)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        foreach (var linkedDiscountId in linkedDiscountIds)
            await _discountManagerPlusRequirementService.SyncLinkedDiscountRequirementsAsync(linkedDiscountId);
    }

    private async Task ValidateParentDiscountAsync(PromotionRuleModel model)
    {
        var parentDiscountId = ResolveParentDiscountId(model);
        if (parentDiscountId <= 0)
            return;

        var discount = await _discountService.GetDiscountByIdAsync(parentDiscountId);
        if (discount != null)
            return;

        ModelState.AddModelError(
            model.DiscountId > 0 ? nameof(model.DiscountId) : nameof(model.LinkedDiscountId),
            await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LinkedDiscount.Invalid"));
    }

    private async Task ValidateRuleSetupForActivationAsync(PromotionRuleModel model)
    {
        if (model == null || model.DiscountId > 0 || (!model.IsActive && model.DiscountId <= 0))
            return;

        var ruleType = (PromotionRuleType)model.RuleTypeId;
        var isExistingRule = model.Id > 0;

        var ruleProducts = isExistingRule
            ? await _promotionRuleService.GetRuleProductsByRuleIdAsync(model.Id)
            : new List<PromotionRuleProduct>();

        var ruleTiers = isExistingRule
            ? await _promotionRuleService.GetRuleTiersByRuleIdAsync(model.Id)
            : new List<PromotionRuleTier>();

        var ruleConditions = isExistingRule
            ? await _promotionRuleService.GetRuleConditionsByRuleIdAsync(model.Id)
            : new List<PromotionRuleCondition>();

        if (ruleType is PromotionRuleType.ProductBased or PromotionRuleType.ComboPricing or PromotionRuleType.BuyXGetY)
        {
            var hasBuyProducts = ruleType == PromotionRuleType.BuyXGetY
                ? ruleProducts.Any(x => !x.IsRewardProduct)
                : ruleProducts.Any();

            if (!hasBuyProducts)
            {
                ModelState.AddModelError(nameof(model.IsActive),
                    await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.ProductsRequiredForActivation"));
            }
        }

        if (ruleType == PromotionRuleType.BuyXGetY && !ruleTiers.Any())
        {
            ModelState.AddModelError(nameof(model.IsActive),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.TiersRequiredForActivation"));
        }

        if (ruleType is PromotionRuleType.CartCondition or PromotionRuleType.SubtotalBased && !ruleConditions.Any())
        {
            ModelState.AddModelError(nameof(model.IsActive),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.ConditionsRequiredForActivation"));
        }

        if (ruleType == PromotionRuleType.ProductBased && model.DiscountTypeId == (int)DiscountType.FreeItem)
        {
            var hasRewardProduct = ruleProducts.Any(x => x.IsRewardProduct && x.ProductId > 0);
            if (!hasRewardProduct)
            {
                ModelState.AddModelError(nameof(model.IsActive),
                    await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.RewardProductRequiredForFreeItem"));
            }
        }
    }

    private async Task<PromotionRuleModel> PrepareDiscountBoundPopupModelAsync(PromotionRuleModel model, PromotionRule? rule)
    {
        model = await _promotionRuleModelFactory.PreparePromotionRuleModelAsync(model, rule);
        model.IsDiscountBound = true;

        var parentDiscountId = ResolveParentDiscountId(model);
        if (parentDiscountId > 0)
        {
            model.DiscountId = parentDiscountId;
            var discount = await _discountService.GetDiscountByIdAsync(parentDiscountId);
            if (discount != null)
                model.ParentDiscountName = $"{discount.Name} (#{discount.Id})";
        }

        return model;
    }

    private static int ResolveParentDiscountId(PromotionRuleModel model)
    {
        if (model == null)
            return 0;

        if (model.DiscountId > 0)
            return model.DiscountId;

        return model.LinkedDiscountId.GetValueOrDefault();
    }

    private static int GetParentDiscountId(PromotionRule rule)
    {
        if (rule == null)
            return 0;

        return rule.DiscountId > 0 ? rule.DiscountId : rule.LinkedDiscountId.GetValueOrDefault();
    }


    private async Task PrepareRuleProductSourceTypesAsync(PromotionRuleProductAddModel model)
    {
        model.AvailableSourceTypes = new List<SelectListItem>
        {
            new()
            {
                Value = ((int)RuleProductSourceType.Product).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(RuleProductSourceType.Product)
            },
            new()
            {
                Value = ((int)RuleProductSourceType.Category).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(RuleProductSourceType.Category)
            },
            new()
            {
                Value = ((int)RuleProductSourceType.Manufacturer).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(RuleProductSourceType.Manufacturer)
            },
            new()
            {
                Value = ((int)RuleProductSourceType.Vendor).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(RuleProductSourceType.Vendor)
            }
        };
    }

    private async Task PrepareRuleProductSourceListsAsync(PromotionRuleProductAddModel model)
    {
        await PrepareCategorySelectListAsync(model.AvailableCategories);
        await PrepareManufacturerSelectListAsync(model.AvailableManufacturers);
        await PrepareVendorSelectListAsync(model.AvailableVendors);
    }

    private async Task PrepareCategorySelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
    {
        items.Clear();
        if (includeEmptyItem)
        {
            items.Add(new SelectListItem
            {
                Value = string.Empty,
                Text = await _localizationService.GetResourceAsync("Admin.Common.None")
            });
        }

        var categories = await _categoryService.GetAllCategoriesAsync(showHidden: true);
        foreach (var category in categories)
        {
            items.Add(new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.Name
            });
        }
    }

    private async Task PrepareManufacturerSelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
    {
        items.Clear();
        if (includeEmptyItem)
        {
            items.Add(new SelectListItem
            {
                Value = string.Empty,
                Text = await _localizationService.GetResourceAsync("Admin.Common.None")
            });
        }

        var manufacturers = await _manufacturerService.GetAllManufacturersAsync(showHidden: true);
        foreach (var manufacturer in manufacturers)
        {
            items.Add(new SelectListItem
            {
                Value = manufacturer.Id.ToString(),
                Text = manufacturer.Name
            });
        }
    }

    private async Task PrepareVendorSelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
    {
        items.Clear();
        if (includeEmptyItem)
        {
            items.Add(new SelectListItem
            {
                Value = string.Empty,
                Text = await _localizationService.GetResourceAsync("Admin.Common.None")
            });
        }

        var vendors = await _vendorService.GetAllVendorsAsync(showHidden: true, pageIndex: 0, pageSize: 2000);
        foreach (var vendor in vendors)
        {
            items.Add(new SelectListItem
            {
                Value = vendor.Id.ToString(),
                Text = vendor.Name
            });
        }
    }

    private async Task ValidateRuleProductSourceAsync(PromotionRuleProductAddModel model)
    {
        if (model.IsAllProducts)
            return;

        var sourceType = Enum.IsDefined(typeof(RuleProductSourceType), model.SourceTypeId)
            ? (RuleProductSourceType)model.SourceTypeId
            : RuleProductSourceType.Product;

        switch (sourceType)
        {
            case RuleProductSourceType.Product:
                if (model.ProductId <= 0)
                    ModelState.AddModelError(nameof(model.ProductId), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId.Hint"));
                break;
            case RuleProductSourceType.Category:
                if (!model.CategoryId.HasValue || model.CategoryId.Value <= 0)
                    ModelState.AddModelError(nameof(model.CategoryId), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId.Hint"));
                break;
            case RuleProductSourceType.Manufacturer:
                if (!model.ManufacturerId.HasValue || model.ManufacturerId.Value <= 0)
                    ModelState.AddModelError(nameof(model.ManufacturerId), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId.Hint"));
                break;
            case RuleProductSourceType.Vendor:
                if (!model.VendorId.HasValue || model.VendorId.Value <= 0)
                    ModelState.AddModelError(nameof(model.VendorId), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId.Hint"));
                break;
        }
    }

    private static void NormalizeRuleProductSource(PromotionRuleProductAddModel model)
    {
        if (model.IsAllProducts)
        {
            model.ProductId = 0;
            model.CategoryId = null;
            model.ManufacturerId = null;
            model.VendorId = null;
            model.SourceTypeId = (int)RuleProductSourceType.AllProducts;
            model.RewardAttributeSelectionTypeId = (int)RewardAttributeSelectionType.Any;
            model.RewardAttributeValueIds = null;
            return;
        }

        var sourceType = Enum.IsDefined(typeof(RuleProductSourceType), model.SourceTypeId)
            ? (RuleProductSourceType)model.SourceTypeId
            : RuleProductSourceType.Product;

        switch (sourceType)
        {
            case RuleProductSourceType.Product:
                model.CategoryId = null;
                model.ManufacturerId = null;
                model.VendorId = null;
                break;
            case RuleProductSourceType.Category:
                model.ProductId = 0;
                model.ManufacturerId = null;
                model.VendorId = null;
                break;
            case RuleProductSourceType.Manufacturer:
                model.ProductId = 0;
                model.CategoryId = null;
                model.VendorId = null;
                break;
            case RuleProductSourceType.Vendor:
                model.ProductId = 0;
                model.CategoryId = null;
                model.ManufacturerId = null;
                break;
        }

        if (sourceType != RuleProductSourceType.Product)
        {
            model.RewardAttributeSelectionTypeId = (int)RewardAttributeSelectionType.Any;
            model.RewardAttributeValueIds = null;
        }
    }

    private static int GetRuleProductSourceTypeId(PromotionRuleProduct ruleProduct)
    {
        if (ruleProduct.IsAllProducts)
            return (int)RuleProductSourceType.AllProducts;
        if (ruleProduct.CategoryId.HasValue && ruleProduct.CategoryId.Value > 0)
            return (int)RuleProductSourceType.Category;
        if (ruleProduct.ManufacturerId.HasValue && ruleProduct.ManufacturerId.Value > 0)
            return (int)RuleProductSourceType.Manufacturer;
        if (ruleProduct.VendorId.HasValue && ruleProduct.VendorId.Value > 0)
            return (int)RuleProductSourceType.Vendor;
        return (int)RuleProductSourceType.Product;
    }

    private async Task PrepareRewardAttributeSelectionTypesAsync(PromotionRuleProductAddModel model)
    {
        model.AvailableRewardAttributeSelectionTypes = new List<SelectListItem>
        {
            new()
            {
                Value = ((int)RewardAttributeSelectionType.Any).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(RewardAttributeSelectionType.Any)
            },
            new()
            {
                Value = ((int)RewardAttributeSelectionType.SpecificValues).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(RewardAttributeSelectionType.SpecificValues)
            }
        };
    }

    private async Task PrepareRuleProductSelectedProductNameAsync(PromotionRuleProductAddModel model)
    {
        if (model.ProductId <= 0)
        {
            model.ProductName = string.Empty;
            return;
        }

        var product = await _productService.GetProductByIdAsync(model.ProductId);
        model.ProductName = product?.Name ?? string.Empty;
    }

    private async Task PrepareTierRewardProductNameAsync(PromotionRuleTierModel model)
    {
        if (!model.RewardProductId.HasValue || model.RewardProductId.Value <= 0)
        {
            model.RewardProductName = string.Empty;
            return;
        }

        var product = await _productService.GetProductByIdAsync(model.RewardProductId.Value);
        model.RewardProductName = product?.Name ?? string.Empty;
    }

    private static bool SupportsTierRewardTargeting(PromotionRuleTierModel model)
    {
        if (model == null || model.RuleTypeId != (int)PromotionRuleType.BuyXGetY)
            return false;

        return model.DiscountTypeId == (int)DiscountType.FreeItem ||
               model.DiscountTypeId == (int)DiscountType.Percentage ||
               model.DiscountTypeId == (int)DiscountType.FixedAmount;
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleTierList(PromotionRuleTierSearchModel searchModel)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(searchModel.PromotionRuleId);
        if (rule == null)
            return Json(new { });

        var model = await _promotionRuleModelFactory.PrepareRuleTierListModelAsync(searchModel);
        return Json(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleTierCreatePopup(int promotionRuleId)
    {
        var model = await _promotionRuleModelFactory.PrepareRuleTierModelAsync(
            new PromotionRuleTierModel { PromotionRuleId = promotionRuleId }, null);
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleTierCreatePopup(PromotionRuleTierModel model)
    {
        model.SelectedRuleProductIds = model.SelectedRuleProductIds?
            .Where(x => x > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        if (!SupportsTierRewardTargeting(model))
        {
            model.RewardProductId = null;
            model.AutoAddReward = false;
        }
        else if (model.DiscountTypeId != (int)DiscountType.FreeItem)
            model.AutoAddReward = false;

        if (!ModelState.IsValid)
        {
            model = await _promotionRuleModelFactory.PrepareRuleTierModelAsync(model, null);
            await PrepareTierRewardProductNameAsync(model);
            return View(model);
        }

        var tier = model.ToEntity<PromotionRuleTier>();
        await _promotionRuleService.InsertRuleTierAsync(tier);
        await _promotionRuleService.SaveRuleTierProductMappingsAsync(model.PromotionRuleId, tier.Id, model.SelectedRuleProductIds);

        ViewBag.RefreshPage = true;
        return View(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleTierEditPopup(int id)
    {
        var tier = await _promotionRuleService.GetRuleTierByIdAsync(id);
        if (tier == null)
            return RedirectToAction(nameof(List));

        var model = await _promotionRuleModelFactory.PrepareRuleTierModelAsync(null, tier);
        await PrepareTierRewardProductNameAsync(model);
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleTierEditPopup(PromotionRuleTierModel model)
    {
        var tier = await _promotionRuleService.GetRuleTierByIdAsync(model.Id);
        if (tier == null)
            return RedirectToAction(nameof(List));

        model.SelectedRuleProductIds = model.SelectedRuleProductIds?
            .Where(x => x > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        if (!SupportsTierRewardTargeting(model))
        {
            model.RewardProductId = null;
            model.AutoAddReward = false;
        }
        else if (model.DiscountTypeId != (int)DiscountType.FreeItem)
            model.AutoAddReward = false;

        if (!ModelState.IsValid)
        {
            model = await _promotionRuleModelFactory.PrepareRuleTierModelAsync(model, tier);
            await PrepareTierRewardProductNameAsync(model);
            return View(model);
        }

        tier = model.ToEntity(tier);
        await _promotionRuleService.UpdateRuleTierAsync(tier);
        await _promotionRuleService.SaveRuleTierProductMappingsAsync(tier.PromotionRuleId, tier.Id, model.SelectedRuleProductIds);

        ViewBag.RefreshPage = true;
        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleTierDelete(int id)
    {
        var tier = await _promotionRuleService.GetRuleTierByIdAsync(id);
        if (tier != null)
            await _promotionRuleService.DeleteRuleTierAsync(tier);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionList(PromotionRuleConditionSearchModel searchModel)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(searchModel.PromotionRuleId);
        if (rule == null)
            return Json(new PromotionRuleConditionListModel
            {
                Data = new List<PromotionRuleConditionModel>(),
                Draw = searchModel.Draw,
                RecordsFiltered = 0,
                RecordsTotal = 0
            });

        try
        {
            var model = await _promotionRuleModelFactory.PrepareRuleConditionListModelAsync(searchModel);
            return Json(model);
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync($"Failed to load rule conditions for promotion rule {searchModel.PromotionRuleId}.", ex);
            return Json(new PromotionRuleConditionListModel
            {
                Data = new List<PromotionRuleConditionModel>(),
                Draw = searchModel.Draw,
                RecordsFiltered = 0,
                RecordsTotal = 0
            });
        }
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> GetRuleConditions(int promotionRuleId, int conditionId = 0, int groupId = 0, int interactionTypeId = 0, bool deleteCondition = false, bool deleteGroup = false)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        if (rule == null)
            return Json(new PromotionRuleConditionRequirementListModel());

        if (deleteCondition && conditionId > 0)
        {
            var condition = await _promotionRuleService.GetRuleConditionByIdAsync(conditionId);
            if (condition != null && condition.PromotionRuleId == promotionRuleId)
                await DeleteConditionWithChildrenAsync(condition);
        }

        if (deleteGroup && groupId > 0)
        {
            var groupConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(promotionRuleId);
            foreach (var condition in groupConditions.Where(x => (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == groupId))
                await _promotionRuleService.DeleteRuleConditionAsync(condition);
        }

        if (interactionTypeId > 0 && groupId > 0 && Enum.IsDefined(typeof(ConditionLogicalOperator), interactionTypeId))
        {
            var groupConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(promotionRuleId);
            var groupLookup = groupConditions.ToDictionary(x => x.Id);
            var rootConditions = groupConditions.Where(x =>
                (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == groupId &&
                (!x.ParentConditionId.HasValue || !groupLookup.ContainsKey(x.ParentConditionId.Value)));

            foreach (var condition in rootConditions)
            {
                condition.LogicalOperatorId = interactionTypeId;
                await _promotionRuleService.UpdateRuleConditionAsync(condition);
            }
        }

        var model = await _promotionRuleModelFactory.PrepareRuleConditionRequirementListModelAsync(promotionRuleId);
        return Json(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public IActionResult RuleConditionSelectProductPopup()
    {
        var model = new PromotionRuleSelectProductSearchModel();
        model.SetPopupGridPageSize();

        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionSelectProductPopupList(PromotionRuleSelectProductSearchModel searchModel)
    {
        var products = await _productService.SearchProductsAsync(
            keywords: searchModel.SearchProductName,
            showHidden: true,
            pageIndex: Math.Max(searchModel.Page - 1, 0),
            pageSize: searchModel.PageSize);

        var model = await new PromotionRuleSelectProductListModel().PrepareToGridAsync(searchModel, products, () =>
            products.Select(product => new PromotionRuleSelectProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Published = product.Published
            }).ToAsyncEnumerable());

        return Json(model);
    }

    [HttpPost]
    [FormValueRequired("save")]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionSelectProductPopup([Bind(Prefix = nameof(PromotionRuleSelectProductSearchModel.AddProductModel))] PromotionRuleConditionProductSelectorModel model)
    {
        var product = await _productService.GetProductByIdAsync(model.AssociatedToProductId);
        if (product == null)
            return Content("Cannot load a product");

        ViewBag.RefreshPage = true;
        ViewBag.ProductId = product.Id;
        ViewBag.ProductName = product.Name;

        var searchModel = new PromotionRuleSelectProductSearchModel();
        searchModel.SetPopupGridPageSize();
        return View(searchModel);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleRewardAttributePopup(int productId, string valueIdsInput, string btnId, string selectedValueIds)
    {
        if (productId <= 0)
            return Content("Select a product first");

        var model = await _promotionRuleModelFactory.PrepareRewardAttributePopupModelAsync(productId, selectedValueIds);
        ViewBag.ValueIdsInput = valueIdsInput;
        ViewBag.BtnId = btnId;
        return View("RuleRewardAttributePopup", model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleRewardAttributePopup(PromotionRuleRewardAttributePopupModel model, string valueIdsInput, string btnId, IFormCollection form)
    {
        var product = await _productService.GetProductByIdAsync(model.ProductId);
        if (product == null)
            return Content("Cannot load a product");

        var warnings = new List<string>();
        var attributesXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, warnings);
        if (!string.IsNullOrWhiteSpace(attributesXml))
        {
            var values = await _productAttributeParser.ParseProductAttributeValuesAsync(attributesXml);
            model.RewardAttributeValueIds = string.Join(",", values.Select(x => x.Id).Distinct());
        }

        ViewBag.ValueIdsInput = valueIdsInput;
        ViewBag.BtnId = btnId;

        if (warnings.Any())
        {
            var viewModel = await _promotionRuleModelFactory.PrepareRewardAttributePopupModelAsync(model.ProductId, model.RewardAttributeValueIds);
            viewModel.Warnings = warnings;
            return View("RuleRewardAttributePopup", viewModel);
        }

        ViewBag.RefreshPage = true;
        return View("RuleRewardAttributePopup", model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionCreatePopup(int promotionRuleId, int? conditionGroup = null)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var existingGroups = await GetConditionGroupsAsync(promotionRuleId, conditionGroup);
        var defaultGroup = conditionGroup.HasValue && conditionGroup.Value > 0
            ? conditionGroup.Value
            : existingGroups.Last();

        var model = await _promotionRuleModelFactory.PrepareRuleConditionModelAsync(
            new PromotionRuleConditionModel
            {
                PromotionRuleId = promotionRuleId,
                RuleTypeId = rule.RuleTypeId,
                ConditionGroup = defaultGroup,
                LogicalOperatorId = (int)ConditionLogicalOperator.And
            }, null);

        ViewBag.ExistingConditionGroups = existingGroups;
        ViewBag.NextConditionGroup = existingGroups.Max() + 1;
        return View("RuleConditionCreatePopup", model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionCreatePopup(PromotionRuleConditionModel model)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(model.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        model.RuleTypeId = rule.RuleTypeId;
        if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
            model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
        if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
            model.ParentConditionId = null;

        MapConditionSourceBuilderToStorage(model);
        MapConditionSelectionsToStorage(model);
        NormalizeConditionModelForRule(rule, model);

        await ValidateConditionSourceAsync(model);

        if (model.ConditionGroup <= 0)
            ModelState.AddModelError(nameof(model.ConditionGroup), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

        await ValidateConditionParentAsync(model, null);

        if (rule.RuleType != PromotionRuleType.SubtotalBased &&
            !HasAnyCartConditionCriteria(model))
        {
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));
        }

        await PrepareConditionSelectedNamesAsync(model);

        if (!ModelState.IsValid)
        {
            var existingGroups = await GetConditionGroupsAsync(model.PromotionRuleId, model.ConditionGroup);
            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;

            return View(await _promotionRuleModelFactory.PrepareRuleConditionModelAsync(model, null));
        }

        var condition = model.ToEntity<PromotionRuleCondition>();
        await _promotionRuleService.InsertRuleConditionAsync(condition);

        ViewBag.RefreshPage = true;
        return View(model);
    }

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionEditPopup(int id)
    {
        var condition = await _promotionRuleService.GetRuleConditionByIdAsync(id);
        if (condition == null)
            return RedirectToAction(nameof(List));

        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(condition.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        var model = await _promotionRuleModelFactory.PrepareRuleConditionModelAsync(null, condition);
        var existingGroups = await GetConditionGroupsAsync(condition.PromotionRuleId, condition.ConditionGroup);
        ViewBag.ExistingConditionGroups = existingGroups;
        ViewBag.NextConditionGroup = existingGroups.Max() + 1;

        return View("RuleConditionCreatePopup", model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionEditPopup(PromotionRuleConditionModel model)
    {
        var condition = await _promotionRuleService.GetRuleConditionByIdAsync(model.Id);
        if (condition == null)
            return RedirectToAction(nameof(List));

        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(condition.PromotionRuleId);
        if (rule == null)
            return RedirectToAction(nameof(List));

        model.PromotionRuleId = condition.PromotionRuleId;
        model.RuleTypeId = rule.RuleTypeId;
        if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
            model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
        if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
            model.ParentConditionId = null;

        MapConditionSourceBuilderToStorage(model);
        MapConditionSelectionsToStorage(model);
        NormalizeConditionModelForRule(rule, model);

        await ValidateConditionSourceAsync(model);

        if (model.ConditionGroup <= 0)
            ModelState.AddModelError(nameof(model.ConditionGroup), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

        await ValidateConditionParentAsync(model, condition);

        if (rule.RuleType != PromotionRuleType.SubtotalBased &&
            !HasAnyCartConditionCriteria(model))
        {
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));
        }

        await PrepareConditionSelectedNamesAsync(model);

        if (!ModelState.IsValid)
        {
            var existingGroups = await GetConditionGroupsAsync(model.PromotionRuleId, model.ConditionGroup);
            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;
            return View("RuleConditionCreatePopup", await _promotionRuleModelFactory.PrepareRuleConditionModelAsync(model, condition));
        }

        condition = model.ToEntity(condition);
        await _promotionRuleService.UpdateRuleConditionAsync(condition);

        ViewBag.RefreshPage = true;
        return View("RuleConditionCreatePopup", model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleConditionDelete(int id)
    {
        var condition = await _promotionRuleService.GetRuleConditionByIdAsync(id);
        if (condition != null)
            await DeleteConditionWithChildrenAsync(condition);

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES)]
    public async Task<IActionResult> RuleUsageHistoryList(PromotionRuleUsageHistorySearchModel searchModel)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(searchModel.PromotionRuleId);
        if (rule == null)
            return Json(new PromotionRuleUsageHistoryListModel());

        var model = await _promotionRuleModelFactory.PrepareRuleUsageHistoryListModelAsync(searchModel);
        return Json(model);
    }

    private async Task<List<int>> GetConditionGroupsAsync(int promotionRuleId, int? includeGroup = null)
    {
        var existingConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(promotionRuleId);
        var existingGroups = existingConditions
            .Select(x => x.ConditionGroup > 0 ? x.ConditionGroup : 1)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (includeGroup.HasValue && includeGroup.Value > 0 && !existingGroups.Contains(includeGroup.Value))
            existingGroups.Add(includeGroup.Value);

        if (!existingGroups.Any())
            existingGroups.Add(1);

        return existingGroups.OrderBy(x => x).ToList();
    }

    private static bool HasAnyCartConditionCriteria(PromotionRuleConditionModel model)
    {
        var builderState = ConditionSourceBuilderHelper.ParseBuilderStateJson(model?.ConditionSourceBuilderJson);
        var hasSourceBuilderValues = model != null &&
                                     model.ConditionSourceTypeId > 0 &&
                                     ConditionSourceBuilderHelper.HasConfiguredValues(model.ConditionSourceTypeId, builderState);

        return (model!.RequiredProductId ?? 0) > 0 ||
               (model.ExcludedProductId ?? 0) > 0 ||
               (model.RequiredCategoryId ?? 0) > 0 ||
               (model.RequiredVendorId ?? 0) > 0 ||
               (model.RequiredCustomerRoleId ?? 0) > 0 ||
               model.MinValue != 0 ||
               model.MaxValue != 0 ||
               model.IsFirstOrderOnly ||
               model.IsNewCustomerOnly ||
               (model.ConditionSourceTypeId > 0 && (!string.IsNullOrWhiteSpace(model.ConditionSourceData) || hasSourceBuilderValues)) ||
               !string.IsNullOrWhiteSpace(model.RequiredCountryCodesCsv) ||
               !string.IsNullOrWhiteSpace(model.RequiredPaymentMethodsCsv) ||
               model.RequiredOrderCountMin.HasValue ||
               model.RequiredOrderCountMax.HasValue ||
               model.QuantityMin.HasValue ||
               model.QuantityMax.HasValue;
    }

    private static bool IsLineScopedSourceType(int sourceTypeId)
    {
        if (!Enum.IsDefined(typeof(ConditionSourceType), sourceTypeId))
            return false;

        var sourceType = (ConditionSourceType)sourceTypeId;
        return sourceType == ConditionSourceType.Products ||
               sourceType == ConditionSourceType.Categories ||
               sourceType == ConditionSourceType.Manufacturers ||
               sourceType == ConditionSourceType.Vendors ||
               sourceType == ConditionSourceType.SpecificationAttributeOptions ||
               sourceType == ConditionSourceType.ProductAttributeValues ||
               sourceType == ConditionSourceType.ExpiryDays;
    }

    private static bool HasLineScopedCriteria(PromotionRuleConditionModel model)
    {
        return (model.RequiredProductId ?? 0) > 0 ||
               (model.ExcludedProductId ?? 0) > 0 ||
               (model.RequiredCategoryId ?? 0) > 0 ||
               (model.RequiredVendorId ?? 0) > 0 ||
               (model.ConditionSourceTypeId > 0 && IsLineScopedSourceType(model.ConditionSourceTypeId));
    }

    private static void NormalizeConditionModelForRule(PromotionRule rule, PromotionRuleConditionModel model)
    {
        model.RequiredCouponCodesCsv = null;

        if (rule.RuleType == PromotionRuleType.SubtotalBased)
        {
            model.RequiredProductId = null;
            model.ExcludedProductId = null;
            model.ConditionSourceTypeId = 0;
            model.ConditionSourceData = null;
            model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel());
            model.ConditionSourceBuilderTouched = false;
            model.SourceBuilderRows = new List<ConditionSourceBuilderRowModel>();
            model.SelectedSourceTokens = new List<string>();
            model.QuantityMin = null;
            model.QuantityMax = null;
            model.RequireSameLineMatch = false;
            model.AttributeMatchModeId = (int)AttributeMatchMode.Any;

            if (model.ConditionOperatorId != (int)ConditionOperator.Between)
                model.MaxValue = 0;

            return;
        }

        if (rule.RuleType == PromotionRuleType.CartCondition)
        {
            if (model.ConditionOperatorId != (int)ConditionOperator.Between)
                model.MaxValue = 0;
        }
        else
        {
            model.ConditionOperatorId = (int)ConditionOperator.GreaterThan;
            model.MinValue = 0;
            model.MaxValue = 0;
        }

        if (model.ConditionSourceTypeId <= 0)
        {
            model.ConditionSourceData = null;
            model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel());
            model.ConditionSourceBuilderTouched = false;
            model.SourceBuilderRows = new List<ConditionSourceBuilderRowModel>();
            model.SelectedSourceTokens = new List<string>();
            model.AttributeMatchModeId = (int)AttributeMatchMode.Any;
        }

        if (!HasLineScopedCriteria(model))
            model.RequireSameLineMatch = false;
    }

    private static void MapConditionSourceBuilderToStorage(PromotionRuleConditionModel model)
    {
        var builderState = ConditionSourceBuilderHelper.ParseBuilderStateJson(model.ConditionSourceBuilderJson);
        model.SourceBuilderRows = builderState.Rows?.ToList() ?? new List<ConditionSourceBuilderRowModel>();
        model.SelectedSourceTokens = builderState.Tokens?.ToList() ?? new List<string>();
        model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(builderState);

        if (model.ConditionSourceTypeId <= 0)
        {
            model.ConditionSourceData = null;
            return;
        }

        if (!model.ConditionSourceBuilderTouched &&
            !ConditionSourceBuilderHelper.HasConfiguredValues(model.ConditionSourceTypeId, builderState) &&
            !string.IsNullOrWhiteSpace(model.ConditionSourceData))
        {
            return;
        }

        model.ConditionSourceData = ConditionSourceBuilderHelper.SerializeSourceData(model.ConditionSourceTypeId, builderState);
    }

    private static void MapConditionSelectionsToStorage(PromotionRuleConditionModel model)
    {
        model.RequiredCountryCodesCsv = BuildCsv(model.SelectedCountryCodes, true);
        model.RequiredPaymentMethodsCsv = BuildCsv(model.SelectedPaymentMethodSystemNames);
        model.RequiredCouponCodesCsv = null;
    }

    private static string BuildCsv(IEnumerable<string> values, bool upperCase = false)
    {
        var items = (values ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => upperCase ? x.Trim().ToUpperInvariant() : x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Any() ? string.Join(",", items) : string.Empty;
    }

    private async Task ValidateConditionSourceAsync(PromotionRuleConditionModel model)
    {
        if (!Enum.IsDefined(typeof(ConditionRestrictionType), model.ConditionRestrictionTypeId))
            model.ConditionRestrictionTypeId = (int)ConditionRestrictionType.Include;

        var builderState = ConditionSourceBuilderHelper.ParseBuilderStateJson(model.ConditionSourceBuilderJson);
        var hasBuilderValues = model.ConditionSourceTypeId > 0 &&
                               ConditionSourceBuilderHelper.HasConfiguredValues(model.ConditionSourceTypeId, builderState);

        if (model.ConditionSourceTypeId > 0 && (string.IsNullOrWhiteSpace(model.ConditionSourceData) || hasBuilderValues))
        {
            var serializedSourceData = ConditionSourceBuilderHelper.SerializeSourceData(model.ConditionSourceTypeId, builderState);
            if (!string.IsNullOrWhiteSpace(serializedSourceData))
                model.ConditionSourceData = serializedSourceData;
        }

        if (model.ConditionSourceTypeId > 0 && !Enum.IsDefined(typeof(ConditionSourceType), model.ConditionSourceTypeId))
        {
            ModelState.AddModelError(nameof(model.ConditionSourceTypeId),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType.Invalid"));
        }

        if (model.ConditionSourceTypeId > 0 && string.IsNullOrWhiteSpace(model.ConditionSourceData) && !hasBuilderValues)
        {
            ModelState.AddModelError(nameof(model.ConditionSourceData),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData.Required"));
        }
        else if (model.ConditionSourceTypeId > 0 &&
                 (model.ConditionSourceBuilderTouched || hasBuilderValues))
        {
            foreach (var row in builderState.Rows ?? Array.Empty<ConditionSourceBuilderRowModel>())
            {
                if (model.ConditionSourceTypeId != (int)ConditionSourceType.ExpiryDays &&
                    (!row.EntryId.HasValue || row.EntryId.Value <= 0))
                {
                    ModelState.AddModelError(nameof(model.ConditionSourceData),
                        await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Entry.Required"));
                    break;
                }

                if (model.ConditionSourceTypeId == (int)ConditionSourceType.SpecificationAttributeOptions &&
                    (!row.EntryId.HasValue || row.EntryId.Value <= 0))
                {
                    ModelState.AddModelError(nameof(model.ConditionSourceData),
                        await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Attribute.Required"));
                    break;
                }

                if (model.ConditionSourceTypeId == (int)ConditionSourceType.ProductAttributeValues &&
                    (!row.EntryId.HasValue || row.EntryId.Value <= 0))
                {
                    ModelState.AddModelError(nameof(model.ConditionSourceData),
                        await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Attribute.Required"));
                    break;
                }

                if (row.RangeMin.HasValue && row.RangeMin.Value < 0 ||
                    row.RangeMax.HasValue && row.RangeMax.Value < 0)
                {
                    ModelState.AddModelError(nameof(model.ConditionSourceData),
                        await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Range.Invalid"));
                    break;
                }

                if (row.RangeMin.HasValue && row.RangeMax.HasValue && row.RangeMax.Value < row.RangeMin.Value)
                {
                    ModelState.AddModelError(nameof(model.ConditionSourceData),
                        await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Range.Invalid"));
                    break;
                }
            }
        }

        if (model.QuantityMin.HasValue && model.QuantityMin < 0)
            ModelState.AddModelError(nameof(model.QuantityMin),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin.Invalid"));

        if (model.QuantityMax.HasValue && model.QuantityMax < 0)
            ModelState.AddModelError(nameof(model.QuantityMax),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax.Invalid"));

        if (model.QuantityMin.HasValue && model.QuantityMax.HasValue && model.QuantityMax < model.QuantityMin)
            ModelState.AddModelError(nameof(model.QuantityMax),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityRange.Invalid"));

        if (model.RequiredOrderCountMin.HasValue && model.RequiredOrderCountMin.Value < 0)
        {
            ModelState.AddModelError(nameof(model.RequiredOrderCountMin),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin.Invalid"));
        }

        if (model.RequiredOrderCountMax.HasValue && model.RequiredOrderCountMax.Value < 0)
        {
            ModelState.AddModelError(nameof(model.RequiredOrderCountMax),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax.Invalid"));
        }

        if (model.RequiredOrderCountMin.HasValue &&
            model.RequiredOrderCountMax.HasValue &&
            model.RequiredOrderCountMax.Value > 0 &&
            model.RequiredOrderCountMax.Value < model.RequiredOrderCountMin.Value)
        {
            ModelState.AddModelError(nameof(model.RequiredOrderCountMax),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountRange.Invalid"));
        }

        if (!Enum.IsDefined(typeof(AttributeMatchMode), model.AttributeMatchModeId))
            model.AttributeMatchModeId = (int)AttributeMatchMode.Any;
    }

    private async Task ValidateConditionParentAsync(PromotionRuleConditionModel model, PromotionRuleCondition? existingCondition)
    {
        if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
        {
            model.ParentConditionId = null;
            return;
        }

        var parentCondition = await _promotionRuleService.GetRuleConditionByIdAsync(model.ParentConditionId.Value);
        if (parentCondition == null || parentCondition.PromotionRuleId != model.PromotionRuleId)
        {
            ModelState.AddModelError(nameof(model.ParentConditionId),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.Invalid"));
            return;
        }

        var parentGroup = parentCondition.ConditionGroup <= 0 ? 1 : parentCondition.ConditionGroup;
        var currentGroup = model.ConditionGroup <= 0 ? 1 : model.ConditionGroup;
        if (parentGroup != currentGroup)
        {
            ModelState.AddModelError(nameof(model.ParentConditionId),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.GroupMismatch"));
        }

        if (existingCondition == null)
            return;

        if (parentCondition.Id == existingCondition.Id)
        {
            ModelState.AddModelError(nameof(model.ParentConditionId),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.SelfReference"));
            return;
        }

        var conditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(model.PromotionRuleId);
        var childrenByParent = conditions
            .Where(x => x.ParentConditionId.HasValue && x.ParentConditionId.Value > 0)
            .GroupBy(x => x.ParentConditionId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToList());

        if (IsDescendantCondition(existingCondition.Id, parentCondition.Id, childrenByParent))
        {
            ModelState.AddModelError(nameof(model.ParentConditionId),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.CyclicReference"));
        }
    }

    private static bool IsDescendantCondition(int rootConditionId, int targetConditionId, Dictionary<int, List<int>> childrenByParent)
    {
        if (!childrenByParent.TryGetValue(rootConditionId, out var childIds))
            return false;

        var stack = new Stack<int>(childIds);
        var visited = new HashSet<int>();
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            if (current == targetConditionId)
                return true;

            if (childrenByParent.TryGetValue(current, out var nestedChildren))
            {
                foreach (var child in nestedChildren)
                    stack.Push(child);
            }
        }

        return false;
    }

    private async Task DeleteConditionWithChildrenAsync(PromotionRuleCondition condition)
    {
        if (condition == null)
            return;

        var allConditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(condition.PromotionRuleId);
        var childrenByParent = allConditions
            .Where(x => x.ParentConditionId.HasValue && x.ParentConditionId.Value > 0)
            .GroupBy(x => x.ParentConditionId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToList());

        var toDelete = new List<int>();
        var stack = new Stack<int>();
        stack.Push(condition.Id);
        var visited = new HashSet<int>();
        while (stack.Count > 0)
        {
            var currentId = stack.Pop();
            if (!visited.Add(currentId))
                continue;

            toDelete.Add(currentId);
            if (childrenByParent.TryGetValue(currentId, out var childIds))
            {
                foreach (var childId in childIds)
                    stack.Push(childId);
            }
        }

        foreach (var conditionId in toDelete.OrderByDescending(x => x))
        {
            var conditionToDelete = allConditions.FirstOrDefault(x => x.Id == conditionId)
                                    ?? await _promotionRuleService.GetRuleConditionByIdAsync(conditionId);
            if (conditionToDelete != null)
                await _promotionRuleService.DeleteRuleConditionAsync(conditionToDelete);
        }
    }

    private async Task PrepareConditionSelectedNamesAsync(PromotionRuleConditionModel model)
    {
        if (model.RequiredProductId.HasValue && model.RequiredProductId.Value > 0)
        {
            var product = await _productService.GetProductByIdAsync(model.RequiredProductId.Value);
            model.RequiredProductName = product?.Name ?? string.Empty;
        }
        else
        {
            model.RequiredProductName = string.Empty;
        }

        if (model.ExcludedProductId.HasValue && model.ExcludedProductId.Value > 0)
        {
            var product = await _productService.GetProductByIdAsync(model.ExcludedProductId.Value);
            model.ExcludedProductName = product?.Name ?? string.Empty;
        }
        else
        {
            model.ExcludedProductName = string.Empty;
        }

        if (model.RequiredCategoryId.HasValue && model.RequiredCategoryId.Value > 0)
        {
            var category = await _categoryService.GetCategoryByIdAsync(model.RequiredCategoryId.Value);
            model.RequiredCategoryName = category?.Name ?? string.Empty;
        }
        else
        {
            model.RequiredCategoryName = string.Empty;
        }

        if (model.RequiredVendorId.HasValue && model.RequiredVendorId.Value > 0)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(model.RequiredVendorId.Value);
            model.RequiredVendorName = vendor?.Name ?? string.Empty;
        }
        else
        {
            model.RequiredVendorName = string.Empty;
        }

        if (model.RequiredCustomerRoleId.HasValue && model.RequiredCustomerRoleId.Value > 0)
        {
            var role = (await _customerService.GetAllCustomerRolesAsync(true))
                .FirstOrDefault(x => x.Id == model.RequiredCustomerRoleId.Value);
            model.RequiredCustomerRoleName = role?.Name ?? string.Empty;
        }
        else
        {
            model.RequiredCustomerRoleName = string.Empty;
        }

        if (model.ParentConditionId.HasValue && model.ParentConditionId.Value > 0)
        {
            var parentCondition = await _promotionRuleService.GetRuleConditionByIdAsync(model.ParentConditionId.Value);
            if (parentCondition != null)
            {
                model.ParentConditionName = $"#{parentCondition.Id}";
            }
            else
            {
                model.ParentConditionName = string.Empty;
            }
        }
        else
        {
            model.ParentConditionName = string.Empty;
        }
    }

    /// <summary>
    /// Prepare available products for selection
    /// </summary>
    private async Task<IList<SelectListItem>> PrepareAvailableProductsAsync()
    {
        var products = await _productService.SearchProductsAsync(
            showHidden: true,
            pageIndex: 0,
            pageSize: 500);

        return products.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = $"{x.Name} (SKU: {x.Sku})"
        }).ToList();
    }

    #endregion
}

