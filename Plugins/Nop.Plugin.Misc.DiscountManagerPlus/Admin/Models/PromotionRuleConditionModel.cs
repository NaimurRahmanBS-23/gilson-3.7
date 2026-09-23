using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class PromotionRuleConditionSearchModel : BaseNopModel
    {
        public PromotionRuleConditionSearchModel()
        {
            PageIndex = 0;
            PageSize = 15;
        }

        public int PromotionRuleId { get; set; }
        public int DiscountRequirementId { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public partial class PromotionRuleConditionModel : BaseNopEntityModel
    {
        public PromotionRuleConditionModel()
        {
            ConditionGroup = 1;
            ParentConditionName = string.Empty;
            LogicalOperatorId = (int)ConditionLogicalOperator.And;
            LogicalOperatorName = string.Empty;
            Summary = string.Empty;
            ConditionOperatorName = string.Empty;
            RequiredProductName = string.Empty;
            ExcludedProductName = string.Empty;
            RequiredCategoryName = string.Empty;
            RequiredVendorName = string.Empty;
            RequiredCustomerRoleName = string.Empty;
            ConditionRestrictionTypeId = (int)ConditionRestrictionType.Include;
            ConditionRestrictionTypeName = string.Empty;
            ConditionSourceTypeName = string.Empty;
            ConditionSourceData = string.Empty;
            ConditionSourceBuilderJson = string.Empty;
            ConditionSourceBuilderWarning = string.Empty;
            SourceBuilderRows = new List<ConditionSourceBuilderRowModel>();
            SelectedSourceTokens = new List<string>();
            RequiredCountryCodesCsv = string.Empty;
            SelectedCountryCodes = new List<string>();
            RequiredPaymentMethodsCsv = string.Empty;
            SelectedPaymentMethodSystemNames = new List<string>();
            RequiredCouponCodesCsv = string.Empty;
            AttributeMatchModeId = (int)AttributeMatchMode.Any;
            AttributeMatchModeName = string.Empty;
            AvailableConditionOperators = new List<SelectListItem>();
            AvailableLogicalOperators = new List<SelectListItem>();
            AvailableProducts = new List<SelectListItem>();
            AvailableCategories = new List<SelectListItem>();
            AvailableVendors = new List<SelectListItem>();
            AvailableCustomerRoles = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailablePaymentMethods = new List<SelectListItem>();
            AvailableManufacturers = new List<SelectListItem>();
            AvailableSpecificationAttributes = new List<SelectListItem>();
            AvailableProductAttributes = new List<SelectListItem>();
            AvailableSourceDeviceTypes = new List<SelectListItem>();
            AvailableSourceSalesChannels = new List<SelectListItem>();
            AvailableConditionSourceTypes = new List<SelectListItem>();
            AvailableConditionRestrictionTypes = new List<SelectListItem>();
            AvailableAttributeMatchModes = new List<SelectListItem>();
            AvailableParentConditions = new List<SelectListItem>();
            AvailableSpecificationAttributeOptionsLookup = new Dictionary<int, IList<SelectListItem>>();
            AvailableProductAttributeValuesLookup = new Dictionary<int, IList<SelectListItem>>();
        }

        public int PromotionRuleId { get; set; }
        public int DiscountRequirementId { get; set; }
        public int RuleTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup")]
        public int ConditionGroup { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition")]
        public int? ParentConditionId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition")]
        public string ParentConditionName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator")]
        public int LogicalOperatorId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator")]
        public string LogicalOperatorName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Summary")]
        public string Summary { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator")]
        public int ConditionOperatorId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator")]
        public string ConditionOperatorName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MinValue")]
        public decimal MinValue { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MaxValue")]
        public decimal MaxValue { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct")]
        public int? RequiredProductId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct")]
        public string RequiredProductName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct")]
        public int? ExcludedProductId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct")]
        public string ExcludedProductName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory")]
        public int? RequiredCategoryId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory")]
        public string RequiredCategoryName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor")]
        public int? RequiredVendorId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor")]
        public string RequiredVendorName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole")]
        public int? RequiredCustomerRoleId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole")]
        public string RequiredCustomerRoleName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsFirstOrderOnly")]
        public bool IsFirstOrderOnly { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsNewCustomerOnly")]
        public bool IsNewCustomerOnly { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionRestrictionType")]
        public int ConditionRestrictionTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionRestrictionType")]
        public string ConditionRestrictionTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType")]
        public int ConditionSourceTypeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType")]
        public string ConditionSourceTypeName { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData")]
        [AllowHtml]
        public string ConditionSourceData { get; set; }

        public string ConditionSourceBuilderJson { get; set; }
        public bool ConditionSourceBuilderTouched { get; set; }
        public string ConditionSourceBuilderWarning { get; set; }
        public IList<ConditionSourceBuilderRowModel> SourceBuilderRows { get; set; }
        public IList<string> SelectedSourceTokens { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin")]
        [UIHint("Int32Nullable")]
        public int? QuantityMin { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax")]
        [UIHint("Int32Nullable")]
        public int? QuantityMax { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCountryCodesCsv")]
        public string RequiredCountryCodesCsv { get; set; }

        public List<string> SelectedCountryCodes { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredPaymentMethodsCsv")]
        public string RequiredPaymentMethodsCsv { get; set; }

        public List<string> SelectedPaymentMethodSystemNames { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCouponCodesCsv")]
        public string RequiredCouponCodesCsv { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin")]
        [UIHint("Int32Nullable")]
        public int? RequiredOrderCountMin { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax")]
        [UIHint("Int32Nullable")]
        public int? RequiredOrderCountMax { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequireSameLineMatch")]
        public bool RequireSameLineMatch { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode")]
        public int AttributeMatchModeId { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode")]
        public string AttributeMatchModeName { get; set; }

        public IList<SelectListItem> AvailableConditionOperators { get; set; }
        public IList<SelectListItem> AvailableLogicalOperators { get; set; }
        public IList<SelectListItem> AvailableProducts { get; set; }
        public IList<SelectListItem> AvailableCategories { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailablePaymentMethods { get; set; }
        public IList<SelectListItem> AvailableManufacturers { get; set; }
        public IList<SelectListItem> AvailableSpecificationAttributes { get; set; }
        public IList<SelectListItem> AvailableProductAttributes { get; set; }
        public IList<SelectListItem> AvailableSourceDeviceTypes { get; set; }
        public IList<SelectListItem> AvailableSourceSalesChannels { get; set; }
        public IList<SelectListItem> AvailableConditionSourceTypes { get; set; }
        public IList<SelectListItem> AvailableConditionRestrictionTypes { get; set; }
        public IList<SelectListItem> AvailableAttributeMatchModes { get; set; }
        public IList<SelectListItem> AvailableParentConditions { get; set; }
        public IDictionary<int, IList<SelectListItem>> AvailableSpecificationAttributeOptionsLookup { get; set; }
        public IDictionary<int, IList<SelectListItem>> AvailableProductAttributeValuesLookup { get; set; }
    }

    public partial class ConditionSourceBuilderRowModel : BaseNopModel
    {
        public ConditionSourceBuilderRowModel()
        {
            Key = string.Empty;
            EntryName = string.Empty;
            SelectedOptionValues = new List<string>();
            SelectedOptionTexts = new List<string>();
        }

        public string Key { get; set; }
        public int? EntryId { get; set; }
        public string EntryName { get; set; }
        public IList<string> SelectedOptionValues { get; set; }
        public IList<string> SelectedOptionTexts { get; set; }
        public int? RangeMin { get; set; }
        public int? RangeMax { get; set; }
    }

    public partial class ConditionSourceBuilderStateModel : BaseNopModel
    {
        public ConditionSourceBuilderStateModel()
        {
            Rows = new List<ConditionSourceBuilderRowModel>();
            Tokens = new List<string>();
        }

        public IList<ConditionSourceBuilderRowModel> Rows { get; set; }
        public IList<string> Tokens { get; set; }
    }

    public partial class PromotionRuleConditionRequirementListModel : BaseNopModel
    {
        public PromotionRuleConditionRequirementListModel()
        {
            Conditions = new List<PromotionRuleConditionRequirementModel>();
            AvailableGroups = new List<SelectListItem>();
        }

        public IList<PromotionRuleConditionRequirementModel> Conditions { get; set; }
        public IList<SelectListItem> AvailableGroups { get; set; }
        public int NextGroupId { get; set; }
    }

    public partial class PromotionRuleConditionRequirementModel : BaseNopModel
    {
        public PromotionRuleConditionRequirementModel()
        {
            RuleName = string.Empty;
            Summary = string.Empty;
            InteractionType = string.Empty;
            AvailableInteractionTypes = new List<SelectListItem>();
            ChildRequirements = new List<PromotionRuleConditionRequirementModel>();
        }

        public int ConditionId { get; set; }
        public int GroupId { get; set; }
        public bool IsGroup { get; set; }
        public string RuleName { get; set; }
        public string Summary { get; set; }
        public string InteractionType { get; set; }
        public bool IsLastInGroup { get; set; }
        public IList<SelectListItem> AvailableInteractionTypes { get; set; }
        public IList<PromotionRuleConditionRequirementModel> ChildRequirements { get; set; }
    }
}
