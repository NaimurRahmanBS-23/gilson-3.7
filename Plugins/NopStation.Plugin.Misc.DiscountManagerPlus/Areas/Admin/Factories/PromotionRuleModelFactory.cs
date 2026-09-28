using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Data;
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
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Extensions;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Factories;

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
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPriceCalculationService _priceCalculationService;
    private readonly IProductService _productService;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
    private readonly ISpecificationAttributeService _specificationAttributeService;
    private readonly IStoreService _storeService;
    private readonly IStoreContext _storeContext;
    private readonly ITaxService _taxService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IVendorService _vendorService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly IStoreMappingSupportedModelFactory _storeMappingSupportedModelFactory;
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
        IPaymentPluginManager paymentPluginManager,
        IPriceFormatter priceFormatter,
        IPriceCalculationService priceCalculationService,
        IProductService productService,
        IProductAttributeService productAttributeService,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        ISpecificationAttributeService specificationAttributeService,
        IStoreService storeService,
        IStoreContext storeContext,
        ITaxService taxService,
        IManufacturerService manufacturerService,
        IVendorService vendorService,
        IPromotionRuleService promotionRuleService,
        IStoreMappingSupportedModelFactory storeMappingSupportedModelFactory,
        IWorkContext workContext,
        IPromotionRuleExcludedProductService excludedProductService,
        IPictureService pictureService = null)
    {
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _categoryService = categoryService;
        _customerService = customerService;
        _countryService = countryService;
        _discountService = discountService;
        _orderService = orderService;
        _paymentPluginManager = paymentPluginManager;
        _priceFormatter = priceFormatter;
        _priceCalculationService = priceCalculationService;
        _productService = productService;
        _productAttributeService = productAttributeService;
        _productAttributeMappingRepository = productAttributeMappingRepository;
        _specificationAttributeService = specificationAttributeService;
        _storeService = storeService;
        _storeContext = storeContext;
        _taxService = taxService;
        _manufacturerService = manufacturerService;
        _vendorService = vendorService;
        _promotionRuleService = promotionRuleService;
        _storeMappingSupportedModelFactory = storeMappingSupportedModelFactory;
        _workContext = workContext;
        _excludedProductService = excludedProductService;
        _pictureService = pictureService;
    }

    #endregion

    #region Utilities

    protected virtual async Task PrepareRuleTypeSelectListAsync(IList<SelectListItem> items)
    {
        foreach (var ruleType in Enum.GetValues<PromotionRuleType>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)ruleType).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(ruleType)
            });
        }
    }

    protected virtual async Task PrepareDiscountTypeSelectListAsync(IList<SelectListItem> items)
    {
        foreach (var discountType in Enum.GetValues<DiscountType>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)discountType).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(discountType)
            });
        }
    }

    protected virtual async Task PrepareDiscountScopeSelectListAsync(IList<SelectListItem> items)
    {
        foreach (var discountScope in Enum.GetValues<DiscountScope>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)discountScope).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(discountScope)
            });
        }
    }

    protected virtual async Task PrepareLinkedDiscountSelectListAsync(IList<SelectListItem> items)
    {
        items.Clear();
        items.Add(new SelectListItem
        {
            Value = string.Empty,
            Text = await _localizationService.GetResourceAsync("Admin.Common.None")
        });

        var discounts = await _discountService.GetAllDiscountsAsync(showHidden: true, isActive: null);
        foreach (var discount in discounts.OrderBy(x => x.Name).ThenBy(x => x.Id))
        {
            var typeText = await _localizationService.GetLocalizedEnumAsync(discount.DiscountType);
            var couponText = discount.RequiresCouponCode && !string.IsNullOrWhiteSpace(discount.CouponCode)
                ? $" | {discount.CouponCode}"
                : string.Empty;

            items.Add(new SelectListItem
            {
                Value = discount.Id.ToString(),
                Text = $"{discount.Name} (#{discount.Id}) | {typeText}{couponText}"
            });
        }
    }

    protected virtual async Task PrepareConditionOperatorSelectListAsync(IList<SelectListItem> items)
    {
        foreach (var op in Enum.GetValues<ConditionOperator>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)op).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(op)
            });
        }
    }

    protected virtual async Task PrepareLogicalOperatorSelectListAsync(IList<SelectListItem> items)
    {
        foreach (var op in Enum.GetValues<ConditionLogicalOperator>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)op).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(op)
            });
        }
    }

    protected virtual async Task<IList<SelectListItem>> BuildLogicalOperatorSelectListAsync(int selectedId)
    {
        var items = new List<SelectListItem>();
        await PrepareLogicalOperatorSelectListAsync(items);
        var selectedValue = selectedId.ToString();
        foreach (var item in items)
            item.Selected = item.Value == selectedValue;
        return items;
    }

    protected virtual HashSet<int> ParseIdSet(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<int>();

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();
    }

    protected virtual async Task PrepareRewardProductAttributeModelsAsync(
        IList<PromotionRuleRewardAttributePopupModel.RewardProductAttributeModel> models,
        Product product,
        HashSet<int> selectedValueIds)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(product);

        var attributes = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        foreach (var attribute in attributes)
        {
            var attributeDefinition = await _productAttributeService.GetProductAttributeByIdAsync(attribute.ProductAttributeId);
            var attributeModel = new PromotionRuleRewardAttributePopupModel.RewardProductAttributeModel
            {
                Id = attribute.Id,
                ProductAttributeId = attribute.ProductAttributeId,
                Name = attributeDefinition?.Name ?? string.Empty,
                TextPrompt = attribute.TextPrompt,
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlType,
                HasCondition = !string.IsNullOrEmpty(attribute.ConditionAttributeXml)
            };

            if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
            {
                attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            if (attribute.ShouldHaveValues())
            {
                var attributeValues = await _productAttributeService.GetProductAttributeValuesAsync(attribute.Id);
                foreach (var attributeValue in attributeValues)
                {
                    var (priceAdjustment, _) = await _taxService.GetProductPriceAsync(product,
                        await _priceCalculationService.GetProductAttributeValuePriceAdjustmentAsync(product, attributeValue, customer, store));

                    var priceAdjustmentStr = string.Empty;
                    if (priceAdjustment != 0)
                    {
                        if (attributeValue.PriceAdjustmentUsePercentage)
                        {
                            priceAdjustmentStr = attributeValue.PriceAdjustment.ToString("G29");
                            priceAdjustmentStr = priceAdjustment > 0 ? $"+{priceAdjustmentStr}%" : $"{priceAdjustmentStr}%";
                        }
                        else
                        {
                            priceAdjustmentStr = priceAdjustment > 0
                                ? $"+{await _priceFormatter.FormatPriceAsync(priceAdjustment, false, false)}"
                                : $"-{await _priceFormatter.FormatPriceAsync(-priceAdjustment, false, false)}";
                        }
                    }

                    attributeModel.Values.Add(new PromotionRuleRewardAttributePopupModel.RewardProductAttributeValueModel
                    {
                        Id = attributeValue.Id,
                        Name = attributeValue.Name,
                        IsPreSelected = selectedValueIds.Contains(attributeValue.Id) || attributeValue.IsPreSelected,
                        CustomerEntersQty = attributeValue.CustomerEntersQty,
                        Quantity = attributeValue.Quantity,
                        PriceAdjustment = priceAdjustmentStr,
                        PriceAdjustmentValue = priceAdjustment
                    });
                }
            }

            models.Add(attributeModel);
        }
    }

    protected virtual async Task PrepareConditionRestrictionTypeSelectListAsync(IList<SelectListItem> items)
    {
        foreach (var restriction in Enum.GetValues<ConditionRestrictionType>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)restriction).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(restriction)
            });
        }
    }

    protected virtual async Task PrepareConditionSourceTypeSelectListAsync(IList<SelectListItem> items)
    {
        items.Add(new SelectListItem
        {
            Value = "0",
            Text = await _localizationService.GetResourceAsync("Admin.Common.Select")
        });

        foreach (var sourceType in Enum.GetValues<ConditionSourceType>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)sourceType).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(sourceType)
            });
        }
    }

    protected virtual async Task PrepareAttributeMatchModeSelectListAsync(IList<SelectListItem> items)
    {
        items.Clear();
        foreach (var mode in Enum.GetValues<AttributeMatchMode>())
        {
            items.Add(new SelectListItem
            {
                Value = ((int)mode).ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(mode)
            });
        }
    }

    protected virtual async Task PrepareParentConditionSelectListAsync(
        PromotionRuleConditionModel model,
        int? currentConditionId = null)
    {
        model.AvailableParentConditions.Clear();
        model.AvailableParentConditions.Add(new SelectListItem
        {
            Value = string.Empty,
            Text = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.None")
        });

        if (model.PromotionRuleId <= 0 && model.DiscountRequirementId <= 0)
            return;

        var conditions = model.DiscountRequirementId > 0
            ? await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(model.DiscountRequirementId)
            : await _promotionRuleService.GetRuleConditionsByRuleIdAsync(model.PromotionRuleId);
        var selectableParents = conditions
            .Where(x => x.Id != currentConditionId && (x.ConditionGroup <= 0 ? 1 : x.ConditionGroup) == model.ConditionGroup)
            .OrderBy(x => x.Id)
            .ToList();

        foreach (var parentCondition in selectableParents)
        {
            model.AvailableParentConditions.Add(new SelectListItem
            {
                Value = parentCondition.Id.ToString(),
                Text = $"#{parentCondition.Id}"
            });
        }
    }

    protected virtual async Task PrepareProductSelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
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

        var products = await _productService.SearchProductsAsync(pageIndex: 0, pageSize: 2000, showHidden: true);
        foreach (var product in products)
        {
            items.Add(new SelectListItem
            {
                Value = product.Id.ToString(),
                Text = $"{product.Name} (#{product.Id})"
            });
        }
    }

    protected virtual async Task<string> BuildRuleProductDisplayNameAsync(PromotionRuleProduct ruleProduct)
    {
        if (ruleProduct == null)
            return string.Empty;

        var sourceLabel = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceName");
        var quantityLabel = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MinQuantity");
        var maxQuantityLabel = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity");

        string sourceValue;
        if (ruleProduct.ProductId > 0)
        {
            var product = await _productService.GetProductByIdAsync(ruleProduct.ProductId);
            sourceValue = $"{product?.Name ?? ruleProduct.ProductId.ToString()} (#{ruleProduct.ProductId})";
        }
        else if (ruleProduct.CategoryId.HasValue && ruleProduct.CategoryId.Value > 0)
        {
            var category = await _categoryService.GetCategoryByIdAsync(ruleProduct.CategoryId.Value);
            sourceValue = $"{category?.Name ?? ruleProduct.CategoryId.Value.ToString()} (Category)";
        }
        else if (ruleProduct.ManufacturerId.HasValue && ruleProduct.ManufacturerId.Value > 0)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(ruleProduct.ManufacturerId.Value);
            sourceValue = $"{manufacturer?.Name ?? ruleProduct.ManufacturerId.Value.ToString()} (Manufacturer)";
        }
        else if (ruleProduct.VendorId.HasValue && ruleProduct.VendorId.Value > 0)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(ruleProduct.VendorId.Value);
            sourceValue = $"{vendor?.Name ?? ruleProduct.VendorId.Value.ToString()} (Vendor)";
        }
        else
        {
            sourceValue = await _localizationService.GetResourceAsync("Admin.Common.None");
        }

        var minQuantity = ruleProduct.MinQuantity > 0 ? ruleProduct.MinQuantity : 1;
        var maxQuantity = ruleProduct.MaxQuantity > 0 ? ruleProduct.MaxQuantity.ToString() : "Unlimited";
        return $"#{ruleProduct.Id} | {sourceLabel}: {sourceValue} | {quantityLabel}: {minQuantity} | {maxQuantityLabel}: {maxQuantity}";
    }

    protected virtual async Task PrepareTierRuleProductSelectListAsync(
        IList<SelectListItem> items,
        int promotionRuleId,
        IList<int> selectedRuleProductIds)
    {
        items.Clear();
        if (promotionRuleId <= 0)
            return;

        var selectedIdSet = selectedRuleProductIds?.ToHashSet() ?? new HashSet<int>();
        var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(promotionRuleId);
        foreach (var ruleProduct in ruleProducts.Where(x => !x.IsRewardProduct).OrderBy(x => x.Id))
        {
            items.Add(new SelectListItem
            {
                Value = ruleProduct.Id.ToString(),
                Text = await BuildRuleProductDisplayNameAsync(ruleProduct),
                Selected = selectedIdSet.Contains(ruleProduct.Id)
            });
        }
    }

    protected virtual async Task<string> BuildTierAppliesToRuleProductsSummaryAsync(
        int promotionRuleId,
        IList<int> mappedRuleProductIds)
    {
        var selectedIds = mappedRuleProductIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();
        if (!selectedIds.Any())
            return await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts.All");

        var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(promotionRuleId);
        var mappedRuleProducts = ruleProducts.Where(x => selectedIds.Contains(x.Id)).OrderBy(x => x.Id).ToList();
        if (!mappedRuleProducts.Any())
            return await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts.All");

        var names = new List<string>();
        foreach (var ruleProduct in mappedRuleProducts.Take(3))
        {
            names.Add(await BuildRuleProductDisplayNameAsync(ruleProduct));
        }

        if (mappedRuleProducts.Count > 3)
            names.Add($"+{mappedRuleProducts.Count - 3}");

        return string.Join("; ", names);
    }

    protected virtual async Task PrepareCategorySelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
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

    protected virtual async Task PrepareVendorSelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
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

    protected virtual async Task PrepareCustomerRoleSelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
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

        var roles = await _customerService.GetAllCustomerRolesAsync(true);
        foreach (var role in roles)
        {
            items.Add(new SelectListItem
            {
                Value = role.Id.ToString(),
                Text = role.Name
            });
        }
    }

    protected virtual async Task PrepareCountrySelectListAsync(IList<SelectListItem> items, IList<string> selectedCountryCodes = null)
    {
        items.Clear();

        var selectedCodes = (selectedCountryCodes ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var countries = await _countryService.GetAllCountriesForBillingAsync(showHidden: true);
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

    protected virtual async Task<string> BuildRuleProductAttributeFilterSummaryAsync(PromotionRuleProduct ruleProduct)
    {
        if (ruleProduct == null || ruleProduct.ProductId <= 0)
            return string.Empty;

        if (ruleProduct.RewardAttributeSelectionType != RewardAttributeSelectionType.SpecificValues ||
            string.IsNullOrWhiteSpace(ruleProduct.RewardAttributeValueIds))
        {
            return await _localizationService.GetLocalizedEnumAsync(RewardAttributeSelectionType.Any);
        }

        var valueIds = ParseCsvList(ruleProduct.RewardAttributeValueIds)
            .Select(x => int.TryParse(x, out var valueId) ? valueId : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        if (!valueIds.Any())
            return await _localizationService.GetLocalizedEnumAsync(RewardAttributeSelectionType.Any);

        var summaries = new List<string>();
        foreach (var valueId in valueIds)
        {
            var value = await _productAttributeService.GetProductAttributeValueByIdAsync(valueId);
            if (value == null)
                continue;

            var mapping = await _productAttributeService.GetProductAttributeMappingByIdAsync(value.ProductAttributeMappingId);
            if (mapping == null)
                continue;

            var attribute = await _productAttributeService.GetProductAttributeByIdAsync(mapping.ProductAttributeId);
            var attributeName = !string.IsNullOrWhiteSpace(mapping.TextPrompt)
                ? mapping.TextPrompt
                : attribute?.Name;

            summaries.Add(!string.IsNullOrWhiteSpace(attributeName)
                ? $"{attributeName}: {value.Name}"
                : value.Name);
        }

        return summaries.Any()
            ? string.Join("; ", summaries.Distinct(StringComparer.OrdinalIgnoreCase))
            : await _localizationService.GetLocalizedEnumAsync(RewardAttributeSelectionType.Any);
    }

    protected virtual async Task PrepareManufacturerSelectListAsync(IList<SelectListItem> items, bool includeEmptyItem = true)
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

    protected virtual async Task PrepareSpecificationAttributeSelectListAsync(IList<SelectListItem> items)
    {
        items.Clear();

        foreach (var attribute in await _specificationAttributeService.GetSpecificationAttributesWithOptionsAsync())
        {
            items.Add(new SelectListItem
            {
                Value = attribute.Id.ToString(),
                Text = attribute.Name
            });
        }
    }

    protected virtual async Task PrepareProductAttributeSelectListAsync(IList<SelectListItem> items)
    {
        items.Clear();

        var attributes = await _productAttributeService.GetAllProductAttributesAsync(pageIndex: 0, pageSize: 2000);
        foreach (var attribute in attributes)
        {
            items.Add(new SelectListItem
            {
                Value = attribute.Id.ToString(),
                Text = attribute.Name
            });
        }
    }

    protected virtual Task PrepareSourceTokenSelectListsAsync(
        IList<SelectListItem> availableDeviceTypes,
        IList<SelectListItem> availableSalesChannels)
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

        return Task.CompletedTask;
    }

    protected virtual async Task PrepareSpecificationAttributeOptionsLookupAsync(
        IDictionary<int, IList<SelectListItem>> lookup)
    {
        lookup.Clear();

        var attributes = await _specificationAttributeService.GetSpecificationAttributesWithOptionsAsync();
        foreach (var attribute in attributes)
        {
            var options = await _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttributeAsync(attribute.Id);
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

    protected virtual async Task PrepareProductAttributeValuesLookupAsync(
        IDictionary<int, IList<SelectListItem>> lookup)
    {
        lookup.Clear();

        var attributes = await _productAttributeService.GetAllProductAttributesAsync(pageIndex: 0, pageSize: 2000);
        foreach (var attribute in attributes)
        {
            var mappings = await _productAttributeMappingRepository.Table
                .Where(x => x.ProductAttributeId == attribute.Id)
                .ToListAsync();

            if (!mappings.Any())
            {
                lookup[attribute.Id] = new List<SelectListItem>();
                continue;
            }

            var values = new List<ProductAttributeValue>();
            foreach (var mapping in mappings)
            {
                var mappingValues = await _productAttributeService.GetProductAttributeValuesAsync(mapping.Id);
                values.AddRange(mappingValues);
            }

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

    protected virtual async Task PreparePaymentMethodSelectListAsync(IList<SelectListItem> items, IList<string> selectedPaymentMethods = null)
    {
        items.Clear();

        var selectedMethods = (selectedPaymentMethods ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var method in (await _paymentPluginManager.LoadAllPluginsAsync()).OrderBy(x => x.PluginDescriptor.FriendlyName))
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
            return Array.Empty<string>();

        return raw
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    protected virtual string BuildCsv(IEnumerable<string> values, bool upperCase = false)
    {
        var normalized = (values ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => upperCase ? x.Trim().ToUpperInvariant() : x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized.Any() ? string.Join(",", normalized) : string.Empty;
    }

    protected virtual async Task HydrateConditionSourceBuilderAsync(PromotionRuleConditionModel model)
    {
        if (model == null)
            return;

        model.SourceBuilderRows ??= new List<ConditionSourceBuilderRowModel>();
        model.SelectedSourceTokens ??= new List<string>();

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
            {
                switch ((ConditionSourceType)model.ConditionSourceTypeId)
                {
                    case ConditionSourceType.Products:
                        if (!productCache.TryGetValue(row.EntryId.Value, out var product))
                        {
                            product = await _productService.GetProductByIdAsync(row.EntryId.Value);
                            if (product != null)
                                productCache[row.EntryId.Value] = product;
                        }

                        row.EntryName = product?.Name ?? $"#{row.EntryId.Value}";
                        break;

                    case ConditionSourceType.Categories:
                        if (!categoryCache.TryGetValue(row.EntryId.Value, out var category))
                        {
                            category = await _categoryService.GetCategoryByIdAsync(row.EntryId.Value);
                            if (category != null)
                                categoryCache[row.EntryId.Value] = category;
                        }

                        row.EntryName = category?.Name ?? $"#{row.EntryId.Value}";
                        break;

                    case ConditionSourceType.Manufacturers:
                        if (!manufacturerCache.TryGetValue(row.EntryId.Value, out var manufacturer))
                        {
                            manufacturer = await _manufacturerService.GetManufacturerByIdAsync(row.EntryId.Value);
                            if (manufacturer != null)
                                manufacturerCache[row.EntryId.Value] = manufacturer;
                        }

                        row.EntryName = manufacturer?.Name ?? $"#{row.EntryId.Value}";
                        break;

                    case ConditionSourceType.Vendors:
                        if (!vendorCache.TryGetValue(row.EntryId.Value, out var vendor))
                        {
                            vendor = await _vendorService.GetVendorByIdAsync(row.EntryId.Value);
                            if (vendor != null)
                                vendorCache[row.EntryId.Value] = vendor;
                        }

                        row.EntryName = vendor?.Name ?? $"#{row.EntryId.Value}";
                        break;

                    case ConditionSourceType.SpecificationAttributeOptions:
                        if (!specAttributeCache.TryGetValue(row.EntryId.Value, out var specAttribute))
                        {
                            specAttribute = await _specificationAttributeService.GetSpecificationAttributeByIdAsync(row.EntryId.Value);
                            if (specAttribute != null)
                                specAttributeCache[row.EntryId.Value] = specAttribute;
                        }

                        row.EntryName = specAttribute?.Name ?? $"#{row.EntryId.Value}";
                        for (var index = 0; index < row.SelectedOptionValues.Count; index++)
                        {
                            if (!int.TryParse(row.SelectedOptionValues[index], out var optionId) || optionId <= 0)
                                continue;

                            if (!specOptionCache.TryGetValue(optionId, out var option))
                            {
                                option = await _specificationAttributeService.GetSpecificationAttributeOptionByIdAsync(optionId);
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
                        if (!productAttributeCache.TryGetValue(row.EntryId.Value, out var productAttribute))
                        {
                            productAttribute = await _productAttributeService.GetProductAttributeByIdAsync(row.EntryId.Value);
                            if (productAttribute != null)
                                productAttributeCache[row.EntryId.Value] = productAttribute;
                        }

                        row.EntryName = productAttribute?.Name ?? $"#{row.EntryId.Value}";
                        var normalizedOptionValues = new List<string>();
                        var normalizedOptionTexts = new List<string>();
                        foreach (var selectedOptionValue in row.SelectedOptionValues)
                        {
                            if (int.TryParse(selectedOptionValue, out var optionId) && optionId > 0)
                            {
                                if (!productAttributeValueCache.TryGetValue(optionId, out var optionValue))
                                {
                                    optionValue = await _productAttributeService.GetProductAttributeValueByIdAsync(optionId);
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

                    case ConditionSourceType.ExpiryDays:
                        if (!productCache.TryGetValue(row.EntryId.Value, out var expiryProduct))
                        {
                            expiryProduct = await _productService.GetProductByIdAsync(row.EntryId.Value);
                            if (expiryProduct != null)
                                productCache[row.EntryId.Value] = expiryProduct;
                        }

                        row.EntryName = expiryProduct?.Name ?? $"#{row.EntryId.Value}";
                        break;
                }
            }
            else if (model.ConditionSourceTypeId == (int)ConditionSourceType.ExpiryDays)
            {
                row.EntryName = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.AnyProduct");
            }
        }

        model.SourceBuilderRows = rows;
        model.ConditionSourceBuilderJson = ConditionSourceBuilderHelper.SerializeBuilderStateJson(new ConditionSourceBuilderStateModel
        {
            Rows = rows
        });
    }

    protected virtual async Task<string> BuildConditionSourceSummaryAsync(PromotionRuleConditionModel model)
    {
        if (model.ConditionSourceTypeId <= 0)
            return string.Empty;

        if (ConditionSourceBuilderHelper.IsSessionSourceType(model.ConditionSourceTypeId))
        {
            var tokens = (model.SelectedSourceTokens ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!tokens.Any())
                return string.Empty;

            return string.Join(", ", tokens);
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
                    ? $"#{row.EntryId.Value}"
                    : await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.AnyProduct");

            if (sourceType == ConditionSourceType.SpecificationAttributeOptions ||
                sourceType == ConditionSourceType.ProductAttributeValues)
            {
                var optionTexts = row.SelectedOptionTexts?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();
                if (optionTexts.Any())
                    entryName = $"{entryName} [{string.Join(", ", optionTexts)}]";
            }

            if (row.RangeMin.HasValue || row.RangeMax.HasValue)
            {
                var rangeText = row.RangeMin.HasValue && row.RangeMax.HasValue
                    ? $"{row.RangeMin.Value}-{row.RangeMax.Value}"
                    : row.RangeMin.HasValue
                        ? $"{row.RangeMin.Value}+"
                        : $"0-{row.RangeMax.Value}";
                entryName = sourceType == ConditionSourceType.ExpiryDays
                    ? $"{entryName} ({rangeText} days)"
                    : $"{entryName} ({rangeText})";
            }

            summaries.Add(entryName);
        }

        return string.Join("; ", summaries);
    }

    protected virtual async Task<string> BuildCountrySummaryAsync(string csv)
    {
        var codes = ParseCsvList(csv)
            .Select(x => x.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!codes.Any())
            return string.Empty;

        var countries = await _countryService.GetAllCountriesForBillingAsync(showHidden: true);
        var names = countries
            .Where(x => !string.IsNullOrWhiteSpace(x.TwoLetterIsoCode) && codes.Contains(x.TwoLetterIsoCode))
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return names.Any() ? string.Join(", ", names) : string.Join(", ", codes);
    }

    protected virtual async Task<string> BuildPaymentMethodSummaryAsync(string csv)
    {
        var systemNames = ParseCsvList(csv).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!systemNames.Any())
            return string.Empty;

        var names = (await _paymentPluginManager.LoadAllPluginsAsync())
            .Where(x => systemNames.Contains(x.PluginDescriptor.SystemName))
            .Select(x => x.PluginDescriptor.FriendlyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return names.Any() ? string.Join(", ", names) : string.Join(", ", systemNames);
    }

    protected virtual async Task PopulateSetupStatusAsync(PromotionRuleModel model, PromotionRule promotionRule)
    {
        var productsRequired = promotionRule.RuleType is PromotionRuleType.ProductBased or PromotionRuleType.ComboPricing or PromotionRuleType.BuyXGetY;
        var tiersRequired = promotionRule.RuleType == PromotionRuleType.BuyXGetY;
        var conditionsRequired = promotionRule.RuleType is PromotionRuleType.CartCondition or PromotionRuleType.SubtotalBased;

        var productsConfigured = !productsRequired || model.RuleProductCount > 0;
        var tiersConfigured = !tiersRequired || model.RuleTierCount > 0;
        var conditionsConfigured = !conditionsRequired || model.RuleConditionCount > 0;

        var isSetupComplete = productsConfigured && tiersConfigured && conditionsConfigured;
        model.SetupStatus = await _localizationService.GetResourceAsync(
            isSetupComplete
                ? "Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupStatus.Ready"
                : "Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupStatus.NeedsConfiguration");

        if (isSetupComplete)
        {
            model.SetupDetails = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupDetails.Ready");
            return;
        }

        var missingSections = new List<string>();
        if (productsRequired && !productsConfigured)
            missingSections.Add(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Products"));
        if (tiersRequired && !tiersConfigured)
            missingSections.Add(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Tiers"));
        if (conditionsRequired && !conditionsConfigured)
            missingSections.Add(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Conditions"));

        model.SetupDetails = string.Format(
            await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupDetails.Missing"),
            string.Join(", ", missingSections));
    }

    protected virtual async Task PopulateParentDiscountSummaryAsync(PromotionRuleModel model, Nop.Core.Domain.Discounts.Discount discount)
    {
        if (model == null || discount == null)
            return;

        model.ParentDiscountName = $"{discount.Name} (#{discount.Id})";

        var limitationText = await _localizationService.GetLocalizedEnumAsync(discount.DiscountLimitation);
        model.ParentDiscountLimitationSummary = discount.DiscountLimitation switch
        {
            Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesOnly or Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer when discount.LimitationTimes > 0
                => $"{limitationText} ({discount.LimitationTimes})",
            _ => limitationText
        };

        model.ParentDiscountMaximumDiscountAmountDisplay = discount.MaximumDiscountAmount.HasValue
            ? await _priceFormatter.FormatPriceAsync(discount.MaximumDiscountAmount.Value, true, false)
            : await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountAmount.Unlimited");

        model.ParentDiscountMaximumDiscountedQuantityDisplay = discount.MaximumDiscountedQuantity.HasValue &&
                                                               discount.MaximumDiscountedQuantity.Value > 0
            ? discount.MaximumDiscountedQuantity.Value.ToString()
            : await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountedQuantity.Unlimited");

        model.ParentDiscountUsesNativeCustomerLimit =
            discount.DiscountLimitation == Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer;
    }

    protected virtual async Task PopulateParentDiscountSummaryAsync(DiscountDetailsPromotionRulesModel model, Nop.Core.Domain.Discounts.Discount discount)
    {
        if (model == null || discount == null)
            return;

        model.DiscountName = $"{discount.Name} (#{discount.Id})";

        var limitationText = await _localizationService.GetLocalizedEnumAsync(discount.DiscountLimitation);
        model.ParentDiscountLimitationSummary = discount.DiscountLimitation switch
        {
            Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesOnly or Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer when discount.LimitationTimes > 0
                => $"{limitationText} ({discount.LimitationTimes})",
            _ => limitationText
        };

        model.ParentDiscountMaximumDiscountAmountDisplay = discount.MaximumDiscountAmount.HasValue
            ? await _priceFormatter.FormatPriceAsync(discount.MaximumDiscountAmount.Value, true, false)
            : await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountAmount.Unlimited");

        model.ParentDiscountMaximumDiscountedQuantityDisplay = discount.MaximumDiscountedQuantity.HasValue &&
                                                               discount.MaximumDiscountedQuantity.Value > 0
            ? discount.MaximumDiscountedQuantity.Value.ToString()
            : await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountedQuantity.Unlimited");

        model.ParentDiscountUsesNativeCustomerLimit =
            discount.DiscountLimitation == Nop.Core.Domain.Discounts.DiscountLimitationType.NTimesPerCustomer;
    }

    #endregion

    #region Methods

    public async Task<PromotionRuleSearchModel> PreparePromotionRuleSearchModelAsync(PromotionRuleSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        await PrepareRuleTypeSelectListAsync(searchModel.AvailableRuleTypes);
        searchModel.AvailableRuleTypes.Insert(0, new SelectListItem
        {
            Value = "0",
            Text = await _localizationService.GetResourceAsync("Admin.Common.All")
        });

        searchModel.SetGridPageSize();
        return searchModel;
    }

    public async Task<DiscountDetailsPromotionRulesModel> PrepareDiscountDetailsPromotionRulesModelAsync(int discountId)
    {
        var model = new DiscountDetailsPromotionRulesModel
        {
            DiscountId = discountId
        };

        if (discountId > 0)
        {
            var discount = await _discountService.GetDiscountByIdAsync(discountId);
            if (discount != null)
                await PopulateParentDiscountSummaryAsync(model, discount);
        }

        model.SearchModel = await PreparePromotionRuleSearchModelAsync(new PromotionRuleSearchModel
        {
            DiscountId = discountId,
            HideFilters = true
        });

        if (discountId > 0)
        {
            var rules = await _promotionRuleService.GetPromotionRulesByDiscountIdAsync(discountId);
            model.Rules = new List<PromotionRuleModel>();
            foreach (var rule in rules)
                model.Rules.Add(await PreparePromotionRuleModelAsync(null, rule, true));
        }

        return model;
    }

    public async Task<PromotionRuleListModel> PreparePromotionRuleListModelAsync(PromotionRuleSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var rules = await _promotionRuleService.GetAllPromotionRulesAsync(
            name: searchModel.SearchName,
            ruleTypeId: searchModel.SearchRuleTypeId > 0 ? searchModel.SearchRuleTypeId : null,
            isActive: searchModel.SearchIsActive,
            discountId: searchModel.DiscountId,
            pageIndex: searchModel.Page - 1,
            pageSize: searchModel.PageSize);

        var model = await new PromotionRuleListModel().PrepareToGridAsync(searchModel, rules, () =>
            rules.SelectAwait(async x => await PreparePromotionRuleModelAsync(null, x, true)));

        return model;
    }

    public async Task<PromotionRuleModel> PreparePromotionRuleModelAsync(
        PromotionRuleModel model,
        PromotionRule promotionRule,
        bool excludeProperties = false)
    {
        model ??= new PromotionRuleModel();

        if (promotionRule != null)
        {
            if (model.Id == 0)
                model = promotionRule.ToModel<PromotionRuleModel>();

            model.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(promotionRule.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(promotionRule.UpdatedOnUtc, DateTimeKind.Utc);

            model.RuleTypeName = await _localizationService.GetLocalizedEnumAsync(promotionRule.RuleType);
            model.DiscountTypeName = await _localizationService.GetLocalizedEnumAsync(promotionRule.DiscountType);
            model.DiscountScopeName = await _localizationService.GetLocalizedEnumAsync(promotionRule.DiscountScope);
            var parentDiscountId = promotionRule.DiscountId > 0
                ? promotionRule.DiscountId
                : promotionRule.LinkedDiscountId.GetValueOrDefault();
            if (parentDiscountId > 0)
            {
                model.DiscountId = parentDiscountId;
                model.IsDiscountBound = true;
                var linkedDiscount = await _discountService.GetDiscountByIdAsync(parentDiscountId);
                if (linkedDiscount != null)
                    await PopulateParentDiscountSummaryAsync(model, linkedDiscount);

                model.LinkedDiscountName = model.ParentDiscountName;
            }

            if (promotionRule.StartDateUtc.HasValue)
                model.StartDate = await _dateTimeHelper.ConvertToUserTimeAsync(promotionRule.StartDateUtc.Value, DateTimeKind.Utc);

            if (promotionRule.EndDateUtc.HasValue)
                model.EndDate = await _dateTimeHelper.ConvertToUserTimeAsync(promotionRule.EndDateUtc.Value, DateTimeKind.Utc);

            if (promotionRule.UsageWindowStartUtc.HasValue)
                model.UsageWindowStartUtc = await _dateTimeHelper.ConvertToUserTimeAsync(promotionRule.UsageWindowStartUtc.Value, DateTimeKind.Utc);

            if (promotionRule.UsageWindowEndUtc.HasValue)
                model.UsageWindowEndUtc = await _dateTimeHelper.ConvertToUserTimeAsync(promotionRule.UsageWindowEndUtc.Value, DateTimeKind.Utc);

            model.RuleProductCount = (await _promotionRuleService.GetRuleProductsByRuleIdAsync(promotionRule.Id)).Count;
            model.RuleTierCount = (await _promotionRuleService.GetRuleTiersByRuleIdAsync(promotionRule.Id)).Count;
            model.RuleConditionCount = (await _promotionRuleService.GetRuleConditionsByRuleIdAsync(promotionRule.Id)).Count;
            model.ExcludedProductCount = (await _excludedProductService.GetExcludedProductsByRuleIdAsync(promotionRule.Id)).Count;
            await PopulateSetupStatusAsync(model, promotionRule);

            if (!excludeProperties)
            {
                var allowRewardProduct = promotionRule.RuleType == PromotionRuleType.BuyXGetY ||
                                         (promotionRule.RuleType == PromotionRuleType.ProductBased && promotionRule.DiscountType == DiscountType.FreeItem);

                model.RuleProductSearchModel = new PromotionRuleProductSearchModel
                {
                    PromotionRuleId = promotionRule.Id,
                    AllowRewardProduct = allowRewardProduct
                };
                model.RuleProductSearchModel.SetGridPageSize();

                model.RuleTierSearchModel = new PromotionRuleTierSearchModel { PromotionRuleId = promotionRule.Id };
                model.RuleTierSearchModel.SetGridPageSize();

                model.RuleConditionSearchModel = new PromotionRuleConditionSearchModel { PromotionRuleId = promotionRule.Id };
                model.RuleConditionSearchModel.SetGridPageSize();

                model.ExcludedProductSearchModel = new PromotionRuleExcludedProductSearchModel { PromotionRuleId = promotionRule.Id };
                model.ExcludedProductSearchModel.SetGridPageSize();

                model.RuleUsageHistorySearchModel = new PromotionRuleUsageHistorySearchModel
                {
                    PromotionRuleId = promotionRule.Id,
                    UseParentDiscountHistory = model.IsDiscountBound,
                    ParentDiscountName = model.ParentDiscountName
                };
                model.RuleUsageHistorySearchModel.SetGridPageSize();
            }
        }

        if (!excludeProperties)
        {
            await PrepareRuleTypeSelectListAsync(model.AvailableRuleTypes);
            await PrepareDiscountTypeSelectListAsync(model.AvailableDiscountTypes);
            await PrepareDiscountScopeSelectListAsync(model.AvailableDiscountScopes);
            if (!model.IsDiscountBound)
                await PrepareLinkedDiscountSelectListAsync(model.AvailableLinkedDiscounts);

            if (promotionRule != null && !promotionRule.LimitedToStores && promotionRule.LimitedToStore > 0 && !model.SelectedStoreIds.Any())
                model.SelectedStoreIds.Add(promotionRule.LimitedToStore);

            await _storeMappingSupportedModelFactory.PrepareModelStoresAsync(model, promotionRule, false);
        }

        if (model.IsDiscountBound && model.DiscountId > 0 && string.IsNullOrWhiteSpace(model.ParentDiscountName))
        {
            var discount = await _discountService.GetDiscountByIdAsync(model.DiscountId);
            if (discount != null)
            {
                await PopulateParentDiscountSummaryAsync(model, discount);
                model.LinkedDiscountName = model.ParentDiscountName;
            }
        }

        return model ?? new PromotionRuleModel();
    }

    public async Task<PromotionRuleUsageHistoryListModel> PrepareRuleUsageHistoryListModelAsync(PromotionRuleUsageHistorySearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var promotionRule = await _promotionRuleService.GetPromotionRuleByIdAsync(searchModel.PromotionRuleId);
        if (promotionRule == null)
            return new PromotionRuleUsageHistoryListModel();

        var parentDiscountId = promotionRule.DiscountId > 0
            ? promotionRule.DiscountId
            : promotionRule.LinkedDiscountId.GetValueOrDefault();

        if (parentDiscountId > 0)
        {
            var discount = await _discountService.GetDiscountByIdAsync(parentDiscountId);
            var parentDiscountName = discount == null
                ? string.Empty
                : $"{discount.Name} (#{discount.Id})";
            searchModel.ParentDiscountName = parentDiscountName;

            var linkedRuleUsageHistory = await _promotionRuleService.GetRuleUsagesAsync(
                promotionRuleId: searchModel.PromotionRuleId,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize);

            if (linkedRuleUsageHistory.Any())
            {
                searchModel.UseParentDiscountHistory = false;

                return await new PromotionRuleUsageHistoryListModel().PrepareToGridAsync(searchModel, linkedRuleUsageHistory, () =>
                    linkedRuleUsageHistory.SelectAwait(async historyEntry =>
                    {
                        var usageModel = new PromotionRuleUsageHistoryModel
                        {
                            Id = historyEntry.Id,
                            CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(historyEntry.CreatedOnUtc, DateTimeKind.Utc),
                            DiscountAmountApplied = await _priceFormatter.FormatPriceAsync(historyEntry.DiscountAmountApplied, true, false),
                            HistorySource = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Source.Plugin")
                        };

                        var customer = await _customerService.GetCustomerByIdAsync(historyEntry.CustomerId);
                        usageModel.CustomerEmail = customer?.Email ?? await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");

                        var order = await _orderService.GetOrderByIdAsync(historyEntry.OrderId);
                        if (order != null && !order.Deleted)
                        {
                            usageModel.OrderId = order.Id;
                            usageModel.CustomOrderNumber = order.CustomOrderNumber;
                            usageModel.OrderTotal = await _priceFormatter.FormatPriceAsync(order.OrderTotal, true, false);
                        }
                        else
                        {
                            usageModel.CustomOrderNumber = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Order.Deleted");
                            usageModel.CustomerEmail = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");
                        }

                        return usageModel;
                    }));
            }

            searchModel.UseParentDiscountHistory = true;

            var usageHistory = await _discountService.GetAllDiscountUsageHistoryAsync(
                discountId: parentDiscountId,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize);

            return await new PromotionRuleUsageHistoryListModel().PrepareToGridAsync(searchModel, usageHistory, () =>
                usageHistory.SelectAwait(async historyEntry =>
                {
                    var usageModel = new PromotionRuleUsageHistoryModel
                    {
                        Id = historyEntry.Id,
                        CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(historyEntry.CreatedOnUtc, DateTimeKind.Utc),
                        HistorySource = searchModel.ParentDiscountName
                    };

                    var order = await _orderService.GetOrderByIdAsync(historyEntry.OrderId);
                    if (order != null && !order.Deleted)
                    {
                        usageModel.OrderId = order.Id;
                        usageModel.CustomOrderNumber = order.CustomOrderNumber;
                        usageModel.OrderTotal = await _priceFormatter.FormatPriceAsync(order.OrderTotal, true, false);

                        var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
                        usageModel.CustomerEmail = customer?.Email ?? await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");
                    }
                    else
                    {
                        usageModel.CustomOrderNumber = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Order.Deleted");
                        usageModel.CustomerEmail = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");
                    }

                    return usageModel;
                }));
        }

        searchModel.UseParentDiscountHistory = false;
        var standaloneUsageHistory = await _promotionRuleService.GetRuleUsagesAsync(
            promotionRuleId: searchModel.PromotionRuleId,
            pageIndex: searchModel.Page - 1,
            pageSize: searchModel.PageSize);

        var model = await new PromotionRuleUsageHistoryListModel().PrepareToGridAsync(searchModel, standaloneUsageHistory, () =>
            standaloneUsageHistory.SelectAwait(async historyEntry =>
            {
                var usageModel = new PromotionRuleUsageHistoryModel
                {
                    Id = historyEntry.Id,
                    CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(historyEntry.CreatedOnUtc, DateTimeKind.Utc),
                    DiscountAmountApplied = await _priceFormatter.FormatPriceAsync(historyEntry.DiscountAmountApplied, true, false),
                    HistorySource = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Source.Plugin")
                };

                var customer = await _customerService.GetCustomerByIdAsync(historyEntry.CustomerId);
                usageModel.CustomerEmail = customer?.Email ?? await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted");

                var order = await _orderService.GetOrderByIdAsync(historyEntry.OrderId);
                if (order != null && !order.Deleted)
                {
                    usageModel.OrderId = order.Id;
                    usageModel.CustomOrderNumber = order.CustomOrderNumber;
                    usageModel.OrderTotal = await _priceFormatter.FormatPriceAsync(order.OrderTotal, true, false);
                }
                else
                {
                    usageModel.CustomOrderNumber = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Order.Deleted");
                }

                return usageModel;
            }));

        return model;
    }

    public async Task<PromotionRuleProductListModel> PrepareRuleProductListModelAsync(PromotionRuleProductSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var ruleProducts = await _promotionRuleService.GetRuleProductsByRuleIdAsync(searchModel.PromotionRuleId);
        var pagedList = ruleProducts.ToPagedList(searchModel);

        var model = await new PromotionRuleProductListModel().PrepareToGridAsync(searchModel, pagedList, () =>
            pagedList.SelectAwait(async x =>
            {
                var product = await _productService.GetProductByIdAsync(x.ProductId);
                var category = x.CategoryId.HasValue ? await _categoryService.GetCategoryByIdAsync(x.CategoryId.Value) : null;
                var manufacturer = x.ManufacturerId.HasValue ? await _manufacturerService.GetManufacturerByIdAsync(x.ManufacturerId.Value) : null;
                var vendor = x.VendorId.HasValue ? await _vendorService.GetVendorByIdAsync(x.VendorId.Value) : null;

                var sourceType = RuleProductSourceType.Product;
                var sourceName = product?.Name ?? string.Empty;

                if (x.CategoryId.HasValue && x.CategoryId.Value > 0)
                {
                    sourceType = RuleProductSourceType.Category;
                    sourceName = category?.Name ?? string.Empty;
                }
                else if (x.ManufacturerId.HasValue && x.ManufacturerId.Value > 0)
                {
                    sourceType = RuleProductSourceType.Manufacturer;
                    sourceName = manufacturer?.Name ?? string.Empty;
                }
                else if (x.VendorId.HasValue && x.VendorId.Value > 0)
                {
                    sourceType = RuleProductSourceType.Vendor;
                    sourceName = vendor?.Name ?? string.Empty;
                }

                return new PromotionRuleProductModel
                {
                    Id = x.Id,
                    PromotionRuleId = x.PromotionRuleId,
                    ProductId = x.ProductId,
                    ProductName = x.IsAllProducts ? "All Products" : (product?.Name ?? string.Empty),
                    SourceTypeName = x.IsAllProducts ? "All Products" : await _localizationService.GetLocalizedEnumAsync(sourceType),
                    SourceName = x.IsAllProducts ? "All Products" : sourceName,
                    MinQuantity = x.MinQuantity,
                    MaxQuantity = x.MaxQuantity,
                    IsAllProducts = x.IsAllProducts,
                    IsRewardProduct = x.IsRewardProduct,
                    RewardAttributeSelectionTypeId = x.RewardAttributeSelectionTypeId,
                    RewardAttributeSelectionTypeName = await _localizationService.GetLocalizedEnumAsync(x.RewardAttributeSelectionType),
                    RewardAttributeValueIds = x.RewardAttributeValueIds,
                    AttributeFilterSummary = await BuildRuleProductAttributeFilterSummaryAsync(x),
                    CategoryId = x.CategoryId,
                    ManufacturerId = x.ManufacturerId,
                    VendorId = x.VendorId
                };
            }));

        return model;
    }

    public async Task<PromotionRuleExcludedProductListModel> PrepareExcludedProductListModelAsync(PromotionRuleExcludedProductSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var excludedProducts = await _excludedProductService.GetExcludedProductsPagedAsync(
            searchModel.PromotionRuleId,
            searchModel.Page - 1,
            searchModel.PageSize);

        var model = await new PromotionRuleExcludedProductListModel().PrepareToGridAsync(searchModel, excludedProducts, () =>
            excludedProducts.SelectAwait(async x =>
            {
                var product = await _productService.GetProductByIdAsync(x.ProductId);
                var productPicture = (await _productService.GetProductPicturesByProductIdAsync(x.ProductId)).FirstOrDefault();
                var pictureUrl = string.Empty;

                if (productPicture != null && _pictureService != null)
                {
                    pictureUrl = await _pictureService.GetPictureUrlAsync(productPicture.PictureId, 75);
                }

                return new PromotionRuleExcludedProductModel
                {
                    Id = x.Id,
                    PromotionRuleId = x.PromotionRuleId,
                    ProductId = x.ProductId,
                    ProductName = product?.Name ?? string.Empty,
                    Sku = product?.Sku ?? string.Empty,
                    Price = product?.Price ?? 0,
                    ProductPictureUrl = pictureUrl,
                    CreatedOnUtc = x.CreatedOnUtc
                };
            }));

        return model;
    }

    public async Task<PromotionRuleTierListModel> PrepareRuleTierListModelAsync(PromotionRuleTierSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var tiers = await _promotionRuleService.GetRuleTiersByRuleIdAsync(searchModel.PromotionRuleId);
        var pagedList = tiers.ToPagedList(searchModel);

        var model = await new PromotionRuleTierListModel().PrepareToGridAsync(searchModel, pagedList, () =>
            pagedList.SelectAwait(async x => await PrepareRuleTierModelAsync(null, x, true)));

        return model;
    }

    public async Task<PromotionRuleTierModel> PrepareRuleTierModelAsync(
        PromotionRuleTierModel model,
        PromotionRuleTier tier,
        bool excludeProperties = false)
    {
        if (tier != null)
        {
            if (model == null)
                model = tier.ToModel<PromotionRuleTierModel>();

            model.DiscountTypeName = await _localizationService.GetLocalizedEnumAsync(tier.DiscountType);

            if (tier.RewardProductId.HasValue)
            {
                var rewardProduct = await _productService.GetProductByIdAsync(tier.RewardProductId.Value);
                model.RewardProductName = rewardProduct?.Name ?? string.Empty;
            }

            var mappedRuleProductIds = await _promotionRuleService.GetMappedRuleProductIdsByTierIdAsync(tier.Id);
            model.SelectedRuleProductIds = mappedRuleProductIds.ToList();
            model.AppliesToRuleProductsSummary = await BuildTierAppliesToRuleProductsSummaryAsync(
                tier.PromotionRuleId,
                mappedRuleProductIds);
        }

        var promotionRuleId = tier?.PromotionRuleId ?? model?.PromotionRuleId ?? 0;
        if (promotionRuleId > 0)
        {
            var promotionRule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
            if (promotionRule != null)
            {
                model ??= new PromotionRuleTierModel();
                model.PromotionRuleId = promotionRuleId;
                model.RuleTypeId = (int)promotionRule.RuleType;
            }
        }

        if (!excludeProperties)
        {
            await PrepareDiscountTypeSelectListAsync(model.AvailableDiscountTypes);
            await PrepareProductSelectListAsync(model.AvailableProducts);
            await PrepareTierRuleProductSelectListAsync(model.AvailableRuleProducts, model.PromotionRuleId, model.SelectedRuleProductIds);
        }

        if (model != null && model.PromotionRuleId > 0 && string.IsNullOrWhiteSpace(model.AppliesToRuleProductsSummary))
        {
            model.AppliesToRuleProductsSummary = await BuildTierAppliesToRuleProductsSummaryAsync(
                model.PromotionRuleId,
                model.SelectedRuleProductIds);
        }

        return model ?? new PromotionRuleTierModel();
    }

    public async Task<PromotionRuleConditionListModel> PrepareRuleConditionListModelAsync(PromotionRuleConditionSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var conditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(searchModel.PromotionRuleId);
        var orderedConditions = conditions
            .OrderBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
            .ThenBy(x => x.Id)
            .ToList();

        var firstConditionIdsByGroup = orderedConditions
            .GroupBy(x => x.ConditionGroup <= 0 ? 1 : x.ConditionGroup)
            .Select(x => x.First().Id)
            .ToHashSet();

        var pagedList = orderedConditions.ToPagedList(searchModel);
        var conditionModels = new List<PromotionRuleConditionModel>();
        foreach (var condition in pagedList)
        {
            var conditionModel = await PrepareRuleConditionModelAsync(null, condition, true);
            if (firstConditionIdsByGroup.Contains(condition.Id))
                conditionModel.LogicalOperatorName = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator.FirstInGroup");

            conditionModels.Add(conditionModel);
        }

        return new PromotionRuleConditionListModel
        {
            Data = conditionModels,
            Draw = searchModel.Draw,
            RecordsFiltered = orderedConditions.Count,
            RecordsTotal = orderedConditions.Count
        };
    }

    public async Task<PromotionRuleConditionRequirementListModel> PrepareRuleConditionRequirementListModelAsync(int promotionRuleId)
    {
        var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(promotionRuleId);
        var ruleTypeId = rule?.RuleTypeId ?? 0;
        var conditions = await _promotionRuleService.GetRuleConditionsByRuleIdAsync(promotionRuleId);
        return await PrepareConditionRequirementListModelAsync(conditions, ruleTypeId);
    }

    public async Task<PromotionRuleConditionRequirementListModel> PrepareDiscountRequirementConditionListModelAsync(int discountRequirementId)
    {
        var conditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
        return await PrepareConditionRequirementListModelAsync(conditions, 0);
    }

    protected virtual async Task<PromotionRuleConditionRequirementListModel> PrepareConditionRequirementListModelAsync(
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
                    Text = $"G{x}"
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

            var rootConditions = conditionsByParent.TryGetValue(0, out var rootNodes)
                ? rootNodes
                : new List<PromotionRuleCondition>();

            var interactionId = rootConditions.Skip(1).Select(x => x.LogicalOperatorId).FirstOrDefault();
            if (!Enum.IsDefined(typeof(ConditionLogicalOperator), interactionId))
                interactionId = (int)ConditionLogicalOperator.And;

            var groupModel = new PromotionRuleConditionRequirementModel
            {
                IsGroup = true,
                GroupId = groupId,
                RuleName = $"G{groupId}",
                AvailableInteractionTypes = await BuildLogicalOperatorSelectListAsync(interactionId)
            };

            groupModel.ChildRequirements = (await BuildConditionRequirementTreeAsync(
                    rootConditions,
                    groupId,
                    ruleTypeId,
                    conditionsByParent,
                    new HashSet<int>()))
                .ToList();
            model.Conditions.Add(groupModel);
        }

        return model;
    }

    protected virtual async Task<IList<PromotionRuleConditionRequirementModel>> BuildConditionRequirementTreeAsync(
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

            var conditionModel = await PrepareRuleConditionModelAsync(null, condition, true);
            if (ruleTypeId > 0)
            {
                conditionModel.RuleTypeId = ruleTypeId;
                conditionModel.Summary = await BuildConditionSummaryAsync(conditionModel);
            }

            var isLast = i == siblings.Count - 1;
            var item = new PromotionRuleConditionRequirementModel
            {
                ConditionId = condition.Id,
                GroupId = groupId,
                IsGroup = false,
                RuleName = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Condition.Title"),
                Summary = conditionModel.Summary,
                IsLastInGroup = isLast
            };

            if (!isLast)
            {
                var nextLogicalId = siblings[i + 1].LogicalOperatorId;
                var nextLogicalOperator = Enum.IsDefined(typeof(ConditionLogicalOperator), nextLogicalId)
                    ? (ConditionLogicalOperator)nextLogicalId
                    : ConditionLogicalOperator.And;
                item.InteractionType = await _localizationService.GetLocalizedEnumAsync(nextLogicalOperator);
            }

            if (conditionsByParent.TryGetValue(condition.Id, out var children) && children.Any())
            {
                item.ChildRequirements = (await BuildConditionRequirementTreeAsync(
                        children,
                        groupId,
                        ruleTypeId,
                        conditionsByParent,
                        visited))
                    .ToList();
            }

            visited.Remove(condition.Id);
            items.Add(item);
        }

        return items;
    }

    public async Task<PromotionRuleConditionModel> PrepareRuleConditionModelAsync(
        PromotionRuleConditionModel model,
        PromotionRuleCondition condition,
        bool excludeProperties = false)
    {
        var currentConditionId = condition?.Id;
        if (condition != null)
        {
            if (model == null)
            {
                model = condition.ToModel<PromotionRuleConditionModel>();
                model.SelectedCountryCodes = ParseCsvList(condition.RequiredCountryCodesCsv)
                    .Select(x => x.ToUpperInvariant())
                    .ToList();
                model.SelectedPaymentMethodSystemNames = ParseCsvList(condition.RequiredPaymentMethodsCsv).ToList();
            }

            if (model.RuleTypeId <= 0)
            {
                var rule = await _promotionRuleService.GetPromotionRuleByIdAsync(condition.PromotionRuleId);
                if (rule != null)
                    model.RuleTypeId = rule.RuleTypeId;
            }

            model.LogicalOperatorName = await _localizationService.GetLocalizedEnumAsync(condition.LogicalOperator);
            model.ConditionOperatorName = await _localizationService.GetLocalizedEnumAsync(condition.ConditionOperator);
            model.ConditionRestrictionTypeName = await _localizationService.GetLocalizedEnumAsync(condition.ConditionRestrictionType);
            model.ConditionSourceTypeName = condition.ConditionSourceTypeId > 0
                ? await _localizationService.GetLocalizedEnumAsync(condition.ConditionSourceType)
                : string.Empty;
            model.AttributeMatchModeName = await _localizationService.GetLocalizedEnumAsync(condition.AttributeMatchMode);

            if (condition.ParentConditionId.HasValue && condition.ParentConditionId.Value > 0)
            {
                var parentCondition = await _promotionRuleService.GetRuleConditionByIdAsync(condition.ParentConditionId.Value);
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

            if (condition.RequiredProductId.HasValue)
            {
                var product = await _productService.GetProductByIdAsync(condition.RequiredProductId.Value);
                model.RequiredProductName = product?.Name ?? string.Empty;
            }

            if (condition.ExcludedProductId.HasValue)
            {
                var product = await _productService.GetProductByIdAsync(condition.ExcludedProductId.Value);
                model.ExcludedProductName = product?.Name ?? string.Empty;
            }

            if (condition.RequiredCategoryId.HasValue)
            {
                var category = await _categoryService.GetCategoryByIdAsync(condition.RequiredCategoryId.Value);
                model.RequiredCategoryName = category?.Name ?? string.Empty;
            }

            if (condition.RequiredVendorId.HasValue)
            {
                var vendor = await _vendorService.GetVendorByIdAsync(condition.RequiredVendorId.Value);
                model.RequiredVendorName = vendor?.Name ?? string.Empty;
            }

            if (condition.RequiredCustomerRoleId.HasValue)
            {
                var role = (await _customerService.GetAllCustomerRolesAsync(true))
                    .FirstOrDefault(x => x.Id == condition.RequiredCustomerRoleId.Value);
                model.RequiredCustomerRoleName = role?.Name ?? string.Empty;
            }

            await HydrateConditionSourceBuilderAsync(model);
            model.Summary = await BuildConditionSummaryAsync(model);
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

        if (!excludeProperties)
        {
            await PrepareLogicalOperatorSelectListAsync(model.AvailableLogicalOperators);
            await PrepareConditionOperatorSelectListAsync(model.AvailableConditionOperators);
            await PrepareCategorySelectListAsync(model.AvailableCategories);
            await PrepareVendorSelectListAsync(model.AvailableVendors);
            await PrepareCustomerRoleSelectListAsync(model.AvailableCustomerRoles);
            await PrepareCountrySelectListAsync(model.AvailableCountries, model.SelectedCountryCodes);
            await PreparePaymentMethodSelectListAsync(model.AvailablePaymentMethods, model.SelectedPaymentMethodSystemNames);
            await PrepareSpecificationAttributeSelectListAsync(model.AvailableSpecificationAttributes);
            await PrepareProductAttributeSelectListAsync(model.AvailableProductAttributes);
            await PrepareSourceTokenSelectListsAsync(model.AvailableSourceDeviceTypes, model.AvailableSourceSalesChannels);
            await PrepareSpecificationAttributeOptionsLookupAsync(model.AvailableSpecificationAttributeOptionsLookup);
            await PrepareProductAttributeValuesLookupAsync(model.AvailableProductAttributeValuesLookup);
            await PrepareConditionRestrictionTypeSelectListAsync(model.AvailableConditionRestrictionTypes);
            await PrepareConditionSourceTypeSelectListAsync(model.AvailableConditionSourceTypes);
            await PrepareAttributeMatchModeSelectListAsync(model.AvailableAttributeMatchModes);
            await PrepareParentConditionSelectListAsync(model, currentConditionId ?? (model.Id > 0 ? model.Id : null));
        }

        return model ?? new PromotionRuleConditionModel();
    }

    protected virtual async Task<string> BuildConditionSummaryAsync(PromotionRuleConditionModel model)
    {
        var parts = new List<string>();

        var hasSubtotalCriteria = model.ConditionOperatorId > 0 &&
                                 (model.RuleTypeId == (int)PromotionRuleType.SubtotalBased ||
                                  model.MinValue != 0 ||
                                  model.MaxValue != 0);
        if (hasSubtotalCriteria)
        {
            var conditionLabel = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator");
            var operatorText = model.ConditionOperatorName ?? string.Empty;
            var valueText = model.ConditionOperatorId == (int)ConditionOperator.Between
                ? $"{model.MinValue:0.####} - {model.MaxValue:0.####}"
                : $"{model.MinValue:0.####}";
            parts.Add($"{conditionLabel}: {operatorText} {valueText}");
        }

        if (!string.IsNullOrWhiteSpace(model.RequiredProductName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct")}: {model.RequiredProductName}");

        if (!string.IsNullOrWhiteSpace(model.ExcludedProductName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct")}: {model.ExcludedProductName}");

        if (!string.IsNullOrWhiteSpace(model.RequiredCategoryName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory")}: {model.RequiredCategoryName}");

        if (!string.IsNullOrWhiteSpace(model.RequiredVendorName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor")}: {model.RequiredVendorName}");

        if (!string.IsNullOrWhiteSpace(model.RequiredCustomerRoleName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole")}: {model.RequiredCustomerRoleName}");

        if (model.IsFirstOrderOnly)
            parts.Add(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsFirstOrderOnly"));

        if (model.IsNewCustomerOnly)
            parts.Add(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsNewCustomerOnly"));

        var sourceSummary = await BuildConditionSourceSummaryAsync(model);
        if (!string.IsNullOrWhiteSpace(sourceSummary) && model.ConditionSourceTypeId > 0)
        {
            var restrictionText = model.ConditionRestrictionTypeName ?? string.Empty;
            var sourceText = model.ConditionSourceTypeName ?? string.Empty;
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData")}: {restrictionText} {sourceText} = {sourceSummary}");
        }

        if (!string.IsNullOrWhiteSpace(model.ParentConditionName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition")}: {model.ParentConditionName}");

        if (model.QuantityMin.HasValue || model.QuantityMax.HasValue)
        {
            var qtyText = model.QuantityMin.HasValue && model.QuantityMax.HasValue
                ? $"{model.QuantityMin.Value} - {model.QuantityMax.Value}"
                : model.QuantityMin.HasValue
                    ? $"{model.QuantityMin.Value}+"
                    : $"<= {model.QuantityMax.Value}";
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityRange")}: {qtyText}");
        }

        if (!string.IsNullOrWhiteSpace(model.RequiredCountryCodesCsv))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCountryCodesCsv")}: {await BuildCountrySummaryAsync(model.RequiredCountryCodesCsv)}");

        if (!string.IsNullOrWhiteSpace(model.RequiredPaymentMethodsCsv))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredPaymentMethodsCsv")}: {await BuildPaymentMethodSummaryAsync(model.RequiredPaymentMethodsCsv)}");

        if (model.RequiredOrderCountMin.HasValue || model.RequiredOrderCountMax.HasValue)
        {
            var orderText = model.RequiredOrderCountMin.HasValue && model.RequiredOrderCountMax.HasValue
                ? $"{model.RequiredOrderCountMin.Value} - {model.RequiredOrderCountMax.Value}"
                : model.RequiredOrderCountMin.HasValue
                    ? $"{model.RequiredOrderCountMin.Value}+"
                    : $"<= {model.RequiredOrderCountMax.Value}";
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountRange")}: {orderText}");
        }

        if (model.RequireSameLineMatch)
            parts.Add(await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequireSameLineMatch"));

        if (!string.IsNullOrWhiteSpace(model.AttributeMatchModeName))
            parts.Add($"{await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode")}: {model.AttributeMatchModeName}");

        if (!parts.Any())
            return await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Summary.Empty");

        return string.Join(" | ", parts);
    }

    public virtual async Task<PromotionRuleRewardAttributePopupModel> PrepareRewardAttributePopupModelAsync(int productId, string selectedValueIds)
    {
        var model = new PromotionRuleRewardAttributePopupModel
        {
            ProductId = productId,
            RewardAttributeValueIds = selectedValueIds ?? string.Empty
        };

        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted)
            return model;

        var selectedIds = ParseIdSet(selectedValueIds ?? string.Empty);
        await PrepareRewardProductAttributeModelsAsync(model.ProductAttributes, product, selectedIds);
        return model;
    }

    #endregion
}
