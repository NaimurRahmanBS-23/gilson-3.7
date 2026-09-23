using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Infrastructure;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Controllers
{
    [AdminAuthorize]
    public class DiscountManagerPlusRequirementController : BasePluginController
    {
        private const string RequirementViewPath = "~/Plugins/Misc.DiscountManagerPlus/Views/DiscountManagerPlusRequirement/";

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
        private readonly IPermissionService _permissionService;

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
            IVendorService vendorService,
            IPermissionService permissionService)
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
            _permissionService = permissionService;
        }

        private bool CanManage()
        {
            return _permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManagePromotionRules);
        }

        public ActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            var settings = _settingService.LoadSetting<DiscountManagerPlusSettings>();
            var requirementId = discountRequirementId.HasValue ? discountRequirementId.Value : 0;

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

            return View(RequirementViewPath + "Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Configure(DiscountManagerPlusRequirementModel model)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var discount = _discountService.GetDiscountById(model.DiscountId);
            if (discount == null)
                return HttpNotFound();

            if (model.RequirementId <= 0)
            {
                var discountRequirement = new DiscountRequirement
                {
                    DiscountRequirementRuleSystemName = DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName
                };
                discount.DiscountRequirements.Add(discountRequirement);
                _discountService.UpdateDiscount(discount);
                _discountManagerPlusRequirementService.MarkAdvancedConditionsRequirement(discountRequirement.Id);

                return Json(new { NewRequirementId = discountRequirement.Id });
            }

            _discountManagerPlusRequirementService.MarkAdvancedConditionsRequirement(model.RequirementId);
            return Json(new { NewRequirementId = model.RequirementId });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult GetRequirementConditions(int discountRequirementId, int conditionId = 0, int groupId = 0, int interactionTypeId = 0, bool deleteCondition = false, bool deleteGroup = false)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var discountRequirement = FindDiscountRequirement(discountRequirementId);
            if (discountRequirement == null)
                return Json(new PromotionRuleConditionRequirementListModel());

            if (deleteCondition && conditionId > 0)
            {
                var condition = _promotionRuleService.GetRuleConditionById(conditionId);
                if (condition != null && condition.DiscountRequirementId == discountRequirementId)
                    DeleteConditionWithChildren(condition, discountRequirementId);
            }

            if (deleteGroup && groupId > 0)
            {
                var groupConditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
                foreach (var condition in groupConditions.Where(x => (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == groupId))
                    _promotionRuleService.DeleteRuleCondition(condition);
            }

            if (interactionTypeId > 0 && groupId > 0 && Enum.IsDefined(typeof(ConditionLogicalOperator), interactionTypeId))
            {
                var groupConditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
                var groupLookup = groupConditions.ToDictionary(x => x.Id);
                var rootConditions = groupConditions.Where(x =>
                    (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == groupId &&
                    (!x.ParentConditionId.HasValue || !groupLookup.ContainsKey(x.ParentConditionId.Value)));

                foreach (var condition in rootConditions)
                {
                    condition.LogicalOperatorId = interactionTypeId;
                    _promotionRuleService.UpdateRuleCondition(condition);
                }
            }

            var model = _promotionRuleModelFactory.PrepareDiscountRequirementConditionListModel(discountRequirementId);
            return Json(model);
        }

        public ActionResult RequirementConditionSelectProductPopup()
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var model = _promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel());
            return View(RequirementViewPath + "RequirementConditionSelectProductPopup.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RequirementConditionSelectProductPopupList(DataSourceRequest command, PromotionRuleSelectProductSearchModel searchModel)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            int totalCount;
            var pageIndex = command != null && command.Page > 0 ? command.Page - 1 : 0;
            var pageSize = command != null && command.PageSize > 0 ? command.PageSize : 15;
            var items = _promotionRuleModelFactory.PrepareSelectProductList(searchModel, pageIndex, pageSize, out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        [HttpPost]
        [FormValueRequired("save")]
        [AdminAntiForgery]
        public ActionResult RequirementConditionSelectProductPopup([Bind(Prefix = "AddProductModel")] PromotionRuleConditionProductSelectorModel model)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var product = _productService.GetProductById(model.AssociatedToProductId);
            if (product == null)
                return Content("Cannot load a product");

            ViewBag.RefreshPage = true;
            ViewBag.ProductId = product.Id;
            ViewBag.ProductName = product.Name;

            var searchModel = _promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel());
            return View(RequirementViewPath + "RequirementConditionSelectProductPopup.cshtml", searchModel);
        }

        public ActionResult RequirementConditionCreatePopup(int discountRequirementId, int? conditionGroup = null)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var discountRequirement = FindDiscountRequirement(discountRequirementId);
            if (discountRequirement == null)
                return Content("Save the requirement first.");

            var existingGroups = GetConditionGroups(discountRequirementId, conditionGroup);
            var defaultGroup = conditionGroup.HasValue && conditionGroup.Value > 0
                ? conditionGroup.Value
                : existingGroups.Last();

            var model = PrepareRequirementConditionModel(
                new PromotionRuleConditionModel
                {
                    DiscountRequirementId = discountRequirementId,
                    ConditionGroup = defaultGroup,
                    LogicalOperatorId = (int)ConditionLogicalOperator.And
                }, null);

            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;
            return View(RequirementViewPath + "RequirementConditionCreatePopup.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RequirementConditionCreatePopup(PromotionRuleConditionModel model)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var discountRequirement = FindDiscountRequirement(model.DiscountRequirementId);
            if (discountRequirement == null)
                return Content("Save the requirement first.");

            if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
                model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
            if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
                model.ParentConditionId = null;

            MapConditionSourceBuilderToStorage(model);
            MapConditionSelectionsToStorage(model);
            NormalizeConditionModelForRequirement(model);
            ValidateConditionSource(model);

            if (model.ConditionGroup <= 0)
                ModelState.AddModelError("ConditionGroup", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

            ValidateConditionParent(model, null);

            if (!HasAnyDiscountRequirementCriteria(model))
                ModelState.AddModelError(string.Empty, _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));

            PrepareConditionSelectedNames(model);

            if (!ModelState.IsValid)
            {
                var existingGroups = GetConditionGroups(model.DiscountRequirementId, model.ConditionGroup);
                ViewBag.ExistingConditionGroups = existingGroups;
                ViewBag.NextConditionGroup = existingGroups.Max() + 1;

                return View(RequirementViewPath + "RequirementConditionCreatePopup.cshtml",
                    PrepareRequirementConditionModel(model, null));
            }

            var condition = model.ToEntity();
            condition.PromotionRuleId = 0;
            _promotionRuleService.InsertRuleCondition(condition);

            ViewBag.RefreshPage = true;
            return View(RequirementViewPath + "RequirementConditionCreatePopup.cshtml", model);
        }

        public ActionResult RequirementConditionEditPopup(int id)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var condition = _promotionRuleService.GetRuleConditionById(id);
            if (condition == null || condition.DiscountRequirementId <= 0)
                return Content("Condition could not be loaded.");

            var model = PrepareRequirementConditionModel(null, condition);
            var existingGroups = GetConditionGroups(condition.DiscountRequirementId, condition.ConditionGroup);
            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;

            return View(RequirementViewPath + "RequirementConditionCreatePopup.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RequirementConditionEditPopup(PromotionRuleConditionModel model)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var condition = _promotionRuleService.GetRuleConditionById(model.Id);
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
            ValidateConditionSource(model);

            if (model.ConditionGroup <= 0)
                ModelState.AddModelError("ConditionGroup", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

            ValidateConditionParent(model, condition);

            if (!HasAnyDiscountRequirementCriteria(model))
                ModelState.AddModelError(string.Empty, _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));

            PrepareConditionSelectedNames(model);

            if (!ModelState.IsValid)
            {
                var existingGroups = GetConditionGroups(model.DiscountRequirementId, model.ConditionGroup);
                ViewBag.ExistingConditionGroups = existingGroups;
                ViewBag.NextConditionGroup = existingGroups.Max() + 1;
                return View(RequirementViewPath + "RequirementConditionCreatePopup.cshtml",
                    PrepareRequirementConditionModel(model, condition));
            }

            condition = model.ToEntity(condition);
            condition.PromotionRuleId = 0;
            _promotionRuleService.UpdateRuleCondition(condition);

            ViewBag.RefreshPage = true;
            return View(RequirementViewPath + "RequirementConditionCreatePopup.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RequirementConditionDelete(int id)
        {
            if (!CanManage())
                return new HttpUnauthorizedResult();

            var condition = _promotionRuleService.GetRuleConditionById(id);
            if (condition != null && condition.DiscountRequirementId > 0)
                DeleteConditionWithChildren(condition, condition.DiscountRequirementId);

            return new NullJsonResult();
        }

        private PromotionRuleConditionModel PrepareRequirementConditionModel(
            PromotionRuleConditionModel model,
            PromotionRuleCondition condition)
        {
            var preparedModel = _promotionRuleModelFactory.PrepareRuleConditionModel(model, condition);

            if (!preparedModel.AvailableConditionOperators.Any(x => string.IsNullOrEmpty(x.Value)))
            {
                preparedModel.AvailableConditionOperators.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = _localizationService.GetResource("Admin.Common.Select")
                });
            }

            return preparedModel;
        }

        private DiscountRequirement FindDiscountRequirement(int discountRequirementId)
        {
            if (discountRequirementId <= 0)
                return null;

            foreach (var discount in _discountService.GetAllDiscounts(null, showHidden: true))
            {
                var requirement = discount.DiscountRequirements.FirstOrDefault(x => x.Id == discountRequirementId);
                if (requirement != null)
                    return requirement;
            }

            return null;
        }

        private List<int> GetConditionGroups(int discountRequirementId, int? includeGroup)
        {
            var existingConditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
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
            var builderState = ConditionSourceBuilderHelper.ParseBuilderStateJson(model != null ? model.ConditionSourceBuilderJson : null);
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
            model.SourceBuilderRows = builderState.Rows != null ? builderState.Rows.ToList() : new List<ConditionSourceBuilderRowModel>();
            model.SelectedSourceTokens = builderState.Tokens != null ? builderState.Tokens.ToList() : new List<string>();
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
            model.RequiredPaymentMethodsCsv = BuildCsv(model.SelectedPaymentMethodSystemNames, false);
            model.RequiredCouponCodesCsv = null;
        }

        private static string BuildCsv(IEnumerable<string> values, bool upperCase)
        {
            var items = (values ?? new string[0])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => upperCase ? x.Trim().ToUpperInvariant() : x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return items.Any() ? string.Join(",", items) : string.Empty;
        }

        private void ValidateConditionSource(PromotionRuleConditionModel model)
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
                ModelState.AddModelError("ConditionSourceTypeId",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType.Invalid"));
            }

            if (model.ConditionSourceTypeId > 0 && string.IsNullOrWhiteSpace(model.ConditionSourceData) && !hasBuilderValues)
            {
                ModelState.AddModelError("ConditionSourceData",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData.Required"));
            }
            else if (model.ConditionSourceTypeId > 0 &&
                     (model.ConditionSourceBuilderTouched || hasBuilderValues))
            {
                foreach (var row in builderState.Rows ?? new ConditionSourceBuilderRowModel[0])
                {
                    if (model.ConditionSourceTypeId != (int)ConditionSourceType.ExpiryDays &&
                        (!row.EntryId.HasValue || row.EntryId.Value <= 0))
                    {
                        ModelState.AddModelError("ConditionSourceData",
                            _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Entry.Required"));
                        break;
                    }

                    if ((model.ConditionSourceTypeId == (int)ConditionSourceType.SpecificationAttributeOptions ||
                         model.ConditionSourceTypeId == (int)ConditionSourceType.ProductAttributeValues) &&
                        (!row.EntryId.HasValue || row.EntryId.Value <= 0))
                    {
                        ModelState.AddModelError("ConditionSourceData",
                            _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Attribute.Required"));
                        break;
                    }

                    if ((row.RangeMin.HasValue && row.RangeMin.Value < 0) ||
                        (row.RangeMax.HasValue && row.RangeMax.Value < 0))
                    {
                        ModelState.AddModelError("ConditionSourceData",
                            _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Range.Invalid"));
                        break;
                    }

                    if (row.RangeMin.HasValue && row.RangeMax.HasValue && row.RangeMax.Value < row.RangeMin.Value)
                    {
                        ModelState.AddModelError("ConditionSourceData",
                            _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Range.Invalid"));
                        break;
                    }
                }
            }

            if (model.QuantityMin.HasValue && model.QuantityMin < 0)
                ModelState.AddModelError("QuantityMin",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin.Invalid"));

            if (model.QuantityMax.HasValue && model.QuantityMax < 0)
                ModelState.AddModelError("QuantityMax",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax.Invalid"));

            if (model.QuantityMin.HasValue && model.QuantityMax.HasValue && model.QuantityMax < model.QuantityMin)
                ModelState.AddModelError("QuantityMax",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityRange.Invalid"));

            if (model.RequiredOrderCountMin.HasValue && model.RequiredOrderCountMin.Value < 0)
                ModelState.AddModelError("RequiredOrderCountMin",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin.Invalid"));

            if (model.RequiredOrderCountMax.HasValue && model.RequiredOrderCountMax.Value < 0)
                ModelState.AddModelError("RequiredOrderCountMax",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax.Invalid"));

            if (model.RequiredOrderCountMin.HasValue &&
                model.RequiredOrderCountMax.HasValue &&
                model.RequiredOrderCountMax.Value > 0 &&
                model.RequiredOrderCountMax.Value < model.RequiredOrderCountMin.Value)
            {
                ModelState.AddModelError("RequiredOrderCountMax",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountRange.Invalid"));
            }

            if (!Enum.IsDefined(typeof(AttributeMatchMode), model.AttributeMatchModeId))
                model.AttributeMatchModeId = (int)AttributeMatchMode.Any;
        }

        private void ValidateConditionParent(PromotionRuleConditionModel model, PromotionRuleCondition existingCondition)
        {
            if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
            {
                model.ParentConditionId = null;
                return;
            }

            var parentCondition = _promotionRuleService.GetRuleConditionById(model.ParentConditionId.Value);
            if (parentCondition == null || parentCondition.DiscountRequirementId != model.DiscountRequirementId)
            {
                ModelState.AddModelError("ParentConditionId",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.Invalid"));
                return;
            }

            var parentGroup = parentCondition.ConditionGroup <= 0 ? 1 : parentCondition.ConditionGroup;
            var currentGroup = model.ConditionGroup <= 0 ? 1 : model.ConditionGroup;
            if (parentGroup != currentGroup)
            {
                ModelState.AddModelError("ParentConditionId",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.GroupMismatch"));
            }

            if (existingCondition == null)
                return;

            if (parentCondition.Id == existingCondition.Id)
            {
                ModelState.AddModelError("ParentConditionId",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.SelfReference"));
                return;
            }

            var conditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(model.DiscountRequirementId);
            var childrenByParent = conditions
                .Where(x => x.ParentConditionId.HasValue && x.ParentConditionId.Value > 0)
                .GroupBy(x => x.ParentConditionId.Value)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToList());

            if (IsDescendantCondition(existingCondition.Id, parentCondition.Id, childrenByParent))
            {
                ModelState.AddModelError("ParentConditionId",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.CyclicReference"));
            }
        }

        private static bool IsDescendantCondition(int rootConditionId, int targetConditionId, Dictionary<int, List<int>> childrenByParent)
        {
            List<int> childIds;
            if (!childrenByParent.TryGetValue(rootConditionId, out childIds))
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

                List<int> nestedChildren;
                if (childrenByParent.TryGetValue(current, out nestedChildren))
                {
                    foreach (var child in nestedChildren)
                        stack.Push(child);
                }
            }

            return false;
        }

        private void DeleteConditionWithChildren(PromotionRuleCondition condition, int discountRequirementId)
        {
            var allConditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
            var childrenByParent = allConditions
                .Where(x => x.ParentConditionId.HasValue && x.ParentConditionId.Value > 0)
                .GroupBy(x => x.ParentConditionId.Value)
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
                List<int> childIds;
                if (childrenByParent.TryGetValue(currentId, out childIds))
                {
                    foreach (var childId in childIds)
                        stack.Push(childId);
                }
            }

            foreach (var conditionId in toDelete.OrderByDescending(x => x))
            {
                var conditionToDelete = allConditions.FirstOrDefault(x => x.Id == conditionId)
                                        ?? _promotionRuleService.GetRuleConditionById(conditionId);
                if (conditionToDelete != null)
                    _promotionRuleService.DeleteRuleCondition(conditionToDelete);
            }
        }

        private void PrepareConditionSelectedNames(PromotionRuleConditionModel model)
        {
            if (model.RequiredProductId.HasValue && model.RequiredProductId.Value > 0)
            {
                var product = _productService.GetProductById(model.RequiredProductId.Value);
                model.RequiredProductName = product != null ? product.Name : string.Empty;
            }
            else
            {
                model.RequiredProductName = string.Empty;
            }

            if (model.ExcludedProductId.HasValue && model.ExcludedProductId.Value > 0)
            {
                var product = _productService.GetProductById(model.ExcludedProductId.Value);
                model.ExcludedProductName = product != null ? product.Name : string.Empty;
            }
            else
            {
                model.ExcludedProductName = string.Empty;
            }

            if (model.RequiredCategoryId.HasValue && model.RequiredCategoryId.Value > 0)
            {
                var category = _categoryService.GetCategoryById(model.RequiredCategoryId.Value);
                model.RequiredCategoryName = category != null ? category.Name : string.Empty;
            }
            else
            {
                model.RequiredCategoryName = string.Empty;
            }

            if (model.RequiredVendorId.HasValue && model.RequiredVendorId.Value > 0)
            {
                var vendor = _vendorService.GetVendorById(model.RequiredVendorId.Value);
                model.RequiredVendorName = vendor != null ? vendor.Name : string.Empty;
            }
            else
            {
                model.RequiredVendorName = string.Empty;
            }

            if (model.RequiredCustomerRoleId.HasValue && model.RequiredCustomerRoleId.Value > 0)
            {
                var role = _customerService.GetAllCustomerRoles(true)
                    .FirstOrDefault(x => x.Id == model.RequiredCustomerRoleId.Value);
                model.RequiredCustomerRoleName = role != null ? role.Name : string.Empty;
            }
            else
            {
                model.RequiredCustomerRoleName = string.Empty;
            }

            if (model.ParentConditionId.HasValue && model.ParentConditionId.Value > 0)
            {
                var parentCondition = _promotionRuleService.GetRuleConditionById(model.ParentConditionId.Value);
                model.ParentConditionName = parentCondition != null ? "#" + parentCondition.Id : string.Empty;
            }
            else
            {
                model.ParentConditionName = string.Empty;
            }
        }
    }
}
