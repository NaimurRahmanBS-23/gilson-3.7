using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Infrastructure;
using Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using DiscountType = Nop.Plugin.Misc.DiscountManagerPlus.Domain.DiscountType;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Factories
{
    public class PromotionRuleModelFactory : IPromotionRuleModelFactory
    {
        #region Fields

        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly ICategoryService _categoryService;
        private readonly ICustomerService _customerService;
        private readonly ICountryService _countryService;
        private readonly IDiscountService _discountService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ITaxService _taxService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IVendorService _vendorService;
        private readonly IPromotionRuleService _promotionRuleService;
        private readonly IWorkContext _workContext;
        private readonly IPromotionRuleExcludedProductService _excludedProductService;
        private readonly IPictureService _pictureService;

        #endregion

        #region Ctor

        public PromotionRuleModelFactory(
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            ICategoryService categoryService,
            ICustomerService customerService,
            ICountryService countryService,
            IDiscountService discountService,
            IOrderService orderService,
            IPaymentService paymentService,
            IPriceFormatter priceFormatter,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            ISpecificationAttributeService specificationAttributeService,
            IStoreService storeService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            ITaxService taxService,
            IManufacturerService manufacturerService,
            IVendorService vendorService,
            IPromotionRuleService promotionRuleService,
            IWorkContext workContext,
            IPromotionRuleExcludedProductService excludedProductService,
            IPictureService pictureService)
        {
            _dateTimeHelper = dateTimeHelper;
            _localizationService = localizationService;
            _categoryService = categoryService;
            _customerService = customerService;
            _countryService = countryService;
            _discountService = discountService;
            _orderService = orderService;
            _paymentService = paymentService;
            _priceFormatter = priceFormatter;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _productAttributeMappingRepository = productAttributeMappingRepository;
            _specificationAttributeService = specificationAttributeService;
            _storeService = storeService;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _taxService = taxService;
            _manufacturerService = manufacturerService;
            _vendorService = vendorService;
            _promotionRuleService = promotionRuleService;
            _workContext = workContext;
            _excludedProductService = excludedProductService;
            _pictureService = pictureService;
        }

        #endregion

        #region Utilities

        protected virtual void PrepareRuleTypeSelectList(IList<SelectListItem> items)
        {
            foreach (PromotionRuleType ruleType in Enum.GetValues(typeof(PromotionRuleType)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)ruleType).ToString(),
                    Text = ruleType.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareDiscountTypeSelectList(IList<SelectListItem> items)
        {
            foreach (DiscountType discountType in Enum.GetValues(typeof(DiscountType)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)discountType).ToString(),
                    Text = discountType.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareDiscountScopeSelectList(IList<SelectListItem> items)
        {
            foreach (DiscountScope discountScope in Enum.GetValues(typeof(DiscountScope)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)discountScope).ToString(),
                    Text = discountScope.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareLinkedDiscountSelectList(IList<SelectListItem> items)
        {
            items.Clear();
            items.Add(new SelectListItem
            {
                Value = string.Empty,
                Text = _localizationService.GetResource("Admin.Common.None")
            });

            var discounts = _discountService.GetAllDiscounts(null, showHidden: true);
            foreach (var discount in discounts.OrderBy(x => x.Name).ThenBy(x => x.Id))
            {
                var typeText = discount.DiscountType.GetLocalizedEnum(_localizationService, _workContext);
                var couponText = discount.RequiresCouponCode && !string.IsNullOrWhiteSpace(discount.CouponCode)
                    ? " | " + discount.CouponCode
                    : string.Empty;

                items.Add(new SelectListItem
                {
                    Value = discount.Id.ToString(),
                    Text = discount.Name + " (#" + discount.Id + ") | " + typeText + couponText
                });
            }
        }

        protected virtual void PrepareConditionOperatorSelectList(IList<SelectListItem> items)
        {
            foreach (ConditionOperator op in Enum.GetValues(typeof(ConditionOperator)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)op).ToString(),
                    Text = op.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareLogicalOperatorSelectList(IList<SelectListItem> items)
        {
            foreach (ConditionLogicalOperator op in Enum.GetValues(typeof(ConditionLogicalOperator)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)op).ToString(),
                    Text = op.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual IList<SelectListItem> BuildLogicalOperatorSelectList(int selectedId)
        {
            var items = new List<SelectListItem>();
            PrepareLogicalOperatorSelectList(items);
            var selectedValue = selectedId.ToString();
            foreach (var item in items)
                item.Selected = item.Value == selectedValue;
            return items;
        }

        protected virtual HashSet<int> ParseIdSet(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new HashSet<int>();

            var ids = new HashSet<int>();
            foreach (var token in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int id;
                if (int.TryParse(token.Trim(), out id) && id > 0)
                    ids.Add(id);
            }
            return ids;
        }

        protected virtual void PrepareRewardProductAttributeModels(
            IList<PromotionRuleRewardAttributePopupModel.RewardProductAttributeModel> models,
            Product product,
            HashSet<int> selectedValueIds)
        {
            if (models == null)
                throw new ArgumentNullException("models");
            if (product == null)
                throw new ArgumentNullException("product");

            var attributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            var customer = _workContext.CurrentCustomer;

            foreach (var attribute in attributes)
            {
                var attributeDefinition = _productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
                var attributeModel = new PromotionRuleRewardAttributePopupModel.RewardProductAttributeModel
                {
                    Id = attribute.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = attributeDefinition != null ? attributeDefinition.Name : string.Empty,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    HasCondition = !string.IsNullOrEmpty(attribute.ConditionAttributeXml)
                };

                if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                {
                    attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();
                }

                if (attribute.ShouldHaveValues())
                {
                    var attributeValues = _productAttributeService.GetProductAttributeValues(attribute.Id);
                    foreach (var attributeValue in attributeValues)
                    {
                        decimal taxRate;
                        var priceAdjustment = _taxService.GetProductPrice(product,
                            _priceCalculationService.GetProductAttributeValuePriceAdjustment(attributeValue),
                            customer, out taxRate);

                        var priceAdjustmentStr = string.Empty;
                        if (priceAdjustment != 0)
                        {
                            priceAdjustmentStr = priceAdjustment > 0
                                ? "+" + _priceFormatter.FormatPrice(priceAdjustment, false, false)
                                : "-" + _priceFormatter.FormatPrice(-priceAdjustment, false, false);
                        }

                        attributeModel.Values.Add(new PromotionRuleRewardAttributePopupModel.RewardProductAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = attributeValue.Name,
                            IsPreSelected = selectedValueIds.Contains(attributeValue.Id) || attributeValue.IsPreSelected,
                            CustomerEntersQty = false,
                            Quantity = attributeValue.Quantity,
                            PriceAdjustment = priceAdjustmentStr,
                            PriceAdjustmentValue = priceAdjustment
                        });
                    }
                }

                models.Add(attributeModel);
            }
        }

        protected virtual void PrepareConditionRestrictionTypeSelectList(IList<SelectListItem> items)
        {
            foreach (ConditionRestrictionType restriction in Enum.GetValues(typeof(ConditionRestrictionType)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)restriction).ToString(),
                    Text = restriction.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareConditionSourceTypeSelectList(IList<SelectListItem> items)
        {
            items.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Common.Select")
            });

            foreach (ConditionSourceType sourceType in Enum.GetValues(typeof(ConditionSourceType)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)sourceType).ToString(),
                    Text = sourceType.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareAttributeMatchModeSelectList(IList<SelectListItem> items)
        {
            items.Clear();
            foreach (AttributeMatchMode mode in Enum.GetValues(typeof(AttributeMatchMode)))
            {
                items.Add(new SelectListItem
                {
                    Value = ((int)mode).ToString(),
                    Text = mode.GetLocalizedEnum(_localizationService, _workContext)
                });
            }
        }

        protected virtual void PrepareParentConditionSelectList(PromotionRuleConditionModel model, int? currentConditionId)
        {
            model.AvailableParentConditions.Clear();
            model.AvailableParentConditions.Add(new SelectListItem
            {
                Value = string.Empty,
                Text = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.None")
            });

            if (model.PromotionRuleId <= 0 && model.DiscountRequirementId <= 0)
                return;

            var conditions = model.DiscountRequirementId > 0
                ? _promotionRuleService.GetRuleConditionsByDiscountRequirementId(model.DiscountRequirementId)
                : _promotionRuleService.GetRuleConditionsByRuleId(model.PromotionRuleId);
            var selectableParents = conditions
                .Where(x => x.Id != currentConditionId && (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == model.ConditionGroup)
                .OrderBy(x => x.Id)
                .ToList();

            foreach (var parentCondition in selectableParents)
            {
                model.AvailableParentConditions.Add(new SelectListItem
                {
                    Value = parentCondition.Id.ToString(),
                    Text = "#" + parentCondition.Id
                });
            }
        }

        protected virtual void PrepareProductSelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
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

            var products = _productService.SearchProducts(pageIndex: 0, pageSize: 2000, showHidden: true);
            foreach (var product in products)
            {
                items.Add(new SelectListItem
                {
                    Value = product.Id.ToString(),
                    Text = product.Name + " (#" + product.Id + ")"
                });
            }
        }

        protected virtual string BuildRuleProductDisplayName(PromotionRuleProduct ruleProduct)
        {
            if (ruleProduct == null)
                return string.Empty;

            var sourceLabel = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceName");
            var quantityLabel = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MinQuantity");
            var maxQuantityLabel = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity");

            string sourceValue;
            if (ruleProduct.ProductId > 0)
            {
                var product = _productService.GetProductById(ruleProduct.ProductId);
                sourceValue = (product != null ? product.Name : ruleProduct.ProductId.ToString()) + " (#" + ruleProduct.ProductId + ")";
            }
            else if (ruleProduct.CategoryId.HasValue && ruleProduct.CategoryId.Value > 0)
            {
                var category = _categoryService.GetCategoryById(ruleProduct.CategoryId.Value);
                sourceValue = (category != null ? category.Name : ruleProduct.CategoryId.Value.ToString()) + " (Category)";
            }
            else if (ruleProduct.ManufacturerId.HasValue && ruleProduct.ManufacturerId.Value > 0)
            {
                var manufacturer = _manufacturerService.GetManufacturerById(ruleProduct.ManufacturerId.Value);
                sourceValue = (manufacturer != null ? manufacturer.Name : ruleProduct.ManufacturerId.Value.ToString()) + " (Manufacturer)";
            }
            else if (ruleProduct.VendorId.HasValue && ruleProduct.VendorId.Value > 0)
            {
                var vendor = _vendorService.GetVendorById(ruleProduct.VendorId.Value);
                sourceValue = (vendor != null ? vendor.Name : ruleProduct.VendorId.Value.ToString()) + " (Vendor)";
            }
            else
            {
                sourceValue = _localizationService.GetResource("Admin.Common.None");
            }

            var minQuantity = ruleProduct.MinQuantity > 0 ? ruleProduct.MinQuantity : 1;
            var maxQuantity = ruleProduct.MaxQuantity > 0 ? ruleProduct.MaxQuantity.ToString() : "Unlimited";
            return "#" + ruleProduct.Id + " | " + sourceLabel + ": " + sourceValue + " | " + quantityLabel + ": " + minQuantity + " | " + maxQuantityLabel + ": " + maxQuantity;
        }

        protected virtual void PrepareTierRuleProductSelectList(IList<SelectListItem> items, int promotionRuleId, IList<int> selectedRuleProductIds)
        {
            items.Clear();
            if (promotionRuleId <= 0)
                return;

            var selectedIdSet = selectedRuleProductIds != null ? new HashSet<int>(selectedRuleProductIds) : new HashSet<int>();
            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(promotionRuleId);
            foreach (var ruleProduct in ruleProducts.Where(x => !x.IsRewardProduct).OrderBy(x => x.Id))
            {
                items.Add(new SelectListItem
                {
                    Value = ruleProduct.Id.ToString(),
                    Text = BuildRuleProductDisplayName(ruleProduct),
                    Selected = selectedIdSet.Contains(ruleProduct.Id)
                });
            }
        }

        protected virtual string BuildTierAppliesToRuleProductsSummary(int promotionRuleId, IList<int> mappedRuleProductIds)
        {
            var selectedIds = mappedRuleProductIds != null
                ? mappedRuleProductIds.Where(x => x > 0).Distinct().ToList()
                : new List<int>();
            if (!selectedIds.Any())
                return _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts.All");

            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(promotionRuleId);
            var mappedRuleProducts = ruleProducts.Where(x => selectedIds.Contains(x.Id)).OrderBy(x => x.Id).ToList();
            if (!mappedRuleProducts.Any())
                return _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts.All");

            var names = new List<string>();
            foreach (var ruleProduct in mappedRuleProducts.Take(3))
                names.Add(BuildRuleProductDisplayName(ruleProduct));

            if (mappedRuleProducts.Count > 3)
                names.Add("+" + (mappedRuleProducts.Count - 3));

            return string.Join("; ", names);
        }

        protected virtual void PrepareCategorySelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
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

            var categories = _categoryService.GetAllCategories(showHidden: true);
            foreach (var category in categories)
            {
                items.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.Name
                });
            }
        }

        protected virtual void PrepareVendorSelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
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

            var vendors = _vendorService.GetAllVendors(showHidden: true, pageIndex: 0, pageSize: 2000);
            foreach (var vendor in vendors)
            {
                items.Add(new SelectListItem
                {
                    Value = vendor.Id.ToString(),
                    Text = vendor.Name
                });
            }
        }

        protected virtual void PrepareCustomerRoleSelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
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

            var roles = _customerService.GetAllCustomerRoles(true);
            foreach (var role in roles)
            {
                items.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name
                });
            }
        }

        protected virtual void PrepareCountrySelectList(IList<SelectListItem> items, IList<string> selectedCountryCodes)
        {
            items.Clear();

            var selectedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selectedCountryCodes != null)
            {
                foreach (var code in selectedCountryCodes.Where(x => !string.IsNullOrWhiteSpace(x)))
                    selectedCodes.Add(code.Trim().ToUpperInvariant());
            }

            var countries = _countryService.GetAllCountriesForBilling(showHidden: true);
            foreach (var country in countries)
            {
                if (string.IsNullOrWhiteSpace(country.TwoLetterIsoCode))
                    continue;

                items.Add(new SelectListItem
                {
                    Value = country.TwoLetterIsoCode.ToUpperInvariant(),
                    Text = country.Name,
                    Selected = selectedCodes.Contains(country.TwoLetterIsoCode)
                });
            }
        }

        protected virtual string BuildRuleProductAttributeFilterSummary(PromotionRuleProduct ruleProduct)
        {
            if (ruleProduct == null || ruleProduct.ProductId <= 0)
                return string.Empty;

            if (ruleProduct.RewardAttributeSelectionType != RewardAttributeSelectionType.SpecificValues ||
                string.IsNullOrWhiteSpace(ruleProduct.RewardAttributeValueIds))
            {
                return RewardAttributeSelectionType.Any.GetLocalizedEnum(_localizationService, _workContext);
            }

            var valueIds = ParseCsvList(ruleProduct.RewardAttributeValueIds)
                .Select(x =>
                {
                    int valueId;
                    return int.TryParse(x, out valueId) ? valueId : 0;
                })
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            if (!valueIds.Any())
                return RewardAttributeSelectionType.Any.GetLocalizedEnum(_localizationService, _workContext);

            var summaries = new List<string>();
            foreach (var valueId in valueIds)
            {
                var value = _productAttributeService.GetProductAttributeValueById(valueId);
                if (value == null)
                    continue;

                var mapping = _productAttributeService.GetProductAttributeMappingById(value.ProductAttributeMappingId);
                if (mapping == null)
                    continue;

                var attribute = _productAttributeService.GetProductAttributeById(mapping.ProductAttributeId);
                var attributeName = !string.IsNullOrWhiteSpace(mapping.TextPrompt)
                    ? mapping.TextPrompt
                    : (attribute != null ? attribute.Name : null);

                summaries.Add(!string.IsNullOrWhiteSpace(attributeName)
                    ? attributeName + ": " + value.Name
                    : value.Name);
            }

            return summaries.Any()
                ? string.Join("; ", summaries.Distinct(StringComparer.OrdinalIgnoreCase))
                : RewardAttributeSelectionType.Any.GetLocalizedEnum(_localizationService, _workContext);
        }

        protected virtual void PrepareManufacturerSelectList(IList<SelectListItem> items, bool includeEmptyItem = true)
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

            var manufacturers = _manufacturerService.GetAllManufacturers(showHidden: true);
            foreach (var manufacturer in manufacturers)
            {
                items.Add(new SelectListItem
                {
                    Value = manufacturer.Id.ToString(),
                    Text = manufacturer.Name
                });
            }
        }

        protected virtual void PrepareSpecificationAttributeSelectList(IList<SelectListItem> items)
        {
            items.Clear();
            var attributes = _specificationAttributeService.GetSpecificationAttributes(0, int.MaxValue);
            foreach (var attribute in attributes)
            {
                items.Add(new SelectListItem
                {
                    Value = attribute.Id.ToString(),
                    Text = attribute.Name
                });
            }
        }

        protected virtual void PrepareProductAttributeSelectList(IList<SelectListItem> items)
        {
            items.Clear();
            var attributes = _productAttributeService.GetAllProductAttributes(0, 2000);
            foreach (var attribute in attributes)
            {
                items.Add(new SelectListItem
                {
                    Value = attribute.Id.ToString(),
                    Text = attribute.Name
                });
            }
        }

        protected virtual void PrepareSourceTokenSelectLists(IList<SelectListItem> availableDeviceTypes, IList<SelectListItem> availableSalesChannels)
        {
            availableDeviceTypes.Clear();
            availableSalesChannels.Clear();

            var deviceValues = new[] { "desktop", "mobile", "tablet" };
            foreach (var deviceValue in deviceValues)
            {
                availableDeviceTypes.Add(new SelectListItem
                {
                    Value = deviceValue,
                    Text = deviceValue
                });
            }

            var salesChannels = new[] { "web", "app" };
            foreach (var salesChannel in salesChannels)
            {
                availableSalesChannels.Add(new SelectListItem
                {
                    Value = salesChannel,
                    Text = salesChannel
                });
            }
        }

        protected virtual void PrepareSpecificationAttributeOptionsLookup(IDictionary<int, IList<SelectListItem>> lookup)
        {
            lookup.Clear();
            var attributes = _specificationAttributeService.GetSpecificationAttributes(0, int.MaxValue);
            foreach (var attribute in attributes)
            {
                var options = _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(attribute.Id);
                lookup[attribute.Id] = options
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToList();
            }
        }

        protected virtual void PrepareProductAttributeValuesLookup(IDictionary<int, IList<SelectListItem>> lookup)
        {
            lookup.Clear();
            var attributes = _productAttributeService.GetAllProductAttributes(0, 2000);
            foreach (var attribute in attributes)
            {
                var mappings = _productAttributeMappingRepository.Table
                    .Where(x => x.ProductAttributeId == attribute.Id)
                    .ToList();

                if (!mappings.Any())
                {
                    lookup[attribute.Id] = new List<SelectListItem>();
                    continue;
                }

                var values = new List<ProductAttributeValue>();
                foreach (var mapping in mappings)
                    values.AddRange(_productAttributeService.GetProductAttributeValues(mapping.Id));

                lookup[attribute.Id] = values
                    .Where(x => x != null)
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToList();
            }
        }

        protected virtual void PreparePaymentMethodSelectList(IList<SelectListItem> items, IList<string> selectedPaymentMethods)
        {
            items.Clear();

            var selectedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selectedPaymentMethods != null)
            {
                foreach (var method in selectedPaymentMethods.Where(x => !string.IsNullOrWhiteSpace(x)))
                    selectedMethods.Add(method.Trim());
            }

            foreach (var method in _paymentService.LoadAllPaymentMethods().OrderBy(x => x.PluginDescriptor.FriendlyName))
            {
                var systemName = method.PluginDescriptor.SystemName;
                if (string.IsNullOrWhiteSpace(systemName))
                    continue;

                items.Add(new SelectListItem
                {
                    Value = systemName,
                    Text = method.PluginDescriptor.FriendlyName,
                    Selected = selectedMethods.Contains(systemName)
                });
            }
        }

        protected virtual IList<string> ParseCsvList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new string[0];

            return raw
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        protected virtual void HydrateConditionSourceBuilder(PromotionRuleConditionModel model)
        {
            if (model == null)
                return;

            if (model.SourceBuilderRows == null)
                model.SourceBuilderRows = new List<ConditionSourceBuilderRowModel>();
            if (model.SelectedSourceTokens == null)
                model.SelectedSourceTokens = new List<string>();

            if (!string.IsNullOrWhiteSpace(model.ConditionSourceBuilderJson) &&
                (model.SourceBuilderRows.Any() || model.SelectedSourceTokens.Any()))
            {
                return;
            }

            if (model.ConditionSourceTypeId <= 0 || string.IsNullOrWhiteSpace(model.ConditionSourceData))
            {
                model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel
                {
                    Rows = model.SourceBuilderRows,
                    Tokens = model.SelectedSourceTokens
                });
                return;
            }

            if (ConditionSourceBuilderHelper.IsSessionSourceType(model.ConditionSourceTypeId))
            {
                model.SelectedSourceTokens = ConditionSourceBuilderHelper.ParseSessionTokens(model.ConditionSourceData).ToList();
                model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel
                {
                    Tokens = model.SelectedSourceTokens
                });
                return;
            }

            var rows = ConditionSourceBuilderHelper.ParseRows(model.ConditionSourceTypeId, model.ConditionSourceData).ToList();
            if (!rows.Any() && !string.IsNullOrWhiteSpace(model.ConditionSourceData))
            {
                model.ConditionSourceBuilderWarning = model.ConditionSourceData;
                model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel());
                return;
            }

            var productCache = new Dictionary<int, Product>();
            var categoryCache = new Dictionary<int, Category>();
            var manufacturerCache = new Dictionary<int, Manufacturer>();
            var vendorCache = new Dictionary<int, Vendor>();
            var specAttributeCache = new Dictionary<int, SpecificationAttribute>();
            var specOptionCache = new Dictionary<int, SpecificationAttributeOption>();
            var productAttributeCache = new Dictionary<int, ProductAttribute>();
            var productAttributeValueCache = new Dictionary<int, ProductAttributeValue>();

            foreach (var row in rows)
            {
                if (row.EntryId.HasValue && row.EntryId.Value > 0)
                    HydrateSourceBuilderRow(model.ConditionSourceTypeId, row, productCache, categoryCache, manufacturerCache, vendorCache, specAttributeCache, specOptionCache, productAttributeCache, productAttributeValueCache);
                else if (model.ConditionSourceTypeId == (int)ConditionSourceType.ExpiryDays)
                    row.EntryName = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.AnyProduct");
            }

            model.SourceBuilderRows = rows;
            model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel
            {
                Rows = rows
            });
        }

        private void HydrateSourceBuilderRow(
            int sourceTypeId,
            ConditionSourceBuilderRowModel row,
            Dictionary<int, Product> productCache,
            Dictionary<int, Category> categoryCache,
            Dictionary<int, Manufacturer> manufacturerCache,
            Dictionary<int, Vendor> vendorCache,
            Dictionary<int, SpecificationAttribute> specAttributeCache,
            Dictionary<int, SpecificationAttributeOption> specOptionCache,
            Dictionary<int, ProductAttribute> productAttributeCache,
            Dictionary<int, ProductAttributeValue> productAttributeValueCache)
        {
            var entryId = row.EntryId.Value;
            switch ((ConditionSourceType)sourceTypeId)
            {
                case ConditionSourceType.Products:
                case ConditionSourceType.ExpiryDays:
                    Product product;
                    if (!productCache.TryGetValue(entryId, out product))
                    {
                        product = _productService.GetProductById(entryId);
                        if (product != null)
                            productCache[entryId] = product;
                    }
                    row.EntryName = product != null ? product.Name : "#" + entryId;
                    break;
                case ConditionSourceType.Categories:
                    Category category;
                    if (!categoryCache.TryGetValue(entryId, out category))
                    {
                        category = _categoryService.GetCategoryById(entryId);
                        if (category != null)
                            categoryCache[entryId] = category;
                    }
                    row.EntryName = category != null ? category.Name : "#" + entryId;
                    break;
                case ConditionSourceType.Manufacturers:
                    Manufacturer manufacturer;
                    if (!manufacturerCache.TryGetValue(entryId, out manufacturer))
                    {
                        manufacturer = _manufacturerService.GetManufacturerById(entryId);
                        if (manufacturer != null)
                            manufacturerCache[entryId] = manufacturer;
                    }
                    row.EntryName = manufacturer != null ? manufacturer.Name : "#" + entryId;
                    break;
                case ConditionSourceType.Vendors:
                    Vendor vendor;
                    if (!vendorCache.TryGetValue(entryId, out vendor))
                    {
                        vendor = _vendorService.GetVendorById(entryId);
                        if (vendor != null)
                            vendorCache[entryId] = vendor;
                    }
                    row.EntryName = vendor != null ? vendor.Name : "#" + entryId;
                    break;
                case ConditionSourceType.SpecificationAttributeOptions:
                    SpecificationAttribute specAttribute;
                    if (!specAttributeCache.TryGetValue(entryId, out specAttribute))
                    {
                        specAttribute = _specificationAttributeService.GetSpecificationAttributeById(entryId);
                        if (specAttribute != null)
                            specAttributeCache[entryId] = specAttribute;
                    }
                    row.EntryName = specAttribute != null ? specAttribute.Name : "#" + entryId;
                    for (var index = 0; index < row.SelectedOptionValues.Count; index++)
                    {
                        int optionId;
                        if (!int.TryParse(row.SelectedOptionValues[index], out optionId) || optionId <= 0)
                            continue;

                        SpecificationAttributeOption option;
                        if (!specOptionCache.TryGetValue(optionId, out option))
                        {
                            option = _specificationAttributeService.GetSpecificationAttributeOptionById(optionId);
                            if (option != null)
                                specOptionCache[optionId] = option;
                        }
                        if (option == null)
                            continue;

                        if (row.SelectedOptionTexts.Count <= index)
                            row.SelectedOptionTexts.Add(option.Name);
                        else if (string.IsNullOrWhiteSpace(row.SelectedOptionTexts[index]))
                            row.SelectedOptionTexts[index] = option.Name;
                    }
                    break;
                case ConditionSourceType.ProductAttributeValues:
                    ProductAttribute productAttribute;
                    if (!productAttributeCache.TryGetValue(entryId, out productAttribute))
                    {
                        productAttribute = _productAttributeService.GetProductAttributeById(entryId);
                        if (productAttribute != null)
                            productAttributeCache[entryId] = productAttribute;
                    }
                    row.EntryName = productAttribute != null ? productAttribute.Name : "#" + entryId;
                    var normalizedOptionValues = new List<string>();
                    var normalizedOptionTexts = new List<string>();
                    foreach (var selectedOptionValue in row.SelectedOptionValues)
                    {
                        int optionId;
                        if (int.TryParse(selectedOptionValue, out optionId) && optionId > 0)
                        {
                            ProductAttributeValue optionValue;
                            if (!productAttributeValueCache.TryGetValue(optionId, out optionValue))
                            {
                                optionValue = _productAttributeService.GetProductAttributeValueById(optionId);
                                if (optionValue != null)
                                    productAttributeValueCache[optionId] = optionValue;
                            }
                            if (optionValue != null)
                            {
                                normalizedOptionValues.Add(optionValue.Id.ToString());
                                normalizedOptionTexts.Add(optionValue.Name);
                                continue;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(selectedOptionValue))
                        {
                            normalizedOptionValues.Add(selectedOptionValue);
                            normalizedOptionTexts.Add(selectedOptionValue);
                        }
                    }
                    row.SelectedOptionTexts = normalizedOptionTexts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    row.SelectedOptionValues = normalizedOptionValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    break;
            }
        }

        protected virtual string BuildConditionSourceSummary(PromotionRuleConditionModel model)
        {
            if (model.ConditionSourceTypeId <= 0)
                return string.Empty;

            if (ConditionSourceBuilderHelper.IsSessionSourceType(model.ConditionSourceTypeId))
            {
                var tokens = (model.SelectedSourceTokens ?? new string[0])
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return tokens.Any() ? string.Join(", ", tokens) : string.Empty;
            }

            if (!model.SourceBuilderRows.Any())
                return string.IsNullOrWhiteSpace(model.ConditionSourceBuilderWarning) ? model.ConditionSourceData : model.ConditionSourceBuilderWarning;

            var sourceType = (ConditionSourceType)model.ConditionSourceTypeId;
            var summaries = new List<string>();
            foreach (var row in model.SourceBuilderRows)
            {
                var entryName = !string.IsNullOrWhiteSpace(row.EntryName)
                    ? row.EntryName
                    : row.EntryId.HasValue
                        ? "#" + row.EntryId.Value
                        : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.AnyProduct");

                if (sourceType == ConditionSourceType.SpecificationAttributeOptions ||
                    sourceType == ConditionSourceType.ProductAttributeValues)
                {
                    var optionTexts = row.SelectedOptionTexts != null
                        ? row.SelectedOptionTexts.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                        : new List<string>();
                    if (optionTexts.Any())
                        entryName = entryName + " [" + string.Join(", ", optionTexts) + "]";
                }

                if (row.RangeMin.HasValue || row.RangeMax.HasValue)
                {
                    var rangeText = row.RangeMin.HasValue && row.RangeMax.HasValue
                        ? row.RangeMin.Value + "-" + row.RangeMax.Value
                        : row.RangeMin.HasValue
                            ? row.RangeMin.Value + "+"
                            : "0-" + row.RangeMax.Value;
                    entryName = sourceType == ConditionSourceType.ExpiryDays
                        ? entryName + " (" + rangeText + " days)"
                        : entryName + " (" + rangeText + ")";
                }

                summaries.Add(entryName);
            }

            return string.Join("; ", summaries);
        }

        protected virtual string BuildCountrySummary(string csv)
        {
            var codes = new HashSet<string>(ParseCsvList(csv).Select(x => x.ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);
            if (!codes.Any())
                return string.Empty;

            var countries = _countryService.GetAllCountriesForBilling(showHidden: true);
            var names = countries
                .Where(x => !string.IsNullOrWhiteSpace(x.TwoLetterIsoCode) && codes.Contains(x.TwoLetterIsoCode))
                .Select(x => x.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return names.Any() ? string.Join(", ", names) : string.Join(", ", codes);
        }

        protected virtual string BuildPaymentMethodSummary(string csv)
        {
            var systemNames = new HashSet<string>(ParseCsvList(csv), StringComparer.OrdinalIgnoreCase);
            if (!systemNames.Any())
                return string.Empty;

            var names = _paymentService.LoadAllPaymentMethods()
                .Where(x => systemNames.Contains(x.PluginDescriptor.SystemName))
                .Select(x => x.PluginDescriptor.FriendlyName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return names.Any() ? string.Join(", ", names) : string.Join(", ", systemNames);
        }

        protected virtual void PopulateSetupStatus(PromotionRuleModel model, PromotionRule promotionRule)
        {
            var productsRequired = promotionRule.RuleType == PromotionRuleType.ProductBased ||
                                   promotionRule.RuleType == PromotionRuleType.ComboPricing ||
                                   promotionRule.RuleType == PromotionRuleType.BuyXGetY;
            var tiersRequired = promotionRule.RuleType == PromotionRuleType.BuyXGetY;
            var conditionsRequired = promotionRule.RuleType == PromotionRuleType.CartCondition ||
                                     promotionRule.RuleType == PromotionRuleType.SubtotalBased;

            var productsConfigured = !productsRequired || model.RuleProductCount > 0;
            var tiersConfigured = !tiersRequired || model.RuleTierCount > 0;
            var conditionsConfigured = !conditionsRequired || model.RuleConditionCount > 0;

            var isSetupComplete = productsConfigured && tiersConfigured && conditionsConfigured;
            model.SetupStatus = _localizationService.GetResource(
                isSetupComplete
                    ? "Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupStatus.Ready"
                    : "Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupStatus.NeedsConfiguration");

            if (isSetupComplete)
            {
                model.SetupDetails = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupDetails.Ready");
                return;
            }

            var missingSections = new List<string>();
            if (productsRequired && !productsConfigured)
                missingSections.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Products"));
            if (tiersRequired && !tiersConfigured)
                missingSections.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Tiers"));
            if (conditionsRequired && !conditionsConfigured)
                missingSections.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Conditions"));

            model.SetupDetails = string.Format(
                _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupDetails.Missing"),
                string.Join(", ", missingSections));
        }

        protected virtual void PopulateParentDiscountSummary(PromotionRuleModel model, Discount discount)
        {
            if (model == null || discount == null)
                return;

            model.ParentDiscountName = discount.Name + " (#" + discount.Id + ")";

            var limitationText = discount.DiscountLimitation.GetLocalizedEnum(_localizationService, _workContext);
            if ((discount.DiscountLimitation == DiscountLimitationType.NTimesOnly ||
                 discount.DiscountLimitation == DiscountLimitationType.NTimesPerCustomer) &&
                discount.LimitationTimes > 0)
            {
                model.ParentDiscountLimitationSummary = limitationText + " (" + discount.LimitationTimes + ")";
            }
            else
            {
                model.ParentDiscountLimitationSummary = limitationText;
            }

            model.ParentDiscountMaximumDiscountAmountDisplay = discount.MaximumDiscountAmount.HasValue
                ? _priceFormatter.FormatPrice(discount.MaximumDiscountAmount.Value, true, false)
                : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountAmount.Unlimited");

            model.ParentDiscountMaximumDiscountedQuantityDisplay = discount.MaximumDiscountedQuantity.HasValue &&
                                                                   discount.MaximumDiscountedQuantity.Value > 0
                ? discount.MaximumDiscountedQuantity.Value.ToString()
                : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountedQuantity.Unlimited");

            model.ParentDiscountUsesNativeCustomerLimit =
                discount.DiscountLimitation == DiscountLimitationType.NTimesPerCustomer;
        }

        protected virtual void PopulateParentDiscountSummary(DiscountDetailsPromotionRulesModel model, Discount discount)
        {
            if (model == null || discount == null)
                return;

            model.DiscountName = discount.Name + " (#" + discount.Id + ")";

            var limitationText = discount.DiscountLimitation.GetLocalizedEnum(_localizationService, _workContext);
            if ((discount.DiscountLimitation == DiscountLimitationType.NTimesOnly ||
                 discount.DiscountLimitation == DiscountLimitationType.NTimesPerCustomer) &&
                discount.LimitationTimes > 0)
            {
                model.ParentDiscountLimitationSummary = limitationText + " (" + discount.LimitationTimes + ")";
            }
            else
            {
                model.ParentDiscountLimitationSummary = limitationText;
            }

            model.ParentDiscountMaximumDiscountAmountDisplay = discount.MaximumDiscountAmount.HasValue
                ? _priceFormatter.FormatPrice(discount.MaximumDiscountAmount.Value, true, false)
                : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountAmount.Unlimited");

            model.ParentDiscountMaximumDiscountedQuantityDisplay = discount.MaximumDiscountedQuantity.HasValue &&
                                                                   discount.MaximumDiscountedQuantity.Value > 0
                ? discount.MaximumDiscountedQuantity.Value.ToString()
                : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountedQuantity.Unlimited");

            model.ParentDiscountUsesNativeCustomerLimit =
                discount.DiscountLimitation == DiscountLimitationType.NTimesPerCustomer;
        }

        protected virtual void PrepareAvailableStores(PromotionRuleModel model, PromotionRule promotionRule)
        {
            if (promotionRule != null)
            {
                var mappedStoreIds = _storeMappingService.GetStoresIdsWithAccess(promotionRule);
                if (mappedStoreIds != null && mappedStoreIds.Length > 0)
                    model.SelectedStoreIds = mappedStoreIds.ToList();
                else if (!promotionRule.LimitedToStores && promotionRule.LimitedToStore > 0 && !model.SelectedStoreIds.Any())
                    model.SelectedStoreIds.Add(promotionRule.LimitedToStore);
            }

            model.AvailableStores.Clear();
            foreach (var store in _storeService.GetAllStores())
            {
                model.AvailableStores.Add(new SelectListItem
                {
                    Value = store.Id.ToString(),
                    Text = store.Name,
                    Selected = model.SelectedStoreIds.Contains(store.Id)
                });
            }
        }

        private static IList<T> PageItems<T>(IList<T> items, int pageIndex, int pageSize, out int totalCount)
        {
            totalCount = items.Count;
            if (pageSize <= 0)
                pageSize = 15;
            if (pageIndex < 0)
                pageIndex = 0;
            return items.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        }

        #endregion

        #region Methods

        public PromotionRuleSearchModel PreparePromotionRuleSearchModel(PromotionRuleSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            PrepareRuleTypeSelectList(searchModel.AvailableRuleTypes);
            searchModel.AvailableRuleTypes.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Common.All")
            });

            if (searchModel.PageSize <= 0)
                searchModel.PageSize = 15;
            return searchModel;
        }

        public DiscountDetailsPromotionRulesModel PrepareDiscountDetailsPromotionRulesModel(int discountId)
        {
            var model = new DiscountDetailsPromotionRulesModel
            {
                DiscountId = discountId
            };

            if (discountId > 0)
            {
                var discount = _discountService.GetDiscountById(discountId);
                if (discount != null)
                    PopulateParentDiscountSummary(model, discount);
            }

            model.SearchModel = PreparePromotionRuleSearchModel(new PromotionRuleSearchModel
            {
                DiscountId = discountId,
                HideFilters = true
            });

            if (discountId > 0)
            {
                var rules = _promotionRuleService.GetPromotionRulesByDiscountId(discountId);
                model.Rules = new List<PromotionRuleModel>();
                foreach (var rule in rules)
                    model.Rules.Add(PreparePromotionRuleModel(null, rule, true));
            }

            return model;
        }

        public IList<PromotionRuleModel> PreparePromotionRuleList(PromotionRuleSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var rules = _promotionRuleService.GetAllPromotionRules(
                name: searchModel.SearchName,
                ruleTypeId: searchModel.SearchRuleTypeId > 0 ? searchModel.SearchRuleTypeId : (int?)null,
                isActive: searchModel.SearchIsActive,
                discountId: searchModel.DiscountId,
                pageIndex: pageIndex < 0 ? 0 : pageIndex,
                pageSize: pageSize <= 0 ? 15 : pageSize);

            totalCount = rules.TotalCount;
            return rules.Select(x => PreparePromotionRuleModel(null, x, true)).ToList();
        }

        public PromotionRuleModel PreparePromotionRuleModel(PromotionRuleModel model, PromotionRule promotionRule, bool excludeProperties = false)
        {
            if (model == null)
                model = new PromotionRuleModel();

            if (promotionRule != null)
            {
                if (model.Id == 0)
                    model = promotionRule.ToModel();

                model.CreatedOn = _dateTimeHelper.ConvertToUserTime(promotionRule.CreatedOnUtc, DateTimeKind.Utc);
                model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(promotionRule.UpdatedOnUtc, DateTimeKind.Utc);

                model.RuleTypeName = promotionRule.RuleType.GetLocalizedEnum(_localizationService, _workContext);
                model.DiscountTypeName = promotionRule.DiscountType.GetLocalizedEnum(_localizationService, _workContext);
                model.DiscountScopeName = promotionRule.DiscountScope.GetLocalizedEnum(_localizationService, _workContext);
                var parentDiscountId = promotionRule.DiscountId > 0
                    ? promotionRule.DiscountId
                    : promotionRule.LinkedDiscountId.GetValueOrDefault();
                if (parentDiscountId > 0)
                {
                    model.DiscountId = parentDiscountId;
                    model.IsDiscountBound = true;
                    var linkedDiscount = _discountService.GetDiscountById(parentDiscountId);
                    if (linkedDiscount != null)
                        PopulateParentDiscountSummary(model, linkedDiscount);

                    model.LinkedDiscountName = model.ParentDiscountName;
                }

                if (promotionRule.StartDateUtc.HasValue)
                    model.StartDate = _dateTimeHelper.ConvertToUserTime(promotionRule.StartDateUtc.Value, DateTimeKind.Utc);

                if (promotionRule.EndDateUtc.HasValue)
                    model.EndDate = _dateTimeHelper.ConvertToUserTime(promotionRule.EndDateUtc.Value, DateTimeKind.Utc);

                if (promotionRule.UsageWindowStartUtc.HasValue)
                    model.UsageWindowStartUtc = _dateTimeHelper.ConvertToUserTime(promotionRule.UsageWindowStartUtc.Value, DateTimeKind.Utc);

                if (promotionRule.UsageWindowEndUtc.HasValue)
                    model.UsageWindowEndUtc = _dateTimeHelper.ConvertToUserTime(promotionRule.UsageWindowEndUtc.Value, DateTimeKind.Utc);

                model.RuleProductCount = _promotionRuleService.GetRuleProductsByRuleId(promotionRule.Id).Count;
                model.RuleTierCount = _promotionRuleService.GetRuleTiersByRuleId(promotionRule.Id).Count;
                model.RuleConditionCount = _promotionRuleService.GetRuleConditionsByRuleId(promotionRule.Id).Count;
                model.ExcludedProductCount = _excludedProductService.GetExcludedProductsByRuleId(promotionRule.Id).Count;
                PopulateSetupStatus(model, promotionRule);

                if (!excludeProperties)
                {
                    var allowRewardProduct = promotionRule.RuleType == PromotionRuleType.BuyXGetY ||
                                             (promotionRule.RuleType == PromotionRuleType.ProductBased && promotionRule.DiscountType == DiscountType.FreeItem);

                    model.RuleProductSearchModel = new PromotionRuleProductSearchModel
                    {
                        PromotionRuleId = promotionRule.Id,
                        AllowRewardProduct = allowRewardProduct,
                        PageSize = 15
                    };
                    model.RuleTierSearchModel = new PromotionRuleTierSearchModel { PromotionRuleId = promotionRule.Id, PageSize = 15 };
                    model.RuleConditionSearchModel = new PromotionRuleConditionSearchModel { PromotionRuleId = promotionRule.Id, PageSize = 15 };
                    model.ExcludedProductSearchModel = new PromotionRuleExcludedProductSearchModel { PromotionRuleId = promotionRule.Id, PageSize = 15 };
                    model.RuleUsageHistorySearchModel = new PromotionRuleUsageHistorySearchModel
                    {
                        PromotionRuleId = promotionRule.Id,
                        UseParentDiscountHistory = model.IsDiscountBound,
                        ParentDiscountName = model.ParentDiscountName,
                        PageSize = 15
                    };
                }
            }

            if (!excludeProperties)
            {
                PrepareRuleTypeSelectList(model.AvailableRuleTypes);
                PrepareDiscountTypeSelectList(model.AvailableDiscountTypes);
                PrepareDiscountScopeSelectList(model.AvailableDiscountScopes);
                if (!model.IsDiscountBound)
                    PrepareLinkedDiscountSelectList(model.AvailableLinkedDiscounts);

                if (promotionRule != null && !promotionRule.LimitedToStores && promotionRule.LimitedToStore > 0 && !model.SelectedStoreIds.Any())
                    model.SelectedStoreIds.Add(promotionRule.LimitedToStore);

                PrepareAvailableStores(model, promotionRule);
            }

            if (model.IsDiscountBound && model.DiscountId > 0 && string.IsNullOrWhiteSpace(model.ParentDiscountName))
            {
                var discount = _discountService.GetDiscountById(model.DiscountId);
                if (discount != null)
                {
                    PopulateParentDiscountSummary(model, discount);
                    model.LinkedDiscountName = model.ParentDiscountName;
                }
            }

            return model ?? new PromotionRuleModel();
        }

        public IList<PromotionRuleUsageHistoryModel> PrepareRuleUsageHistoryList(PromotionRuleUsageHistorySearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var promotionRule = _promotionRuleService.GetPromotionRuleById(searchModel.PromotionRuleId);
            if (promotionRule == null)
            {
                totalCount = 0;
                return new List<PromotionRuleUsageHistoryModel>();
            }

            var parentDiscountId = promotionRule.DiscountId > 0
                ? promotionRule.DiscountId
                : promotionRule.LinkedDiscountId.GetValueOrDefault();

            if (parentDiscountId > 0)
            {
                var discount = _discountService.GetDiscountById(parentDiscountId);
                searchModel.ParentDiscountName = discount == null ? string.Empty : discount.Name + " (#" + discount.Id + ")";

                var linkedRuleUsageHistory = _promotionRuleService.GetRuleUsages(
                    promotionRuleId: searchModel.PromotionRuleId,
                    pageIndex: pageIndex < 0 ? 0 : pageIndex,
                    pageSize: pageSize <= 0 ? 15 : pageSize);

                if (linkedRuleUsageHistory.Any())
                {
                    searchModel.UseParentDiscountHistory = false;
                    totalCount = linkedRuleUsageHistory.TotalCount;
                    return linkedRuleUsageHistory.Select(historyEntry => MapPluginUsageHistory(historyEntry)).ToList();
                }

                searchModel.UseParentDiscountHistory = true;
                var usageHistory = _discountService.GetAllDiscountUsageHistory(
                    discountId: parentDiscountId,
                    pageIndex: pageIndex < 0 ? 0 : pageIndex,
                    pageSize: pageSize <= 0 ? 15 : pageSize);
                totalCount = usageHistory.TotalCount;
                return usageHistory.Select(historyEntry => MapParentDiscountUsageHistory(historyEntry, searchModel.ParentDiscountName)).ToList();
            }

            searchModel.UseParentDiscountHistory = false;
            var standaloneUsageHistory = _promotionRuleService.GetRuleUsages(
                promotionRuleId: searchModel.PromotionRuleId,
                pageIndex: pageIndex < 0 ? 0 : pageIndex,
                pageSize: pageSize <= 0 ? 15 : pageSize);
            totalCount = standaloneUsageHistory.TotalCount;
            return standaloneUsageHistory.Select(historyEntry => MapPluginUsageHistory(historyEntry)).ToList();
        }

        private PromotionRuleUsageHistoryModel MapPluginUsageHistory(PromotionRuleUsage historyEntry)
        {
            var usageModel = new PromotionRuleUsageHistoryModel
            {
                Id = historyEntry.Id,
                CreatedOn = _dateTimeHelper.ConvertToUserTime(historyEntry.CreatedOnUtc, DateTimeKind.Utc),
                DiscountAmountApplied = _priceFormatter.FormatPrice(historyEntry.DiscountAmountApplied, true, false),
                HistorySource = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Source.Plugin")
            };

            var customer = _customerService.GetCustomerById(historyEntry.CustomerId);
            usageModel.CustomerEmail = customer != null && !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");

            var order = _orderService.GetOrderById(historyEntry.OrderId);
            if (order != null && !order.Deleted)
            {
                usageModel.OrderId = order.Id;
                usageModel.CustomOrderNumber = order.Id.ToString();
                usageModel.OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal, true, false);
            }
            else
            {
                usageModel.CustomOrderNumber = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Order.Deleted");
                usageModel.CustomerEmail = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");
            }

            return usageModel;
        }

        private PromotionRuleUsageHistoryModel MapParentDiscountUsageHistory(DiscountUsageHistory historyEntry, string parentDiscountName)
        {
            var usageModel = new PromotionRuleUsageHistoryModel
            {
                Id = historyEntry.Id,
                CreatedOn = _dateTimeHelper.ConvertToUserTime(historyEntry.CreatedOnUtc, DateTimeKind.Utc),
                HistorySource = parentDiscountName
            };

            var order = _orderService.GetOrderById(historyEntry.OrderId);
            if (order != null && !order.Deleted)
            {
                usageModel.OrderId = order.Id;
                usageModel.CustomOrderNumber = order.Id.ToString();
                usageModel.OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal, true, false);

                var customer = _customerService.GetCustomerById(order.CustomerId);
                usageModel.CustomerEmail = customer != null && !string.IsNullOrEmpty(customer.Email)
                    ? customer.Email
                    : _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");
            }
            else
            {
                usageModel.CustomOrderNumber = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Order.Deleted");
                usageModel.CustomerEmail = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");
            }

            return usageModel;
        }

        public IList<PromotionRuleProductModel> PrepareRuleProductList(PromotionRuleProductSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var ruleProducts = _promotionRuleService.GetRuleProductsByRuleId(searchModel.PromotionRuleId);
            var paged = PageItems(ruleProducts, pageIndex, pageSize, out totalCount);
            var models = new List<PromotionRuleProductModel>();
            foreach (var x in paged)
            {
                var product = _productService.GetProductById(x.ProductId);
                var category = x.CategoryId.HasValue ? _categoryService.GetCategoryById(x.CategoryId.Value) : null;
                var manufacturer = x.ManufacturerId.HasValue ? _manufacturerService.GetManufacturerById(x.ManufacturerId.Value) : null;
                var vendor = x.VendorId.HasValue ? _vendorService.GetVendorById(x.VendorId.Value) : null;

                var sourceType = RuleProductSourceType.Product;
                var sourceName = product != null ? product.Name : string.Empty;

                if (x.CategoryId.HasValue && x.CategoryId.Value > 0)
                {
                    sourceType = RuleProductSourceType.Category;
                    sourceName = category != null ? category.Name : string.Empty;
                }
                else if (x.ManufacturerId.HasValue && x.ManufacturerId.Value > 0)
                {
                    sourceType = RuleProductSourceType.Manufacturer;
                    sourceName = manufacturer != null ? manufacturer.Name : string.Empty;
                }
                else if (x.VendorId.HasValue && x.VendorId.Value > 0)
                {
                    sourceType = RuleProductSourceType.Vendor;
                    sourceName = vendor != null ? vendor.Name : string.Empty;
                }

                models.Add(new PromotionRuleProductModel
                {
                    Id = x.Id,
                    PromotionRuleId = x.PromotionRuleId,
                    ProductId = x.ProductId,
                    ProductName = x.IsAllProducts ? "All Products" : (product != null ? product.Name : string.Empty),
                    SourceTypeName = x.IsAllProducts ? "All Products" : sourceType.GetLocalizedEnum(_localizationService, _workContext),
                    SourceName = x.IsAllProducts ? "All Products" : sourceName,
                    MinQuantity = x.MinQuantity,
                    MaxQuantity = x.MaxQuantity,
                    IsAllProducts = x.IsAllProducts,
                    IsRewardProduct = x.IsRewardProduct,
                    RewardAttributeSelectionTypeId = x.RewardAttributeSelectionTypeId,
                    RewardAttributeSelectionTypeName = x.RewardAttributeSelectionType.GetLocalizedEnum(_localizationService, _workContext),
                    RewardAttributeValueIds = x.RewardAttributeValueIds,
                    AttributeFilterSummary = BuildRuleProductAttributeFilterSummary(x),
                    CategoryId = x.CategoryId,
                    ManufacturerId = x.ManufacturerId,
                    VendorId = x.VendorId
                });
            }

            return models;
        }

        public IList<PromotionRuleExcludedProductModel> PrepareExcludedProductList(PromotionRuleExcludedProductSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var excludedProducts = _excludedProductService.GetExcludedProductsPaged(
                searchModel.PromotionRuleId,
                pageIndex < 0 ? 0 : pageIndex,
                pageSize <= 0 ? 15 : pageSize);
            totalCount = excludedProducts.TotalCount;

            var models = new List<PromotionRuleExcludedProductModel>();
            foreach (var x in excludedProducts)
            {
                var product = _productService.GetProductById(x.ProductId);
                var productPicture = _productService.GetProductPicturesByProductId(x.ProductId).FirstOrDefault();
                var pictureUrl = string.Empty;
                if (productPicture != null && _pictureService != null)
                    pictureUrl = _pictureService.GetPictureUrl(productPicture.PictureId, 75);

                models.Add(new PromotionRuleExcludedProductModel
                {
                    Id = x.Id,
                    PromotionRuleId = x.PromotionRuleId,
                    ProductId = x.ProductId,
                    ProductName = product != null ? product.Name : string.Empty,
                    Sku = product != null ? product.Sku : string.Empty,
                    Price = product != null ? product.Price : 0,
                    ProductPictureUrl = pictureUrl,
                    CreatedOnUtc = x.CreatedOnUtc
                });
            }

            return models;
        }

        public IList<PromotionRuleTierModel> PrepareRuleTierList(PromotionRuleTierSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var tiers = _promotionRuleService.GetRuleTiersByRuleId(searchModel.PromotionRuleId);
            var paged = PageItems(tiers, pageIndex, pageSize, out totalCount);
            return paged.Select(x => PrepareRuleTierModel(null, x, true)).ToList();
        }

        public PromotionRuleTierModel PrepareRuleTierModel(PromotionRuleTierModel model, PromotionRuleTier tier, bool excludeProperties = false)
        {
            if (tier != null)
            {
                if (model == null)
                    model = tier.ToModel();

                model.DiscountTypeName = tier.DiscountType.GetLocalizedEnum(_localizationService, _workContext);

                if (tier.RewardProductId.HasValue)
                {
                    var rewardProduct = _productService.GetProductById(tier.RewardProductId.Value);
                    model.RewardProductName = rewardProduct != null ? rewardProduct.Name : string.Empty;
                }

                var mappedRuleProductIds = _promotionRuleService.GetMappedRuleProductIdsByTierId(tier.Id);
                model.SelectedRuleProductIds = mappedRuleProductIds.ToList();
                model.AppliesToRuleProductsSummary = BuildTierAppliesToRuleProductsSummary(tier.PromotionRuleId, mappedRuleProductIds);
            }

            var promotionRuleId = tier != null ? tier.PromotionRuleId : (model != null ? model.PromotionRuleId : 0);
            if (promotionRuleId > 0)
            {
                var promotionRule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
                if (promotionRule != null)
                {
                    if (model == null)
                        model = new PromotionRuleTierModel();
                    model.PromotionRuleId = promotionRuleId;
                    model.RuleTypeId = (int)promotionRule.RuleType;
                }
            }

            if (!excludeProperties && model != null)
            {
                PrepareDiscountTypeSelectList(model.AvailableDiscountTypes);
                PrepareProductSelectList(model.AvailableProducts);
                PrepareTierRuleProductSelectList(model.AvailableRuleProducts, model.PromotionRuleId, model.SelectedRuleProductIds);
            }

            if (model != null && model.PromotionRuleId > 0 && string.IsNullOrWhiteSpace(model.AppliesToRuleProductsSummary))
            {
                model.AppliesToRuleProductsSummary = BuildTierAppliesToRuleProductsSummary(
                    model.PromotionRuleId,
                    model.SelectedRuleProductIds);
            }

            return model ?? new PromotionRuleTierModel();
        }

        public IList<PromotionRuleConditionModel> PrepareRuleConditionList(PromotionRuleConditionSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                throw new ArgumentNullException("searchModel");

            var conditions = _promotionRuleService.GetRuleConditionsByRuleId(searchModel.PromotionRuleId);
            var orderedConditions = conditions
                .OrderBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
                .ThenBy(x => x.Id)
                .ToList();

            var firstConditionIdsByGroup = new HashSet<int>(orderedConditions
                .GroupBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
                .Select(x => x.First().Id));

            var paged = PageItems(orderedConditions, pageIndex, pageSize, out totalCount);
            var conditionModels = new List<PromotionRuleConditionModel>();
            foreach (var condition in paged)
            {
                var conditionModel = PrepareRuleConditionModel(null, condition, true);
                if (firstConditionIdsByGroup.Contains(condition.Id))
                    conditionModel.LogicalOperatorName = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator.FirstInGroup");
                conditionModels.Add(conditionModel);
            }

            return conditionModels;
        }

        public PromotionRuleConditionRequirementListModel PrepareRuleConditionRequirementListModel(int promotionRuleId)
        {
            var rule = _promotionRuleService.GetPromotionRuleById(promotionRuleId);
            var ruleTypeId = rule != null ? rule.RuleTypeId : 0;
            var conditions = _promotionRuleService.GetRuleConditionsByRuleId(promotionRuleId);
            return PrepareConditionRequirementListModel(conditions, ruleTypeId);
        }

        public PromotionRuleConditionRequirementListModel PrepareDiscountRequirementConditionListModel(int discountRequirementId)
        {
            var conditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
            return PrepareConditionRequirementListModel(conditions, 0);
        }

        protected virtual PromotionRuleConditionRequirementListModel PrepareConditionRequirementListModel(
            IList<PromotionRuleCondition> conditions,
            int ruleTypeId)
        {
            var orderedConditions = conditions
                .OrderBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
                .ThenBy(x => x.Id)
                .ToList();

            var groupedConditions = orderedConditions
                .GroupBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
                .ToList();

            var groupIds = groupedConditions.Select(x => x.Key).ToList();
            if (!groupIds.Any())
                groupIds.Add(1);

            var model = new PromotionRuleConditionRequirementListModel
            {
                AvailableGroups = groupIds
                    .OrderBy(x => x)
                    .Select(x => new SelectListItem
                    {
                        Value = x.ToString(),
                        Text = "G" + x
                    })
                    .ToList(),
                NextGroupId = groupIds.Max() + 1
            };

            foreach (var group in groupedConditions.OrderBy(x => x.Key))
            {
                var groupConditions = group.ToList();
                var groupId = group.Key;
                var conditionLookup = groupConditions.ToDictionary(x => x.Id);
                var conditionsByParent = groupConditions
                    .GroupBy(x => x.ParentConditionId.HasValue && conditionLookup.ContainsKey(x.ParentConditionId.Value)
                        ? x.ParentConditionId.Value
                        : 0)
                    .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Id).ToList());

                List<PromotionRuleCondition> rootConditions;
                if (!conditionsByParent.TryGetValue(0, out rootConditions))
                    rootConditions = new List<PromotionRuleCondition>();

                var interactionId = rootConditions.Skip(1).Select(x => x.LogicalOperatorId).FirstOrDefault();
                if (!Enum.IsDefined(typeof(ConditionLogicalOperator), interactionId))
                    interactionId = (int)ConditionLogicalOperator.And;

                var groupModel = new PromotionRuleConditionRequirementModel
                {
                    IsGroup = true,
                    GroupId = groupId,
                    RuleName = "G" + groupId,
                    AvailableInteractionTypes = BuildLogicalOperatorSelectList(interactionId)
                };

                groupModel.ChildRequirements = BuildConditionRequirementTree(
                    rootConditions,
                    groupId,
                    ruleTypeId,
                    conditionsByParent,
                    new HashSet<int>()).ToList();
                model.Conditions.Add(groupModel);
            }

            return model;
        }

        protected virtual IList<PromotionRuleConditionRequirementModel> BuildConditionRequirementTree(
            IList<PromotionRuleCondition> siblings,
            int groupId,
            int ruleTypeId,
            Dictionary<int, List<PromotionRuleCondition>> conditionsByParent,
            HashSet<int> visited)
        {
            var items = new List<PromotionRuleConditionRequirementModel>();
            if (siblings == null || siblings.Count == 0)
                return items;

            for (var i = 0; i < siblings.Count; i++)
            {
                var condition = siblings[i];
                if (!visited.Add(condition.Id))
                    continue;

                var conditionModel = PrepareRuleConditionModel(null, condition, true);
                if (ruleTypeId > 0)
                {
                    conditionModel.RuleTypeId = ruleTypeId;
                    conditionModel.Summary = BuildConditionSummary(conditionModel);
                }

                var isLast = i == siblings.Count - 1;
                var item = new PromotionRuleConditionRequirementModel
                {
                    ConditionId = condition.Id,
                    GroupId = groupId,
                    IsGroup = false,
                    RuleName = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Condition.Title"),
                    Summary = conditionModel.Summary,
                    IsLastInGroup = isLast
                };

                if (!isLast)
                {
                    var nextLogicalId = siblings[i + 1].LogicalOperatorId;
                    var nextLogicalOperator = Enum.IsDefined(typeof(ConditionLogicalOperator), nextLogicalId)
                        ? (ConditionLogicalOperator)nextLogicalId
                        : ConditionLogicalOperator.And;
                    item.InteractionType = nextLogicalOperator.GetLocalizedEnum(_localizationService, _workContext);
                }

                List<PromotionRuleCondition> children;
                if (conditionsByParent.TryGetValue(condition.Id, out children) && children.Any())
                {
                    item.ChildRequirements = BuildConditionRequirementTree(
                        children,
                        groupId,
                        ruleTypeId,
                        conditionsByParent,
                        visited).ToList();
                }

                visited.Remove(condition.Id);
                items.Add(item);
            }

            return items;
        }

        public PromotionRuleConditionModel PrepareRuleConditionModel(PromotionRuleConditionModel model, PromotionRuleCondition condition, bool excludeProperties = false)
        {
            int? currentConditionId = condition != null ? condition.Id : (int?)null;
            if (condition != null)
            {
                if (model == null)
                {
                    model = condition.ToModel();
                    model.SelectedCountryCodes = ParseCsvList(condition.RequiredCountryCodesCsv)
                        .Select(x => x.ToUpperInvariant())
                        .ToList();
                    model.SelectedPaymentMethodSystemNames = ParseCsvList(condition.RequiredPaymentMethodsCsv).ToList();
                }

                if (model.RuleTypeId <= 0)
                {
                    var rule = _promotionRuleService.GetPromotionRuleById(condition.PromotionRuleId);
                    if (rule != null)
                        model.RuleTypeId = rule.RuleTypeId;
                }

                model.LogicalOperatorName = condition.LogicalOperator.GetLocalizedEnum(_localizationService, _workContext);
                model.ConditionOperatorName = condition.ConditionOperator.GetLocalizedEnum(_localizationService, _workContext);
                model.ConditionRestrictionTypeName = condition.ConditionRestrictionType.GetLocalizedEnum(_localizationService, _workContext);
                model.ConditionSourceTypeName = condition.ConditionSourceTypeId > 0
                    ? condition.ConditionSourceType.GetLocalizedEnum(_localizationService, _workContext)
                    : string.Empty;
                model.AttributeMatchModeName = condition.AttributeMatchMode.GetLocalizedEnum(_localizationService, _workContext);

                if (condition.ParentConditionId.HasValue && condition.ParentConditionId.Value > 0)
                {
                    var parentCondition = _promotionRuleService.GetRuleConditionById(condition.ParentConditionId.Value);
                    model.ParentConditionName = parentCondition != null ? "#" + parentCondition.Id : string.Empty;
                }
                else
                {
                    model.ParentConditionName = string.Empty;
                }

                if (condition.RequiredProductId.HasValue)
                {
                    var product = _productService.GetProductById(condition.RequiredProductId.Value);
                    model.RequiredProductName = product != null ? product.Name : string.Empty;
                }

                if (condition.ExcludedProductId.HasValue)
                {
                    var product = _productService.GetProductById(condition.ExcludedProductId.Value);
                    model.ExcludedProductName = product != null ? product.Name : string.Empty;
                }

                if (condition.RequiredCategoryId.HasValue)
                {
                    var category = _categoryService.GetCategoryById(condition.RequiredCategoryId.Value);
                    model.RequiredCategoryName = category != null ? category.Name : string.Empty;
                }

                if (condition.RequiredVendorId.HasValue)
                {
                    var vendor = _vendorService.GetVendorById(condition.RequiredVendorId.Value);
                    model.RequiredVendorName = vendor != null ? vendor.Name : string.Empty;
                }

                if (condition.RequiredCustomerRoleId.HasValue)
                {
                    var role = _customerService.GetAllCustomerRoles(true)
                        .FirstOrDefault(x => x.Id == condition.RequiredCustomerRoleId.Value);
                    model.RequiredCustomerRoleName = role != null ? role.Name : string.Empty;
                }

                HydrateConditionSourceBuilder(model);
                model.Summary = BuildConditionSummary(model);
            }
            else if (model != null)
            {
                if (string.IsNullOrWhiteSpace(model.ConditionSourceBuilderJson))
                {
                    model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel
                    {
                        Rows = model.SourceBuilderRows,
                        Tokens = model.SelectedSourceTokens
                    });
                }
            }

            if (!excludeProperties && model != null)
            {
                PrepareLogicalOperatorSelectList(model.AvailableLogicalOperators);
                PrepareConditionOperatorSelectList(model.AvailableConditionOperators);
                PrepareCategorySelectList(model.AvailableCategories);
                PrepareVendorSelectList(model.AvailableVendors);
                PrepareCustomerRoleSelectList(model.AvailableCustomerRoles);
                PrepareCountrySelectList(model.AvailableCountries, model.SelectedCountryCodes);
                PreparePaymentMethodSelectList(model.AvailablePaymentMethods, model.SelectedPaymentMethodSystemNames);
                PrepareSpecificationAttributeSelectList(model.AvailableSpecificationAttributes);
                PrepareProductAttributeSelectList(model.AvailableProductAttributes);
                PrepareSourceTokenSelectLists(model.AvailableSourceDeviceTypes, model.AvailableSourceSalesChannels);
                PrepareSpecificationAttributeOptionsLookup(model.AvailableSpecificationAttributeOptionsLookup);
                PrepareProductAttributeValuesLookup(model.AvailableProductAttributeValuesLookup);
                PrepareConditionRestrictionTypeSelectList(model.AvailableConditionRestrictionTypes);
                PrepareConditionSourceTypeSelectList(model.AvailableConditionSourceTypes);
                PrepareAttributeMatchModeSelectList(model.AvailableAttributeMatchModes);
                PrepareParentConditionSelectList(model, currentConditionId.HasValue ? currentConditionId : (model.Id > 0 ? model.Id : (int?)null));
            }

            return model ?? new PromotionRuleConditionModel();
        }

        protected virtual string BuildConditionSummary(PromotionRuleConditionModel model)
        {
            var parts = new List<string>();

            var hasSubtotalCriteria = model.ConditionOperatorId > 0 &&
                                     (model.RuleTypeId == (int)PromotionRuleType.SubtotalBased ||
                                      model.MinValue != 0 ||
                                      model.MaxValue != 0);
            if (hasSubtotalCriteria)
            {
                var conditionLabel = _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator");
                var operatorText = model.ConditionOperatorName ?? string.Empty;
                var valueText = model.ConditionOperatorId == (int)ConditionOperator.Between
                    ? model.MinValue.ToString("0.####") + " - " + model.MaxValue.ToString("0.####")
                    : model.MinValue.ToString("0.####");
                parts.Add(conditionLabel + ": " + operatorText + " " + valueText);
            }

            if (!string.IsNullOrWhiteSpace(model.RequiredProductName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct") + ": " + model.RequiredProductName);

            if (!string.IsNullOrWhiteSpace(model.ExcludedProductName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct") + ": " + model.ExcludedProductName);

            if (!string.IsNullOrWhiteSpace(model.RequiredCategoryName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory") + ": " + model.RequiredCategoryName);

            if (!string.IsNullOrWhiteSpace(model.RequiredVendorName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor") + ": " + model.RequiredVendorName);

            if (!string.IsNullOrWhiteSpace(model.RequiredCustomerRoleName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole") + ": " + model.RequiredCustomerRoleName);

            if (model.IsFirstOrderOnly)
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsFirstOrderOnly"));

            if (model.IsNewCustomerOnly)
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsNewCustomerOnly"));

            var sourceSummary = BuildConditionSourceSummary(model);
            if (!string.IsNullOrWhiteSpace(sourceSummary) && model.ConditionSourceTypeId > 0)
            {
                var restrictionText = model.ConditionRestrictionTypeName ?? string.Empty;
                var sourceText = model.ConditionSourceTypeName ?? string.Empty;
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData") + ": " + restrictionText + " " + sourceText + " = " + sourceSummary);
            }

            if (!string.IsNullOrWhiteSpace(model.ParentConditionName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition") + ": " + model.ParentConditionName);

            if (model.QuantityMin.HasValue || model.QuantityMax.HasValue)
            {
                var qtyText = model.QuantityMin.HasValue && model.QuantityMax.HasValue
                    ? model.QuantityMin.Value + " - " + model.QuantityMax.Value
                    : model.QuantityMin.HasValue
                        ? model.QuantityMin.Value + "+"
                        : "<= " + model.QuantityMax.Value;
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityRange") + ": " + qtyText);
            }

            if (!string.IsNullOrWhiteSpace(model.RequiredCountryCodesCsv))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCountryCodesCsv") + ": " + BuildCountrySummary(model.RequiredCountryCodesCsv));

            if (!string.IsNullOrWhiteSpace(model.RequiredPaymentMethodsCsv))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredPaymentMethodsCsv") + ": " + BuildPaymentMethodSummary(model.RequiredPaymentMethodsCsv));

            if (model.RequiredOrderCountMin.HasValue || model.RequiredOrderCountMax.HasValue)
            {
                var orderText = model.RequiredOrderCountMin.HasValue && model.RequiredOrderCountMax.HasValue
                    ? model.RequiredOrderCountMin.Value + " - " + model.RequiredOrderCountMax.Value
                    : model.RequiredOrderCountMin.HasValue
                        ? model.RequiredOrderCountMin.Value + "+"
                        : "<= " + model.RequiredOrderCountMax.Value;
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountRange") + ": " + orderText);
            }

            if (model.RequireSameLineMatch)
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequireSameLineMatch"));

            if (!string.IsNullOrWhiteSpace(model.AttributeMatchModeName))
                parts.Add(_localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode") + ": " + model.AttributeMatchModeName);

            if (!parts.Any())
                return _localizationService.GetResource("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Summary.Empty");

            return string.Join(" | ", parts);
        }

        public PromotionRuleRewardAttributePopupModel PrepareRewardAttributePopupModel(int productId, string selectedValueIds)
        {
            var model = new PromotionRuleRewardAttributePopupModel
            {
                ProductId = productId,
                RewardAttributeValueIds = selectedValueIds ?? string.Empty
            };

            var product = _productService.GetProductById(productId);
            if (product == null || product.Deleted)
                return model;

            var selectedIds = ParseIdSet(selectedValueIds ?? string.Empty);
            PrepareRewardProductAttributeModels(model.ProductAttributes, product, selectedIds);
            return model;
        }

        public PromotionRuleSelectProductSearchModel PrepareSelectProductSearchModel(PromotionRuleSelectProductSearchModel searchModel)
        {
            if (searchModel == null)
                searchModel = new PromotionRuleSelectProductSearchModel();

            PrepareCategorySelectList(searchModel.AvailableCategories, true);
            PrepareManufacturerSelectList(searchModel.AvailableManufacturers, true);
            PrepareVendorSelectList(searchModel.AvailableVendors, true);

            searchModel.AvailableStores.Clear();
            searchModel.AvailableStores.Add(new SelectListItem { Value = "0", Text = _localizationService.GetResource("Admin.Common.All") });
            foreach (var store in _storeService.GetAllStores())
            {
                searchModel.AvailableStores.Add(new SelectListItem
                {
                    Value = store.Id.ToString(),
                    Text = store.Name
                });
            }

            searchModel.AvailableProductTypes.Clear();
            searchModel.AvailableProductTypes.Add(new SelectListItem { Value = "0", Text = _localizationService.GetResource("Admin.Common.All") });
            foreach (ProductType productType in Enum.GetValues(typeof(ProductType)))
            {
                searchModel.AvailableProductTypes.Add(new SelectListItem
                {
                    Value = ((int)productType).ToString(),
                    Text = productType.GetLocalizedEnum(_localizationService, _workContext)
                });
            }

            if (searchModel.PageSize <= 0)
                searchModel.PageSize = 15;
            return searchModel;
        }

        public IList<PromotionRuleSelectProductModel> PrepareSelectProductList(PromotionRuleSelectProductSearchModel searchModel, int pageIndex, int pageSize, out int totalCount)
        {
            if (searchModel == null)
                searchModel = new PromotionRuleSelectProductSearchModel();

            IList<int> categoryIds = null;
            if (searchModel.SearchCategoryId > 0)
                categoryIds = new List<int> { searchModel.SearchCategoryId };

            ProductType? productType = null;
            if (searchModel.SearchProductTypeId > 0)
                productType = (ProductType)searchModel.SearchProductTypeId;

            var products = _productService.SearchProducts(
                pageIndex: pageIndex < 0 ? 0 : pageIndex,
                pageSize: pageSize <= 0 ? 15 : pageSize,
                categoryIds: categoryIds,
                manufacturerId: searchModel.SearchManufacturerId,
                storeId: searchModel.SearchStoreId,
                vendorId: searchModel.SearchVendorId,
                productType: productType,
                keywords: searchModel.SearchProductName,
                showHidden: true);

            totalCount = products.TotalCount;
            return products.Select(product => new PromotionRuleSelectProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Published = product.Published
            }).ToList();
        }

        #endregion
    }
}