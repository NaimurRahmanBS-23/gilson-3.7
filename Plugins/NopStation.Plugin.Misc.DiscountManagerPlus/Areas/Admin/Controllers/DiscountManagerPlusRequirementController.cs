using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Vendors;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;
using NopStation.Plugin.Misc.Core.Controllers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Controllers;

public class DiscountManagerPlusRequirementController : NopStationAdminController
{
    private readonly ICategoryService _categoryService;
    private readonly ICustomerService _customerService;
    private readonly IDiscountService _discountService;
    private readonly ILocalizationService _localizationService;
    private readonly IProductService _productService;
    private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;
    private readonly IPromotionRuleModelFactory _promotionRuleModelFactory;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly ISettingService _settingService;
    private readonly IVendorService _vendorService;

    public DiscountManagerPlusRequirementController(
        ICategoryService categoryService,
        ICustomerService customerService,
        IDiscountService discountService,
        ILocalizationService localizationService,
        IProductService productService,
        IDiscountManagerPlusRequirementService discountManagerPlusRequirementService,
        IPromotionRuleModelFactory promotionRuleModelFactory,
        IPromotionRuleService promotionRuleService,
        ISettingService settingService,
        IVendorService vendorService)
    {
        _categoryService = categoryService;
        _customerService = customerService;
        _discountService = discountService;
        _localizationService = localizationService;
        _productService = productService;
        _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
        _promotionRuleModelFactory = promotionRuleModelFactory;
        _promotionRuleService = promotionRuleService;
        _settingService = settingService;
        _vendorService = vendorService;
    }

    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> Configure(int discountId, int? discountRequirementId)
    {
        var discount = await _discountService.GetDiscountByIdAsync(discountId)
            ?? throw new ArgumentException("Discount could not be loaded");

        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>();
        var requirementId = discountRequirementId ?? 0;

        var model = new DiscountManagerPlusRequirementModel
        {
            DiscountId = discount.Id,
            RequirementId = requirementId,
            DefaultDiscountPipelineEnabled = settings.UseDefaultDiscountPipeline,
            ConditionSearchModel = new PromotionRuleConditionSearchModel
            {
                DiscountRequirementId = requirementId
            }
        };

        ViewData.TemplateInfo.HtmlFieldPrefix = string.Format(
            DiscountManagerPlusDefaults.DiscountRequirementHtmlFieldPrefix,
            requirementId);

        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/Configure.cshtml", model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> Configure(DiscountManagerPlusRequirementModel model)
    {
        var discount = await _discountService.GetDiscountByIdAsync(model.DiscountId);
        if (discount == null)
            return NotFound(new { Errors = new[] { "Discount could not be loaded" } });

        if (model.RequirementId <= 0)
        {
            var discountRequirement = new DiscountRequirement
            {
                DiscountId = model.DiscountId,
                DiscountRequirementRuleSystemName = DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName
            };
            await _discountService.InsertDiscountRequirementAsync(discountRequirement);
            await _discountManagerPlusRequirementService.MarkAdvancedConditionsRequirementAsync(discountRequirement.Id);

            return Ok(new { NewRequirementId = discountRequirement.Id });
        }

        await _discountManagerPlusRequirementService.MarkAdvancedConditionsRequirementAsync(model.RequirementId);
        return Ok(new { NewRequirementId = model.RequirementId });
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> GetRequirementConditions(int discountRequirementId, int conditionId = 0, int groupId = 0, int interactionTypeId = 0, bool deleteCondition = false, bool deleteGroup = false)
    {
        var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId);
        if (discountRequirement == null)
            return Json(new PromotionRuleConditionRequirementListModel());

        if (deleteCondition && conditionId > 0)
        {
            var condition = await _promotionRuleService.GetRuleConditionByIdAsync(conditionId);
            if (condition != null && condition.DiscountRequirementId == discountRequirementId)
                await DeleteConditionWithChildrenAsync(condition, discountRequirementId);
        }

        if (deleteGroup && groupId > 0)
        {
            var groupConditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
            foreach (var condition in groupConditions.Where(x => (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == groupId))
                await _promotionRuleService.DeleteRuleConditionAsync(condition);
        }

        if (interactionTypeId > 0 && groupId > 0 && Enum.IsDefined(typeof(ConditionLogicalOperator), interactionTypeId))
        {
            var groupConditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
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

        var model = await _promotionRuleModelFactory.PrepareDiscountRequirementConditionListModelAsync(discountRequirementId);
        return Json(model);
    }

    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public IActionResult RequirementConditionSelectProductPopup()
    {
        var model = new PromotionRuleSelectProductSearchModel();
        model.SetPopupGridPageSize();
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionSelectProductPopup.cshtml", model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionSelectProductPopupList(PromotionRuleSelectProductSearchModel searchModel)
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
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionSelectProductPopup([Bind(Prefix = nameof(PromotionRuleSelectProductSearchModel.AddProductModel))] PromotionRuleConditionProductSelectorModel model)
    {
        var product = await _productService.GetProductByIdAsync(model.AssociatedToProductId);
        if (product == null)
            return Content("Cannot load a product");

        ViewBag.RefreshPage = true;
        ViewBag.ProductId = product.Id;
        ViewBag.ProductName = product.Name;

        var searchModel = new PromotionRuleSelectProductSearchModel();
        searchModel.SetPopupGridPageSize();
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionSelectProductPopup.cshtml", searchModel);
    }

    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionCreatePopup(int discountRequirementId, int? conditionGroup = null)
    {
        var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId);
        if (discountRequirement == null)
            return Content("Save the requirement first.");

        var existingGroups = await GetConditionGroupsAsync(discountRequirementId, conditionGroup);
        var defaultGroup = conditionGroup.HasValue && conditionGroup.Value > 0
            ? conditionGroup.Value
            : existingGroups.Last();

        var model = await PrepareRequirementConditionModelAsync(
            new PromotionRuleConditionModel
            {
                DiscountRequirementId = discountRequirementId,
                ConditionGroup = defaultGroup,
                LogicalOperatorId = (int)ConditionLogicalOperator.And
            }, null);

        ViewBag.ExistingConditionGroups = existingGroups;
        ViewBag.NextConditionGroup = existingGroups.Max() + 1;
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionCreatePopup.cshtml", model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionCreatePopup(PromotionRuleConditionModel model)
    {
        var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(model.DiscountRequirementId);
        if (discountRequirement == null)
            return Content("Save the requirement first.");

        if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
            model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
        if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
            model.ParentConditionId = null;

        MapConditionSourceBuilderToStorage(model);
        MapConditionSelectionsToStorage(model);
        NormalizeConditionModelForRequirement(model);
        await ValidateConditionSourceAsync(model);

        if (model.ConditionGroup <= 0)
            ModelState.AddModelError(nameof(model.ConditionGroup), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

        await ValidateConditionParentAsync(model, null);

        if (!HasAnyDiscountRequirementCriteria(model))
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));

        await PrepareConditionSelectedNamesAsync(model);

        if (!ModelState.IsValid)
        {
            var existingGroups = await GetConditionGroupsAsync(model.DiscountRequirementId, model.ConditionGroup);
            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;

            return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionCreatePopup.cshtml",
                await PrepareRequirementConditionModelAsync(model, null));
        }

        var condition = model.ToEntity<PromotionRuleCondition>();
        condition.PromotionRuleId = 0;
        await _promotionRuleService.InsertRuleConditionAsync(condition);

        ViewBag.RefreshPage = true;
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionCreatePopup.cshtml", model);
    }

    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionEditPopup(int id)
    {
        var condition = await _promotionRuleService.GetRuleConditionByIdAsync(id);
        if (condition == null || condition.DiscountRequirementId <= 0)
            return Content("Condition could not be loaded.");

        var model = await PrepareRequirementConditionModelAsync(null, condition);
        var existingGroups = await GetConditionGroupsAsync(condition.DiscountRequirementId, condition.ConditionGroup);
        ViewBag.ExistingConditionGroups = existingGroups;
        ViewBag.NextConditionGroup = existingGroups.Max() + 1;

        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionCreatePopup.cshtml", model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionEditPopup(PromotionRuleConditionModel model)
    {
        var condition = await _promotionRuleService.GetRuleConditionByIdAsync(model.Id);
        if (condition == null || condition.DiscountRequirementId <= 0)
            return Content("Condition could not be loaded.");

        model.DiscountRequirementId = condition.DiscountRequirementId;
        if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
            model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
        if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
            model.ParentConditionId = null;

        MapConditionSourceBuilderToStorage(model);
        MapConditionSelectionsToStorage(model);
        NormalizeConditionModelForRequirement(model);
        await ValidateConditionSourceAsync(model);

        if (model.ConditionGroup <= 0)
            ModelState.AddModelError(nameof(model.ConditionGroup), await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

        await ValidateConditionParentAsync(model, condition);

        if (!HasAnyDiscountRequirementCriteria(model))
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));

        await PrepareConditionSelectedNamesAsync(model);

        if (!ModelState.IsValid)
        {
            var existingGroups = await GetConditionGroupsAsync(model.DiscountRequirementId, model.ConditionGroup);
            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;
            return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionCreatePopup.cshtml",
                await PrepareRequirementConditionModelAsync(model, condition));
        }

        condition = model.ToEntity(condition);
        condition.PromotionRuleId = 0;
        await _promotionRuleService.UpdateRuleConditionAsync(condition);

        ViewBag.RefreshPage = true;
        return View("~/Plugins/NopStation.Plugin.Misc.DiscountManagerPlus/Areas/Admin/Views/DiscountManagerPlusRequirement/RequirementConditionCreatePopup.cshtml", model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [CheckPermission(StandardPermission.Promotions.DISCOUNTS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> RequirementConditionDelete(int id)
    {
        var condition = await _promotionRuleService.GetRuleConditionByIdAsync(id);
        if (condition != null && condition.DiscountRequirementId > 0)
            await DeleteConditionWithChildrenAsync(condition, condition.DiscountRequirementId);

        return new NullJsonResult();
    }

    private async Task<PromotionRuleConditionModel> PrepareRequirementConditionModelAsync(
        PromotionRuleConditionModel model,
        PromotionRuleCondition condition)
    {
        var preparedModel = await _promotionRuleModelFactory.PrepareRuleConditionModelAsync(model, condition);

        if (!preparedModel.AvailableConditionOperators.Any(x => string.IsNullOrEmpty(x.Value)))
        {
            preparedModel.AvailableConditionOperators.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Admin.Common.Select")
            });
        }

        return preparedModel;
    }

    private async Task<List<int>> GetConditionGroupsAsync(int discountRequirementId, int? includeGroup = null)
    {
        var existingConditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
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

    private static bool HasAnyDiscountRequirementCriteria(PromotionRuleConditionModel model)
    {
        return HasAnyCartConditionCriteria(model) ||
               model.ConditionOperatorId > 0 ||
               model.MinValue != 0 ||
               model.MaxValue != 0;
    }

    private static bool HasAnyCartConditionCriteria(PromotionRuleConditionModel model)
    {
        var builderState = ConditionSourceBuilderHelper.ParseBuilderStateJson(model?.ConditionSourceBuilderJson);
        var hasSourceBuilderValues = model != null &&
                                     model.ConditionSourceTypeId > 0 &&
                                     ConditionSourceBuilderHelper.HasConfiguredValues(model.ConditionSourceTypeId, builderState);

        return (model.RequiredProductId ?? 0) > 0 ||
               (model.ExcludedProductId ?? 0) > 0 ||
               (model.RequiredCategoryId ?? 0) > 0 ||
               (model.RequiredVendorId ?? 0) > 0 ||
               (model.RequiredCustomerRoleId ?? 0) > 0 ||
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

    private static void NormalizeConditionModelForRequirement(PromotionRuleConditionModel model)
    {
        model.RequiredCouponCodesCsv = null;

        if (model.ConditionOperatorId != (int)ConditionOperator.Between)
            model.MaxValue = 0;

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

                if ((model.ConditionSourceTypeId == (int)ConditionSourceType.SpecificationAttributeOptions ||
                     model.ConditionSourceTypeId == (int)ConditionSourceType.ProductAttributeValues) &&
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
            ModelState.AddModelError(nameof(model.RequiredOrderCountMin),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin.Invalid"));

        if (model.RequiredOrderCountMax.HasValue && model.RequiredOrderCountMax.Value < 0)
            ModelState.AddModelError(nameof(model.RequiredOrderCountMax),
                await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax.Invalid"));

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

    private async Task ValidateConditionParentAsync(PromotionRuleConditionModel model, PromotionRuleCondition existingCondition)
    {
        if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
        {
            model.ParentConditionId = null;
            return;
        }

        var parentCondition = await _promotionRuleService.GetRuleConditionByIdAsync(model.ParentConditionId.Value);
        if (parentCondition == null || parentCondition.DiscountRequirementId != model.DiscountRequirementId)
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

        var conditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(model.DiscountRequirementId);
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

    private async Task DeleteConditionWithChildrenAsync(PromotionRuleCondition condition, int discountRequirementId)
    {
        var allConditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
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
            model.ParentConditionName = parentCondition != null ? $"#{parentCondition.Id}" : string.Empty;
        }
        else
        {
            model.ParentConditionName = string.Empty;
        }
    }
}
