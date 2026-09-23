using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Infrastructure;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Controllers
{
    [AdminAuthorize]
    public class PromotionRuleController : BasePluginController
    {
        private const string ViewRoot = "~/Plugins/Misc.DiscountManagerPlus/Views/PromotionRule/";

        private ViewResult PluginView(string viewName, object model)
        {
            return View(ViewRoot + viewName + ".cshtml", model);
        }

        #region Fields

        private readonly IDiscountManagerPlusService _discountManagerPlusService;
        private readonly IDiscountService _discountService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICategoryService _categoryService;
        private readonly ICustomerService _customerService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;
        private readonly IPromotionRuleModelFactory _promotionRuleModelFactory;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IVendorService _vendorService;
        private readonly IPromotionRuleExcludedProductService _excludedProductService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PromotionRuleController(
            IDiscountManagerPlusService discountManagerPlusService,
            IDiscountService discountService,
            ILocalizationService localizationService,
            ILogger logger,
            IManufacturerService manufacturerService,
            ICategoryService categoryService,
            ICustomerService customerService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IDiscountManagerPlusRequirementService discountManagerPlusRequirementService,
            IPromotionRuleModelFactory promotionRuleModelFactory,
            IPromotionRuleService promotionRuleService,
            IStoreMappingService storeMappingService,
            IVendorService vendorService,
            IPromotionRuleExcludedProductService excludedProductService,
            IPermissionService permissionService,
            IWorkContext workContext)
        {
            _discountManagerPlusService = discountManagerPlusService;
            _discountService = discountService;
            _localizationService = localizationService;
            _logger = logger;
            _manufacturerService = manufacturerService;
            _categoryService = categoryService;
            _customerService = customerService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
            _promotionRuleModelFactory = promotionRuleModelFactory;
            _promotionRuleService = promotionRuleService;
            _storeMappingService = storeMappingService;
            _vendorService = vendorService;
            _excludedProductService = excludedProductService;
            _permissionService = permissionService;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        private bool CanManageRules()
        {
            return _permissionService.Authorize(DiscountManagerPlusPermissionProvider.ManagePromotionRules)
                || _permissionService.Authorize(StandardPermissionProvider.ManageDiscounts);
        }

        private static int GetPageIndex(DataSourceRequest command)
        {
            return command != null && command.Page > 0 ? command.Page - 1 : 0;
        }

        private static int GetPageSize(DataSourceRequest command)
        {
            return command != null && command.PageSize > 0 ? command.PageSize : 15;
        }

        #endregion

        #region Methods

        public ActionResult List()
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var searchModel = _promotionRuleModelFactory.PreparePromotionRuleSearchModel(new PromotionRuleSearchModel());
            return PluginView("List", searchModel);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult List(DataSourceRequest command, PromotionRuleSearchModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var items = _promotionRuleModelFactory.PreparePromotionRuleList(model, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        public ActionResult Create()
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var model = _promotionRuleModelFactory.PreparePromotionRuleModel(new PromotionRuleModel(), null);
            return PluginView("Create", model);
        }

        [HttpPost]
        [ParameterBasedOnFormName("save-continue", "continueEditing")]
        [AdminAntiForgery]
        public ActionResult Create(PromotionRuleModel model, bool continueEditing)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            NormalizeRuleIdentity(model);
            ValidateParentDiscount(model);

            if (ModelState.IsValid)
            {
                NormalizeDiscountConfiguration(model);
                ValidateRuleSetupForActivation(model);
            }

            if (!ModelState.IsValid)
                return PluginView("Create", _promotionRuleModelFactory.PreparePromotionRuleModel(model, null));

            var rule = model.ToEntity();
            rule.DiscountId = ResolveParentDiscountId(model);
            rule.IsCumulativeWithDefaultDiscounts = false;
            ApplyStoreScopeState(rule, model);
            _promotionRuleService.InsertPromotionRule(rule);
            SaveStoreMappings(rule, model);
            SyncLinkedDiscountRequirements(null, GetParentDiscountId(rule));

            SuccessNotification(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Added"));

            if (continueEditing)
                return RedirectToAction("Edit", new { id = rule.Id });

            return RedirectToAction("List");
        }

        public ActionResult Edit(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(id);
            if (rule == null)
                return RedirectToAction("List");

            var model = _promotionRuleModelFactory.PreparePromotionRuleModel(null, rule);
            return PluginView("Edit", model);
        }

        [HttpPost]
        [ParameterBasedOnFormName("save-continue", "continueEditing")]
        [AdminAntiForgery]
        public ActionResult Edit(PromotionRuleModel model, bool continueEditing)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(model.Id);
            if (rule == null)
                return RedirectToAction("List");

            var previousLinkedDiscountId = GetParentDiscountId(rule);
            NormalizeRuleIdentity(model);
            ValidateParentDiscount(model);

            if (ModelState.IsValid)
            {
                NormalizeDiscountConfiguration(model);
                ValidateRuleSetupForActivation(model);
            }

            if (!ModelState.IsValid)
                return PluginView("Edit", _promotionRuleModelFactory.PreparePromotionRuleModel(model, rule));

            rule = model.ToEntity(rule);
            rule.DiscountId = ResolveParentDiscountId(model);
            rule.IsCumulativeWithDefaultDiscounts = false;
            ApplyStoreScopeState(rule, model);
            _promotionRuleService.UpdatePromotionRule(rule);
            SaveStoreMappings(rule, model);
            SyncLinkedDiscountRequirements(previousLinkedDiscountId, GetParentDiscountId(rule));

            SuccessNotification(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Updated"));

            if (continueEditing)
                return RedirectToAction("Edit", new { id = rule.Id });

            return RedirectToAction("List");
        }

        public ActionResult CreateParentDiscountRulePopup(int discountId)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                return RedirectToAction("List");

            var model = _promotionRuleModelFactory.PreparePromotionRuleModel(new PromotionRuleModel
            {
                DiscountId = discountId,
                IsDiscountBound = true,
                ParentDiscountName = discount.Name + " (#" + discount.Id + ")",
                IsActive = true
            }, null);

            return PluginView("ParentDiscountRulePopup", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult CreateParentDiscountRulePopup(PromotionRuleModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            model.IsDiscountBound = true;
            NormalizeRuleIdentity(model);
            ValidateParentDiscount(model);

            if (ModelState.IsValid)
                NormalizeDiscountConfiguration(model);

            if (!ModelState.IsValid)
                return PluginView("ParentDiscountRulePopup", PrepareDiscountBoundPopupModel(model, null));

            var rule = model.ToEntity();
            rule.DiscountId = ResolveParentDiscountId(model);
            rule.IsCumulativeWithDefaultDiscounts = false;
            ApplyStoreScopeState(rule, model);
            _promotionRuleService.InsertPromotionRule(rule);
            SaveStoreMappings(rule, model);
            SyncLinkedDiscountRequirements(null, GetParentDiscountId(rule));

            ViewBag.RefreshPage = true;
            return PluginView("ParentDiscountRulePopup", PrepareDiscountBoundPopupModel(model, rule));
        }

        public ActionResult EditParentDiscountRulePopup(int id, int discountId = 0)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(id);
            if (rule == null)
                return RedirectToAction("List");

            var model = _promotionRuleModelFactory.PreparePromotionRuleModel(null, rule);
            if (model.DiscountId <= 0 && discountId > 0)
                model.DiscountId = discountId;
            model.IsDiscountBound = true;
            return PluginView("ParentDiscountRulePopup", PrepareDiscountBoundPopupModel(model, rule));
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult EditParentDiscountRulePopup(PromotionRuleModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(model.Id);
            if (rule == null)
                return RedirectToAction("List");

            model.IsDiscountBound = true;
            var previousLinkedDiscountId = GetParentDiscountId(rule);
            NormalizeRuleIdentity(model);
            ValidateParentDiscount(model);

            if (ModelState.IsValid)
                NormalizeDiscountConfiguration(model);

            if (!ModelState.IsValid)
                return PluginView("ParentDiscountRulePopup", PrepareDiscountBoundPopupModel(model, rule));

            rule = model.ToEntity(rule);
            rule.DiscountId = ResolveParentDiscountId(model);
            rule.IsCumulativeWithDefaultDiscounts = false;
            ApplyStoreScopeState(rule, model);
            _promotionRuleService.UpdatePromotionRule(rule);
            SaveStoreMappings(rule, model);
            SyncLinkedDiscountRequirements(previousLinkedDiscountId, GetParentDiscountId(rule));

            ViewBag.RefreshPage = true;
            return PluginView("ParentDiscountRulePopup", PrepareDiscountBoundPopupModel(model, rule));
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult DeleteParentDiscountRule(int id, int discountId)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(id);
            if (rule != null)
            {
                var parentDiscountId = GetParentDiscountId(rule);

                foreach (var ruleProduct in _promotionRuleService.GetRuleProductsByRuleId(rule.Id))
                    _promotionRuleService.DeleteRuleProduct(ruleProduct);

                foreach (var ruleTier in _promotionRuleService.GetRuleTiersByRuleId(rule.Id))
                    _promotionRuleService.DeleteRuleTier(ruleTier);

                foreach (var ruleCondition in _promotionRuleService.GetRuleConditionsByRuleId(rule.Id))
                    _promotionRuleService.DeleteRuleCondition(ruleCondition);

                _promotionRuleService.DeletePromotionRule(rule);
                SyncLinkedDiscountRequirements(parentDiscountId > 0 ? parentDiscountId : discountId, null);
            }

            return new NullJsonResult();
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult ParentDiscountRuleList(DataSourceRequest command, PromotionRuleSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var items = _promotionRuleModelFactory.PreparePromotionRuleList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Delete(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(id);
            if (rule == null)
                return RedirectToAction("List");

            var linkedDiscountId = GetParentDiscountId(rule);

            foreach (var ruleProduct in _promotionRuleService.GetRuleProductsByRuleId(rule.Id))
                _promotionRuleService.DeleteRuleProduct(ruleProduct);

            foreach (var ruleTier in _promotionRuleService.GetRuleTiersByRuleId(rule.Id))
                _promotionRuleService.DeleteRuleTier(ruleTier);

            foreach (var ruleCondition in _promotionRuleService.GetRuleConditionsByRuleId(rule.Id))
                _promotionRuleService.DeleteRuleCondition(ruleCondition);

            _promotionRuleService.DeletePromotionRule(rule);
            SyncLinkedDiscountRequirements(linkedDiscountId, null);

            SuccessNotification(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Deleted"));

            return RedirectToAction("List");
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductList(DataSourceRequest command, PromotionRuleProductSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(searchModel.PromotionRuleId);
            if (rule == null)
                return Json(new DataSourceResult { Data = new object[0], Total = 0 });

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareRuleProductList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        public ActionResult RuleProductAddPopup(int promotionRuleId)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            var model = new PromotionRuleProductAddModel
            {
                PromotionRuleId = promotionRuleId,
                MinQuantity = 1
            };

            PrepareRuleProductSourceTypes(model);
            PrepareRuleProductSourceLists(model);
            PrepareRewardAttributeSelectionTypes(model);
            ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
            return View(model);
        }

        public ActionResult RuleProductAddMultiplePopup(int promotionRuleId)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            var model = _promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel());
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductAddMultiplePopupList(DataSourceRequest command, PromotionRuleSelectProductSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareSelectProductList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        [HttpPost]
        [FormValueRequired("save")]
        [AdminAntiForgery]
        public ActionResult RuleProductAddMultiplePopup(AddProductsToPromotionRuleModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(model.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            var selectedProductIds = (model.SelectedProductIds ?? new int[0])
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (selectedProductIds.Any())
            {
                var existingRuleProducts = _promotionRuleService.GetRuleProductsByRuleId(model.PromotionRuleId);
                var existingProductIds = new HashSet<int>(existingRuleProducts
                    .Where(x => !x.IsAllProducts && x.ProductId > 0 && !x.IsRewardProduct)
                    .Select(x => x.ProductId));

                foreach (var productId in selectedProductIds)
                {
                    if (existingProductIds.Contains(productId))
                        continue;

                    _promotionRuleService.InsertRuleProduct(new PromotionRuleProduct
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
            return View(_promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel()));
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductAddPopup(PromotionRuleProductAddModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(model.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            ValidateRuleProductSource(model);

            if (model.MaxQuantity > 0 && model.MaxQuantity < model.MinQuantity)
                ModelState.AddModelError("MaxQuantity", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity.Invalid"));

            if (model.SourceTypeId == (int)RuleProductSourceType.Product &&
                model.RewardAttributeSelectionTypeId == (int)RewardAttributeSelectionType.SpecificValues &&
                string.IsNullOrWhiteSpace(model.RewardAttributeValueIds))
            {
                ModelState.AddModelError("RewardAttributeValueIds",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Required"));
            }

            if (!ModelState.IsValid)
            {
                PrepareRuleProductSelectedProductName(model);
                PrepareRuleProductSourceTypes(model);
                PrepareRuleProductSourceLists(model);
                PrepareRewardAttributeSelectionTypes(model);
                ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
                return View(model);
            }

            NormalizeRuleProductSource(model);

            var isRewardProduct = IsRewardProductAllowed(rule) && model.IsRewardProduct;

            _promotionRuleService.InsertRuleProduct(new PromotionRuleProduct
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
            });

            ViewBag.RefreshPage = true;
            return View(model);
        }

        public ActionResult RuleProductEditPopup(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var ruleProduct = _promotionRuleService.GetRuleProductById(id);
            if (ruleProduct == null)
                return RedirectToAction("List");

            var rule = _promotionRuleService.GetPromotionRuleById(ruleProduct.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

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
            PrepareRuleProductSourceTypes(model);
            PrepareRuleProductSourceLists(model);
            PrepareRewardAttributeSelectionTypes(model);
            PrepareRuleProductSelectedProductName(model);

            ViewBag.AllowRewardProduct = IsRewardProductAllowed(rule);
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductEditPopup(PromotionRuleProductAddModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var ruleProduct = _promotionRuleService.GetRuleProductById(model.Id);
            if (ruleProduct == null)
                return RedirectToAction("List");

            var rule = _promotionRuleService.GetPromotionRuleById(ruleProduct.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            ValidateRuleProductSource(model);

            if (model.MaxQuantity > 0 && model.MaxQuantity < model.MinQuantity)
                ModelState.AddModelError("MaxQuantity", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity.Invalid"));

            if (model.SourceTypeId == (int)RuleProductSourceType.Product &&
                model.RewardAttributeSelectionTypeId == (int)RewardAttributeSelectionType.SpecificValues &&
                string.IsNullOrWhiteSpace(model.RewardAttributeValueIds))
            {
                ModelState.AddModelError("RewardAttributeValueIds",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Required"));
            }

            if (!ModelState.IsValid)
            {
                PrepareRuleProductSourceTypes(model);
                PrepareRuleProductSourceLists(model);
                PrepareRewardAttributeSelectionTypes(model);
                PrepareRuleProductSelectedProductName(model);
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

            _promotionRuleService.UpdateRuleProduct(ruleProduct);

            ViewBag.RefreshPage = true;
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductAdd(int promotionRuleId, int productId, int minQuantity, int maxQuantity = 0, bool isRewardProduct = false)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            if (rule == null)
                return Json(new { Result = false });

            _promotionRuleService.InsertRuleProduct(new PromotionRuleProduct
            {
                PromotionRuleId = promotionRuleId,
                ProductId = productId,
                MinQuantity = minQuantity > 0 ? minQuantity : 1,
                MaxQuantity = maxQuantity > 0 ? maxQuantity : 0,
                IsRewardProduct = IsRewardProductAllowed(rule) && isRewardProduct
            });
            return Json(new { Result = true });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductUpdate(int id, int productId, int minQuantity, int maxQuantity = 0, bool isRewardProduct = false)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var ruleProduct = _promotionRuleService.GetRuleProductById(id);
            if (ruleProduct == null)
                return Json(new { Result = false });

            var rule = _promotionRuleService.GetPromotionRuleById(ruleProduct.PromotionRuleId);
            if (rule == null)
                return Json(new { Result = false });

            if (productId > 0)
                ruleProduct.ProductId = productId;

            ruleProduct.MinQuantity = minQuantity > 0 ? minQuantity : 1;
            ruleProduct.MaxQuantity = maxQuantity > 0 ? maxQuantity : 0;
            ruleProduct.IsRewardProduct = IsRewardProductAllowed(rule) && isRewardProduct;

            _promotionRuleService.UpdateRuleProduct(ruleProduct);
            return Json(new { Result = true });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleProductDelete(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var ruleProduct = _promotionRuleService.GetRuleProductById(id);
            if (ruleProduct != null)
                _promotionRuleService.DeleteRuleProduct(ruleProduct);

            return new NullJsonResult();
        }

        #region Excluded Products

        [HttpPost]
        [AdminAntiForgery]
        public virtual ActionResult ExcludedProductList(DataSourceRequest command, PromotionRuleExcludedProductSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareExcludedProductList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        public virtual ActionResult ExcludedProductAddMultiplePopup(int promotionRuleId)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var model = _promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel());
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public virtual ActionResult ExcludedProductAddMultiplePopupList(DataSourceRequest command, PromotionRuleSelectProductSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareSelectProductList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        [HttpPost]
        [FormValueRequired("save")]
        [AdminAntiForgery]
        public virtual ActionResult ExcludedProductAddMultiplePopup(AddExcludedProductsToPromotionRuleModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            if (model.SelectedProductIds == null || !model.SelectedProductIds.Any())
                return Json(new { Error = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectProducts") });

            _excludedProductService.AddExcludedProducts(model.PromotionRuleId, model.SelectedProductIds);

            SuccessNotification(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Added"));

            ViewBag.RefreshPage = true;
            return View(_promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel()));
        }

        [HttpPost]
        [AdminAntiForgery]
        public virtual ActionResult ExcludedProductDelete(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var excludedProduct = _excludedProductService.GetExcludedProductById(id);
            if (excludedProduct != null)
                _excludedProductService.DeleteExcludedProduct(excludedProduct);

            return new NullJsonResult();
        }

        [HttpPost]
        [AdminAntiForgery]
        public virtual ActionResult ExcludedProductBulkDelete(IList<int> selectedIds)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            if (selectedIds == null || !selectedIds.Any())
                return Json(new { Error = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectExcludedProducts") });

            var excludedProducts = new List<PromotionRuleExcludedProduct>();
            foreach (var id in selectedIds)
            {
                var excludedProduct = _excludedProductService.GetExcludedProductById(id);
                if (excludedProduct != null)
                    excludedProducts.Add(excludedProduct);
            }

            foreach (var excludedProduct in excludedProducts)
                _excludedProductService.DeleteExcludedProduct(excludedProduct);

            SuccessNotification(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Deleted"));
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

        private void NormalizeRuleIdentity(PromotionRuleModel model)
        {
            if (model == null)
                return;

            if (string.IsNullOrWhiteSpace(model.Name) && model.DiscountId > 0)
            {
                var discount = _discountService.GetDiscountById(model.DiscountId);
                if (discount != null && !string.IsNullOrWhiteSpace(discount.Name))
                    model.Name = discount.Name;
            }

            if (string.IsNullOrWhiteSpace(model.SystemName) && !string.IsNullOrWhiteSpace(model.Name))
                model.SystemName = GenerateSystemName(model.Name);

            if (!string.IsNullOrWhiteSpace(model.Name))
                ModelState.Remove("Name");
            if (!string.IsNullOrWhiteSpace(model.SystemName))
                ModelState.Remove("SystemName");
        }

        private static string GenerateSystemName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var generated = Regex.Replace(name.Trim(), @"[^A-Za-z0-9]+", ".");
            generated = generated.Trim('.');
            if (generated.Length > 400)
                generated = generated.Substring(0, 400).Trim('.');

            return generated;
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
            if (rule == null)
                throw new ArgumentNullException("rule");
            if (model == null)
                throw new ArgumentNullException("model");

            var selectedStoreIds = (model.SelectedStoreIds ?? new int[0])
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            rule.LimitedToStores = selectedStoreIds.Any();
            rule.LimitedToStore = selectedStoreIds.Count == 1 ? selectedStoreIds[0] : 0;
        }

        private void SaveStoreMappings(PromotionRule rule, PromotionRuleModel model)
        {
            if (rule == null)
                throw new ArgumentNullException("rule");
            if (model == null)
                throw new ArgumentNullException("model");

            var selectedStoreIds = (model.SelectedStoreIds ?? new int[0])
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            var existingMappings = _storeMappingService.GetStoreMappings(rule).ToList();
            foreach (var mapping in existingMappings)
            {
                if (!selectedStoreIds.Contains(mapping.StoreId))
                    _storeMappingService.DeleteStoreMapping(mapping);
            }

            foreach (var storeId in selectedStoreIds)
            {
                if (!existingMappings.Any(x => x.StoreId == storeId))
                    _storeMappingService.InsertStoreMapping(rule, storeId);
            }
        }

        private void SyncLinkedDiscountRequirements(int? previousLinkedDiscountId, int? currentLinkedDiscountId)
        {
            var linkedDiscountIds = new[] { previousLinkedDiscountId, currentLinkedDiscountId }
                .Where(x => x.HasValue && x.Value > 0)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            foreach (var linkedDiscountId in linkedDiscountIds)
                _discountManagerPlusRequirementService.SyncLinkedDiscountRequirements(linkedDiscountId);
        }

        private void ValidateParentDiscount(PromotionRuleModel model)
        {
            var parentDiscountId = ResolveParentDiscountId(model);
            if (parentDiscountId <= 0)
                return;

            var discount = _discountService.GetDiscountById(parentDiscountId);
            if (discount != null)
                return;

            ModelState.AddModelError(
                model.DiscountId > 0 ? "DiscountId" : "LinkedDiscountId",
                _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LinkedDiscount.Invalid"));
        }

        private void ValidateRuleSetupForActivation(PromotionRuleModel model)
        {
            if (model == null || model.DiscountId > 0 || (!model.IsActive && model.DiscountId <= 0))
                return;

            var ruleType = (PromotionRuleType)model.RuleTypeId;
            var isExistingRule = model.Id > 0;

            var ruleProducts = isExistingRule
                ? _promotionRuleService.GetRuleProductsByRuleId(model.Id)
                : new List<PromotionRuleProduct>();

            var ruleTiers = isExistingRule
                ? _promotionRuleService.GetRuleTiersByRuleId(model.Id)
                : new List<PromotionRuleTier>();

            var ruleConditions = isExistingRule
                ? _promotionRuleService.GetRuleConditionsByRuleId(model.Id)
                : new List<PromotionRuleCondition>();

            if (ruleType == PromotionRuleType.ProductBased || ruleType == PromotionRuleType.ComboPricing || ruleType == PromotionRuleType.BuyXGetY)
            {
                var hasBuyProducts = ruleType == PromotionRuleType.BuyXGetY
                    ? ruleProducts.Any(x => !x.IsRewardProduct)
                    : ruleProducts.Any();

                if (!hasBuyProducts)
                {
                    ModelState.AddModelError("IsActive",
                        _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.ProductsRequiredForActivation"));
                }
            }

            if (ruleType == PromotionRuleType.BuyXGetY && !ruleTiers.Any())
            {
                ModelState.AddModelError("IsActive",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.TiersRequiredForActivation"));
            }

            if ((ruleType == PromotionRuleType.CartCondition || ruleType == PromotionRuleType.SubtotalBased) && !ruleConditions.Any())
            {
                ModelState.AddModelError("IsActive",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.ConditionsRequiredForActivation"));
            }

            if (ruleType == PromotionRuleType.ProductBased && model.DiscountTypeId == (int)DiscountType.FreeItem)
            {
                var hasRewardProduct = ruleProducts.Any(x => x.IsRewardProduct && x.ProductId > 0);
                if (!hasRewardProduct)
                {
                    ModelState.AddModelError("IsActive",
                        _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.RewardProductRequiredForFreeItem"));
                }
            }
        }

        private PromotionRuleModel PrepareDiscountBoundPopupModel(PromotionRuleModel model, PromotionRule rule)
        {
            model = _promotionRuleModelFactory.PreparePromotionRuleModel(model, rule);
            model.IsDiscountBound = true;

            var parentDiscountId = ResolveParentDiscountId(model);
            if (parentDiscountId > 0)
            {
                model.DiscountId = parentDiscountId;
                var discount = _discountService.GetDiscountById(parentDiscountId);
                if (discount != null)
                    model.ParentDiscountName = discount.Name + " (#" + discount.Id + ")";
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

        private void PrepareRuleProductSourceTypes(PromotionRuleProductAddModel model)
        {
            model.AvailableSourceTypes = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = ((int)RuleProductSourceType.Product).ToString(),
                    Text = RuleProductSourceType.Product.GetLocalizedEnum(_localizationService, _workContext)
                },
                new SelectListItem
                {
                    Value = ((int)RuleProductSourceType.Category).ToString(),
                    Text = RuleProductSourceType.Category.GetLocalizedEnum(_localizationService, _workContext)
                },
                new SelectListItem
                {
                    Value = ((int)RuleProductSourceType.Manufacturer).ToString(),
                    Text = RuleProductSourceType.Manufacturer.GetLocalizedEnum(_localizationService, _workContext)
                },
                new SelectListItem
                {
                    Value = ((int)RuleProductSourceType.Vendor).ToString(),
                    Text = RuleProductSourceType.Vendor.GetLocalizedEnum(_localizationService, _workContext)
                }
            };
        }

        private void PrepareRuleProductSourceLists(PromotionRuleProductAddModel model)
        {
            PrepareCategorySelectList(model.AvailableCategories);
            PrepareManufacturerSelectList(model.AvailableManufacturers);
            PrepareVendorSelectList(model.AvailableVendors);
        }

        private void PrepareCategorySelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
        {
            items.Clear();
            if (includeEmptyItem)
            {
                items.Add(new SelectListItem
                {
                    Value = string.Empty,
                    Text = _localizationService.GetResource("Admin.Common.None")
                });
            }

            foreach (var category in _categoryService.GetAllCategories(showHidden: true))
            {
                items.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.Name
                });
            }
        }

        private void PrepareManufacturerSelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
        {
            items.Clear();
            if (includeEmptyItem)
            {
                items.Add(new SelectListItem
                {
                    Value = string.Empty,
                    Text = _localizationService.GetResource("Admin.Common.None")
                });
            }

            foreach (var manufacturer in _manufacturerService.GetAllManufacturers(showHidden: true))
            {
                items.Add(new SelectListItem
                {
                    Value = manufacturer.Id.ToString(),
                    Text = manufacturer.Name
                });
            }
        }

        private void PrepareVendorSelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
        {
            items.Clear();
            if (includeEmptyItem)
            {
                items.Add(new SelectListItem
                {
                    Value = string.Empty,
                    Text = _localizationService.GetResource("Admin.Common.None")
                });
            }

            foreach (var vendor in _vendorService.GetAllVendors(showHidden: true, pageIndex: 0, pageSize: 2000))
            {
                items.Add(new SelectListItem
                {
                    Value = vendor.Id.ToString(),
                    Text = vendor.Name
                });
            }
        }

        private void ValidateRuleProductSource(PromotionRuleProductAddModel model)
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
                        ModelState.AddModelError("ProductId", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId.Hint"));
                    break;
                case RuleProductSourceType.Category:
                    if (!model.CategoryId.HasValue || model.CategoryId.Value <= 0)
                        ModelState.AddModelError("CategoryId", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId.Hint"));
                    break;
                case RuleProductSourceType.Manufacturer:
                    if (!model.ManufacturerId.HasValue || model.ManufacturerId.Value <= 0)
                        ModelState.AddModelError("ManufacturerId", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId.Hint"));
                    break;
                case RuleProductSourceType.Vendor:
                    if (!model.VendorId.HasValue || model.VendorId.Value <= 0)
                        ModelState.AddModelError("VendorId", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId.Hint"));
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

        private void PrepareRewardAttributeSelectionTypes(PromotionRuleProductAddModel model)
        {
            model.AvailableRewardAttributeSelectionTypes = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = ((int)RewardAttributeSelectionType.Any).ToString(),
                    Text = RewardAttributeSelectionType.Any.GetLocalizedEnum(_localizationService, _workContext)
                },
                new SelectListItem
                {
                    Value = ((int)RewardAttributeSelectionType.SpecificValues).ToString(),
                    Text = RewardAttributeSelectionType.SpecificValues.GetLocalizedEnum(_localizationService, _workContext)
                }
            };
        }

        private void PrepareRuleProductSelectedProductName(PromotionRuleProductAddModel model)
        {
            if (model.ProductId <= 0)
            {
                model.ProductName = string.Empty;
                return;
            }

            var product = _productService.GetProductById(model.ProductId);
            model.ProductName = product != null ? product.Name : string.Empty;
        }

        private void PrepareTierRewardProductName(PromotionRuleTierModel model)
        {
            if (!model.RewardProductId.HasValue || model.RewardProductId.Value <= 0)
            {
                model.RewardProductName = string.Empty;
                return;
            }

            var product = _productService.GetProductById(model.RewardProductId.Value);
            model.RewardProductName = product != null ? product.Name : string.Empty;
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
        [AdminAntiForgery]
        public ActionResult RuleTierList(DataSourceRequest command, PromotionRuleTierSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(searchModel.PromotionRuleId);
            if (rule == null)
                return Json(new DataSourceResult { Data = new object[0], Total = 0 });

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareRuleTierList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        public ActionResult RuleTierCreatePopup(int promotionRuleId)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var model = _promotionRuleModelFactory.PrepareRuleTierModel(
                new PromotionRuleTierModel { PromotionRuleId = promotionRuleId }, null);
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleTierCreatePopup(PromotionRuleTierModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            model.SelectedRuleProductIds = model.SelectedRuleProductIds != null
                ? model.SelectedRuleProductIds.Where(x => x > 0).Distinct().ToList()
                : new List<int>();

            if (!SupportsTierRewardTargeting(model))
            {
                model.RewardProductId = null;
                model.AutoAddReward = false;
            }
            else if (model.DiscountTypeId != (int)DiscountType.FreeItem)
                model.AutoAddReward = false;

            if (!ModelState.IsValid)
            {
                model = _promotionRuleModelFactory.PrepareRuleTierModel(model, null);
                PrepareTierRewardProductName(model);
                return View(model);
            }

            var tier = model.ToEntity();
            _promotionRuleService.InsertRuleTier(tier);
            _promotionRuleService.SaveRuleTierProductMappings(model.PromotionRuleId, tier.Id, model.SelectedRuleProductIds);

            ViewBag.RefreshPage = true;
            return View(model);
        }

        public ActionResult RuleTierEditPopup(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var tier = _promotionRuleService.GetRuleTierById(id);
            if (tier == null)
                return RedirectToAction("List");

            var model = _promotionRuleModelFactory.PrepareRuleTierModel(null, tier);
            PrepareTierRewardProductName(model);
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleTierEditPopup(PromotionRuleTierModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var tier = _promotionRuleService.GetRuleTierById(model.Id);
            if (tier == null)
                return RedirectToAction("List");

            model.SelectedRuleProductIds = model.SelectedRuleProductIds != null
                ? model.SelectedRuleProductIds.Where(x => x > 0).Distinct().ToList()
                : new List<int>();

            if (!SupportsTierRewardTargeting(model))
            {
                model.RewardProductId = null;
                model.AutoAddReward = false;
            }
            else if (model.DiscountTypeId != (int)DiscountType.FreeItem)
                model.AutoAddReward = false;

            if (!ModelState.IsValid)
            {
                model = _promotionRuleModelFactory.PrepareRuleTierModel(model, tier);
                PrepareTierRewardProductName(model);
                return View(model);
            }

            tier = model.ToEntity(tier);
            _promotionRuleService.UpdateRuleTier(tier);
            _promotionRuleService.SaveRuleTierProductMappings(tier.PromotionRuleId, tier.Id, model.SelectedRuleProductIds);

            ViewBag.RefreshPage = true;
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleTierDelete(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var tier = _promotionRuleService.GetRuleTierById(id);
            if (tier != null)
                _promotionRuleService.DeleteRuleTier(tier);

            return new NullJsonResult();
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleConditionList(DataSourceRequest command, PromotionRuleConditionSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(searchModel.PromotionRuleId);
            if (rule == null)
                return Json(new DataSourceResult { Data = new List<PromotionRuleConditionModel>(), Total = 0 });

            try
            {
                int totalCount;
                var items = _promotionRuleModelFactory.PrepareRuleConditionList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
                return Json(new DataSourceResult { Data = items, Total = totalCount });
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load rule conditions for promotion rule " + searchModel.PromotionRuleId + ".", ex);
                return Json(new DataSourceResult { Data = new List<PromotionRuleConditionModel>(), Total = 0 });
            }
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult GetRuleConditions(int promotionRuleId, int conditionId = 0, int groupId = 0, int interactionTypeId = 0, bool deleteCondition = false, bool deleteGroup = false)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            if (rule == null)
                return Json(new PromotionRuleConditionRequirementListModel());

            if (deleteCondition && conditionId > 0)
            {
                var condition = _promotionRuleService.GetRuleConditionById(conditionId);
                if (condition != null && condition.PromotionRuleId == promotionRuleId)
                    DeleteConditionWithChildren(condition);
            }

            if (deleteGroup && groupId > 0)
            {
                var groupConditions = _promotionRuleService.GetRuleConditionsByRuleId(promotionRuleId);
                foreach (var condition in groupConditions.Where(x => (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == groupId))
                    _promotionRuleService.DeleteRuleCondition(condition);
            }

            if (interactionTypeId > 0 && groupId > 0 && Enum.IsDefined(typeof(ConditionLogicalOperator), interactionTypeId))
            {
                var groupConditions = _promotionRuleService.GetRuleConditionsByRuleId(promotionRuleId);
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

            var model = _promotionRuleModelFactory.PrepareRuleConditionRequirementListModel(promotionRuleId);
            return Json(model);
        }

        public ActionResult RuleConditionSelectProductPopup()
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var model = _promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel());
            return View(model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleConditionSelectProductPopupList(DataSourceRequest command, PromotionRuleSelectProductSearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareSelectProductList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        [HttpPost]
        [FormValueRequired("save")]
        [AdminAntiForgery]
        public ActionResult RuleConditionSelectProductPopup([Bind(Prefix = "AddProductModel")] PromotionRuleConditionProductSelectorModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var product = _productService.GetProductById(model.AssociatedToProductId);
            if (product == null)
                return Content("Cannot load a product");

            ViewBag.RefreshPage = true;
            ViewBag.ProductId = product.Id;
            ViewBag.ProductName = product.Name;

            var searchModel = _promotionRuleModelFactory.PrepareSelectProductSearchModel(new PromotionRuleSelectProductSearchModel());
            return View(searchModel);
        }

        public ActionResult RuleRewardAttributePopup(int productId, string valueIdsInput, string btnId, string selectedValueIds)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            if (productId <= 0)
                return Content("Select a product first");

            var model = _promotionRuleModelFactory.PrepareRewardAttributePopupModel(productId, selectedValueIds);
            ViewBag.ValueIdsInput = valueIdsInput;
            ViewBag.BtnId = btnId;
            return PluginView("RuleRewardAttributePopup", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleRewardAttributePopup(PromotionRuleRewardAttributePopupModel model, string valueIdsInput, string btnId, FormCollection form)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var product = _productService.GetProductById(model.ProductId);
            if (product == null)
                return Content("Cannot load a product");

            var warnings = new List<string>();
            var attributesXml = ParseProductAttributesFromForm(product, form, warnings);
            if (!string.IsNullOrWhiteSpace(attributesXml))
            {
                var values = _productAttributeParser.ParseProductAttributeValues(attributesXml);
                model.RewardAttributeValueIds = string.Join(",", values.Select(x => x.Id).Distinct());
            }

            ViewBag.ValueIdsInput = valueIdsInput;
            ViewBag.BtnId = btnId;

            if (warnings.Any())
            {
                var viewModel = _promotionRuleModelFactory.PrepareRewardAttributePopupModel(model.ProductId, model.RewardAttributeValueIds);
                viewModel.Warnings = warnings;
                return PluginView("RuleRewardAttributePopup", viewModel);
            }

            ViewBag.RefreshPage = true;
            return PluginView("RuleRewardAttributePopup", model);
        }

        public ActionResult RuleConditionCreatePopup(int promotionRuleId, int? conditionGroup = null)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            var existingGroups = GetConditionGroups(promotionRuleId, conditionGroup);
            var defaultGroup = conditionGroup.HasValue && conditionGroup.Value > 0
                ? conditionGroup.Value
                : existingGroups.Last();

            var model = _promotionRuleModelFactory.PrepareRuleConditionModel(
                new PromotionRuleConditionModel
                {
                    PromotionRuleId = promotionRuleId,
                    RuleTypeId = rule.RuleTypeId,
                    ConditionGroup = defaultGroup,
                    LogicalOperatorId = (int)ConditionLogicalOperator.And
                }, null);

            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;
            return PluginView("RuleConditionCreatePopup", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleConditionCreatePopup(PromotionRuleConditionModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(model.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            model.RuleTypeId = rule.RuleTypeId;
            if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
                model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
            if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
                model.ParentConditionId = null;

            MapConditionSourceBuilderToStorage(model);
            MapConditionSelectionsToStorage(model);
            NormalizeConditionModelForRule(rule, model);

            ValidateConditionSource(model);

            if (model.ConditionGroup <= 0)
                ModelState.AddModelError("ConditionGroup", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

            ValidateConditionParent(model, null);

            if (rule.RuleType != PromotionRuleType.SubtotalBased &&
                !HasAnyCartConditionCriteria(model))
            {
                ModelState.AddModelError(string.Empty, _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));
            }

            PrepareConditionSelectedNames(model);

            if (!ModelState.IsValid)
            {
                var existingGroups = GetConditionGroups(model.PromotionRuleId, model.ConditionGroup);
                ViewBag.ExistingConditionGroups = existingGroups;
                ViewBag.NextConditionGroup = existingGroups.Max() + 1;
                return View(_promotionRuleModelFactory.PrepareRuleConditionModel(model, null));
            }

            var condition = model.ToEntity();
            _promotionRuleService.InsertRuleCondition(condition);

            ViewBag.RefreshPage = true;
            return View(model);
        }

        public ActionResult RuleConditionEditPopup(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var condition = _promotionRuleService.GetRuleConditionById(id);
            if (condition == null)
                return RedirectToAction("List");

            var rule = _promotionRuleService.GetPromotionRuleById(condition.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            var model = _promotionRuleModelFactory.PrepareRuleConditionModel(null, condition);
            var existingGroups = GetConditionGroups(condition.PromotionRuleId, condition.ConditionGroup);
            ViewBag.ExistingConditionGroups = existingGroups;
            ViewBag.NextConditionGroup = existingGroups.Max() + 1;

            return PluginView("RuleConditionCreatePopup", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleConditionEditPopup(PromotionRuleConditionModel model)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var condition = _promotionRuleService.GetRuleConditionById(model.Id);
            if (condition == null)
                return RedirectToAction("List");

            var rule = _promotionRuleService.GetPromotionRuleById(condition.PromotionRuleId);
            if (rule == null)
                return RedirectToAction("List");

            model.PromotionRuleId = condition.PromotionRuleId;
            model.RuleTypeId = rule.RuleTypeId;
            if (!Enum.IsDefined(typeof(ConditionLogicalOperator), model.LogicalOperatorId))
                model.LogicalOperatorId = (int)ConditionLogicalOperator.And;
            if (!model.ParentConditionId.HasValue || model.ParentConditionId.Value <= 0)
                model.ParentConditionId = null;

            MapConditionSourceBuilderToStorage(model);
            MapConditionSelectionsToStorage(model);
            NormalizeConditionModelForRule(rule, model);

            ValidateConditionSource(model);

            if (model.ConditionGroup <= 0)
                ModelState.AddModelError("ConditionGroup", _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"));

            ValidateConditionParent(model, condition);

            if (rule.RuleType != PromotionRuleType.SubtotalBased &&
                !HasAnyCartConditionCriteria(model))
            {
                ModelState.AddModelError(string.Empty, _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"));
            }

            PrepareConditionSelectedNames(model);

            if (!ModelState.IsValid)
            {
                var existingGroups = GetConditionGroups(model.PromotionRuleId, model.ConditionGroup);
                ViewBag.ExistingConditionGroups = existingGroups;
                ViewBag.NextConditionGroup = existingGroups.Max() + 1;
                return PluginView("RuleConditionCreatePopup", _promotionRuleModelFactory.PrepareRuleConditionModel(model, condition));
            }

            condition = model.ToEntity(condition);
            _promotionRuleService.UpdateRuleCondition(condition);

            ViewBag.RefreshPage = true;
            return PluginView("RuleConditionCreatePopup", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleConditionDelete(int id)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var condition = _promotionRuleService.GetRuleConditionById(id);
            if (condition != null)
                DeleteConditionWithChildren(condition);

            return new NullJsonResult();
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RuleUsageHistoryList(DataSourceRequest command, PromotionRuleUsageHistorySearchModel searchModel)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var rule = _promotionRuleService.GetPromotionRuleById(searchModel.PromotionRuleId);
            if (rule == null)
                return Json(new DataSourceResult { Data = new object[0], Total = 0 });

            int totalCount;
            var items = _promotionRuleModelFactory.PrepareRuleUsageHistoryList(searchModel, GetPageIndex(command), GetPageSize(command), out totalCount);
            return Json(new DataSourceResult { Data = items, Total = totalCount });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult SearchProducts(string q)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var products = _productService.SearchProducts(
                pageIndex: 0,
                pageSize: 20,
                keywords: q,
                showHidden: true);

            return Json(products.Select(x => new { id = x.Id, text = x.Name }).ToList());
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult SearchCategories(string q)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var categories = _categoryService.GetAllCategories(
                categoryName: q ?? string.Empty,
                pageIndex: 0,
                pageSize: 20,
                showHidden: true);

            return Json(categories.Select(x => new { id = x.Id, text = x.Name }).ToList());
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult SearchManufacturers(string q)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var manufacturers = _manufacturerService.GetAllManufacturers(
                manufacturerName: q ?? string.Empty,
                pageIndex: 0,
                pageSize: 20,
                showHidden: true);

            return Json(manufacturers.Select(x => new { id = x.Id, text = x.Name }).ToList());
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult SearchVendors(string q)
        {
            if (!CanManageRules())
                return new HttpUnauthorizedResult();

            var vendors = _vendorService.GetAllVendors(
                name: q ?? string.Empty,
                pageIndex: 0,
                pageSize: 20,
                showHidden: true);

            return Json(vendors.Select(x => new { id = x.Id, text = x.Name }).ToList());
        }

        private List<int> GetConditionGroups(int promotionRuleId, int? includeGroup)
        {
            var existingConditions = _promotionRuleService.GetRuleConditionsByRuleId(promotionRuleId);
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
            var builderState = ConditionSourceBuilderHelper.ParseBuilderStateJson(model != null ? model.ConditionSourceBuilderJson : null);
            var hasSourceBuilderValues = model != null &&
                                         model.ConditionSourceTypeId > 0 &&
                                         ConditionSourceBuilderHelper.HasConfiguredValues(model.ConditionSourceTypeId, builderState);

            return (model.RequiredProductId ?? 0) > 0 ||
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

                    if (model.ConditionSourceTypeId == (int)ConditionSourceType.SpecificationAttributeOptions &&
                        (!row.EntryId.HasValue || row.EntryId.Value <= 0))
                    {
                        ModelState.AddModelError("ConditionSourceData",
                            _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Attribute.Required"));
                        break;
                    }

                    if (model.ConditionSourceTypeId == (int)ConditionSourceType.ProductAttributeValues &&
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
            {
                ModelState.AddModelError("RequiredOrderCountMin",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin.Invalid"));
            }

            if (model.RequiredOrderCountMax.HasValue && model.RequiredOrderCountMax.Value < 0)
            {
                ModelState.AddModelError("RequiredOrderCountMax",
                    _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax.Invalid"));
            }

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
            if (parentCondition == null || parentCondition.PromotionRuleId != model.PromotionRuleId)
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

            var conditions = _promotionRuleService.GetRuleConditionsByRuleId(model.PromotionRuleId);
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

        private void DeleteConditionWithChildren(PromotionRuleCondition condition)
        {
            if (condition == null)
                return;

            var allConditions = _promotionRuleService.GetRuleConditionsByRuleId(condition.PromotionRuleId);
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

        private string ParseProductAttributesFromForm(Product product, FormCollection form, IList<string> warnings)
        {
            string attributesXml = null;
            var attributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            foreach (var attribute in attributes)
            {
                var controlId = string.Format("product_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                int selectedAttributeId;
                                if (int.TryParse(ctrlAttributes, out selectedAttributeId) && selectedAttributeId > 0)
                                    attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            var cblAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    int selectedAttributeId;
                                    if (int.TryParse(item, out selectedAttributeId) && selectedAttributeId > 0)
                                        attributesXml = _productAttributeParser.AddProductAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                }
            }

            return attributesXml;
        }

        #endregion
    }
}
