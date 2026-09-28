using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public partial record PromotionRuleConditionSearchModel : BaseSearchModel
{
    public int PromotionRuleId { get; set; }
    public int DiscountRequirementId { get; set; }
}

public partial record PromotionRuleConditionListModel : BasePagedListModel<PromotionRuleConditionModel>
{
}

public partial record PromotionRuleConditionModel : BaseNopEntityModel
{
    public int PromotionRuleId { get; set; }
    public int DiscountRequirementId { get; set; }
    public int RuleTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup")]
    public int ConditionGroup { get; set; } = 1;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition")]
    public int? ParentConditionId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition")]
    public string ParentConditionName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator")]
    public int LogicalOperatorId { get; set; } = (int)ConditionLogicalOperator.And;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator")]
    public string LogicalOperatorName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Summary")]
    public string Summary { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator")]
    public int ConditionOperatorId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator")]
    public string ConditionOperatorName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MinValue")]
    public decimal MinValue { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MaxValue")]
    public decimal MaxValue { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct")]
    public int? RequiredProductId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct")]
    public string RequiredProductName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct")]
    public int? ExcludedProductId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct")]
    public string ExcludedProductName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory")]
    public int? RequiredCategoryId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory")]
    public string RequiredCategoryName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor")]
    public int? RequiredVendorId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor")]
    public string RequiredVendorName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole")]
    public int? RequiredCustomerRoleId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole")]
    public string RequiredCustomerRoleName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsFirstOrderOnly")]
    public bool IsFirstOrderOnly { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsNewCustomerOnly")]
    public bool IsNewCustomerOnly { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionRestrictionType")]
    public int ConditionRestrictionTypeId { get; set; } = (int)ConditionRestrictionType.Include;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionRestrictionType")]
    public string ConditionRestrictionTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType")]
    public int ConditionSourceTypeId { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType")]
    public string ConditionSourceTypeName { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData")]
    public string ConditionSourceData { get; set; } = string.Empty;

    public string ConditionSourceBuilderJson { get; set; } = string.Empty;
    public bool ConditionSourceBuilderTouched { get; set; }
    public string ConditionSourceBuilderWarning { get; set; } = string.Empty;
    public IList<ConditionSourceBuilderRowModel> SourceBuilderRows { get; set; } = new List<ConditionSourceBuilderRowModel>();
    public IList<string> SelectedSourceTokens { get; set; } = new List<string>();

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin")]
    [UIHint("Int32Nullable")]
    public int? QuantityMin { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax")]
    [UIHint("Int32Nullable")]
    public int? QuantityMax { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCountryCodesCsv")]
    public string RequiredCountryCodesCsv { get; set; } = string.Empty;

    public List<string> SelectedCountryCodes { get; set; } = new();

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredPaymentMethodsCsv")]
    public string RequiredPaymentMethodsCsv { get; set; } = string.Empty;

    public List<string> SelectedPaymentMethodSystemNames { get; set; } = new();

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCouponCodesCsv")]
    public string RequiredCouponCodesCsv { get; set; } = string.Empty;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin")]
    [UIHint("Int32Nullable")]
    public int? RequiredOrderCountMin { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax")]
    [UIHint("Int32Nullable")]
    public int? RequiredOrderCountMax { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequireSameLineMatch")]
    public bool RequireSameLineMatch { get; set; }

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode")]
    public int AttributeMatchModeId { get; set; } = (int)AttributeMatchMode.Any;

    [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode")]
    public string AttributeMatchModeName { get; set; } = string.Empty;

    public IList<SelectListItem> AvailableConditionOperators { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableLogicalOperators { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableProducts { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCustomerRoles { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableCountries { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailablePaymentMethods { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableManufacturers { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableSpecificationAttributes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableProductAttributes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableSourceDeviceTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableSourceSalesChannels { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableConditionSourceTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableConditionRestrictionTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableAttributeMatchModes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableParentConditions { get; set; } = new List<SelectListItem>();
    public IDictionary<int, IList<SelectListItem>> AvailableSpecificationAttributeOptionsLookup { get; set; } = new Dictionary<int, IList<SelectListItem>>();
    public IDictionary<int, IList<SelectListItem>> AvailableProductAttributeValuesLookup { get; set; } = new Dictionary<int, IList<SelectListItem>>();
}

public partial record ConditionSourceBuilderRowModel : BaseNopModel
{
    public string Key { get; set; } = string.Empty;
    public int? EntryId { get; set; }
    public string EntryName { get; set; } = string.Empty;
    public IList<string> SelectedOptionValues { get; set; } = new List<string>();
    public IList<string> SelectedOptionTexts { get; set; } = new List<string>();
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
}

public partial record ConditionSourceBuilderStateModel : BaseNopModel
{
    public IList<ConditionSourceBuilderRowModel> Rows { get; set; } = new List<ConditionSourceBuilderRowModel>();
    public IList<string> Tokens { get; set; } = new List<string>();
}

public partial record PromotionRuleConditionRequirementListModel : BaseNopModel
{
    public IList<PromotionRuleConditionRequirementModel> Conditions { get; set; } = new List<PromotionRuleConditionRequirementModel>();
    public IList<SelectListItem> AvailableGroups { get; set; } = new List<SelectListItem>();
    public int NextGroupId { get; set; }
}

public partial record PromotionRuleConditionRequirementModel : BaseNopModel
{
    public int ConditionId { get; set; }
    public int GroupId { get; set; }
    public bool IsGroup { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string InteractionType { get; set; } = string.Empty;
    public bool IsLastInGroup { get; set; }
    public IList<SelectListItem> AvailableInteractionTypes { get; set; } = new List<SelectListItem>();
    public IList<PromotionRuleConditionRequirementModel> ChildRequirements { get; set; } = new List<PromotionRuleConditionRequirementModel>();
}
