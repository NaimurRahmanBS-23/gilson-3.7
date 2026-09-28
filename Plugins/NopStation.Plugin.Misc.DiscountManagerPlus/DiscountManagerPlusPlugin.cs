using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Components;
using NopStation.Plugin.Misc.Core.Services;
using NopStation.Plugin.Misc.DiscountManagerPlus.Components;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;
using Microsoft.AspNetCore.Mvc;

namespace NopStation.Plugin.Misc.DiscountManagerPlus;

public class DiscountManagerPlusPlugin : BasePlugin, IMiscPlugin, INopStationPlugin, IWidgetPlugin, IDiscountRequirementRule
{
    #region Fields

    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;
    private readonly IDiscountService _discountService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public DiscountManagerPlusPlugin(
        IActionContextAccessor actionContextAccessor,
        IDiscountManagerPlusService discountManagerPlusService,
        IDiscountManagerPlusRequirementService discountManagerPlusRequirementService,
        IDiscountService discountService,
        ILocalizationService localizationService,
        ISettingService settingService,
        IUrlHelperFactory urlHelperFactory,
        IWebHelper webHelper)
    {
        _actionContextAccessor = actionContextAccessor;
        _discountManagerPlusService = discountManagerPlusService;
        _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
        _discountService = discountService;
        _localizationService = localizationService;
        _settingService = settingService;
        _urlHelperFactory = urlHelperFactory;
        _webHelper = webHelper;
    }

    #endregion

    #region Methods

    public bool HideInWidgetList => false;

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/DiscountManagerPlus/Configure";
    }

    public override async Task InstallAsync()
    {
        await this.InstallPluginAsync();
        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        var discountRequirements = (await _discountService.GetAllDiscountRequirementsAsync())
            .Where(discountRequirement => discountRequirement.DiscountRequirementRuleSystemName == DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName);

        foreach (var discountRequirement in discountRequirements)
            await _discountService.DeleteDiscountRequirementAsync(discountRequirement, false);

        await _discountManagerPlusRequirementService.CleanupManagedDiscountRequirementWrappersAsync();

        await this.UninstallPluginAsync(new DiscountManagerPlusPermissionConfigManager());
        await base.UninstallAsync();
    }

    public override async Task UpdateAsync(string currentVersion, string targetVersion)
    {
        var keyValuePairs = GetPluginResources();
        foreach (var keyValuePair in keyValuePairs)
        {
            await _localizationService.AddOrUpdateLocaleResourceAsync(keyValuePair.Key, keyValuePair.Value);
        }
    }

    public IDictionary<string, string> GetPluginResources()
    {
        return new Dictionary<string, string>
        {
            ["Admin.NopStation.DiscountManagerPlus.Menu.DiscountManagerPlus"] = "Discount Manager Plus",
            ["Admin.NopStation.DiscountManagerPlus.Menu.PromotionRules"] = "Promotion Rules",
            ["Admin.NopStation.DiscountManagerPlus.Menu.Analytics"] = "Analytics",
            ["Admin.NopStation.DiscountManagerPlus.Menu.Configuration"] = "Configuration",

            ["Admin.NopStation.DiscountManagerPlus.Configuration"] = "DiscountManagerPlus Configuration",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Saved"] = "Configuration saved successfully.",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.IsEnabled"] = "Plugin enabled",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.IsEnabled.Hint"] = "Check to enable the Discount Manager Plus.",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.MaxRuleEvaluationTimeMs"] = "Max rule evaluation time (ms)",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.MaxRuleEvaluationTimeMs.Hint"] = "Maximum allowed rule evaluation time in milliseconds. Default: 100ms.",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnablePromotionBadge"] = "Show promotion badge",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnablePromotionBadge.Hint"] = "Display a promotion badge on product listings when a rule applies.",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnableCartSavingsBreakdown"] = "Show cart savings breakdown",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnableCartSavingsBreakdown.Hint"] = "Show a per-rule savings breakdown on the cart page.",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.UseDefaultDiscountPipeline"] = "Use default nopCommerce discount pipeline",
            ["Admin.NopStation.DiscountManagerPlus.Configuration.Fields.UseDefaultDiscountPipeline.Hint"] = "When enabled, DiscountManagerPlus validates rule eligibility for nopCommerce discounts instead of calculating cart line discounts directly.",

            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Title"] = "DiscountManagerPlus",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.UseDiscountManagerPlus"] = "Enable Discount Manager Plus sync",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.RuleType"] = "Rule type",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.DiscountType"] = "Discount type",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.DiscountScope"] = "Discount scope",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.DiscountValue"] = "Discount value",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.Priority"] = "Priority",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Fields.IsExclusive"] = "Exclusive (stop further rules)",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Actions.SaveRule"] = "Save promotion rule",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Actions.OpenRule"] = "Open full rule editor",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.SaveBeforeManage"] = "Save the promotion rule first to manage products, tiers, and conditions.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.SaveDiscountFirst"] = "Save the native discount first, then add DiscountManagerPlus rules here.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.SaveFailed"] = "Failed to save the promotion rule.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Saved"] = "Promotion rule saved successfully.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Disabled"] = "DiscountManagerPlus sync disabled for this discount.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.Empty"] = "No DiscountManagerPlus rules are attached to this discount yet.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.UnsupportedDiscountType"] = "This discount type is not supported by DiscountManagerPlus.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.SafeSetupHint"] = "Default discounts remain active. Use DiscountManagerPlus only for rules that default discounts cannot handle (BOGO, combo, advanced conditions) to avoid double discounts.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.RuleTypeHelp.ProductBased"] = "Product based: add eligible products, categories, or manufacturers in Products. Discount value applies to matched items.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.RuleTypeHelp.ComboPricing"] = "Combo pricing: add bundle items in Products and define tiers with fixed bundle price or discounts.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.RuleTypeHelp.BuyXGetY"] = "Buy X Get Y: add buy products and reward products (mark Is reward). Define tiers for buy quantity and reward quantity. Use 'All Products' for progressive cycling discounts (e.g., buy 1 next 50% off, buy 2 next free).",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.RuleTypeHelp.CartCondition"] = "Cart condition: add conditions that must be met. Quantity-based tiers use the matched condition quantity, not unrelated cart lines.",
            ["Admin.NopStation.DiscountManagerPlus.DiscountIntegration.RuleTypeHelp.SubtotalBased"] = "Subtotal based: add conditions based on subtotal. Discount applies to the whole cart.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules"] = "Promotion Rules",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.AddNew"] = "Add new rule",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ManagedByDiscounts"] = "Promotion rules are managed from the default Discounts page. Open <a href=\"{0}\">Discounts</a> to create or update a rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ManagedByDiscounts.Short"] = "Promotion rules are managed from the default Discounts page.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.BackToList"] = "Back to rule list",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.EditDetails"] = "Edit promotion rule",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Info"] = "Rule info",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Products"] = "Products",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Tiers"] = "Tiers",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Conditions"] = "Conditions",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History"] = "Usage history",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SectionsAvailableAfterSave"] = "Save the rule first, then configure products, tiers, and conditions.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.Note"] = "This rule inherits activation and native limit settings from the parent nopCommerce discount. Configure extra DiscountManagerPlus logic here only.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.NativeCustomerLimit"] = "N Times Per Customer is enforced by the parent nopCommerce discount and is not editable as a DiscountManagerPlus condition.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.Fields.DiscountLimitation"] = "Discount limitation",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.Fields.MaximumDiscountAmount"] = "Maximum discount amount",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.Fields.MaximumDiscountedQuantity"] = "Maximum discounted quantity",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountAmount.Unlimited"] = "No maximum amount limit",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ParentDiscount.MaximumDiscountedQuantity.Unlimited"] = "No quantity limit",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.RuleTypeHelp.ProductBased"] = "Apply discount when selected products are in the cart.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.RuleTypeHelp.ComboPricing"] = "Use bundle pricing for a combination of products.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.RuleTypeHelp.BuyXGetY"] = "Configure tiers to define Buy X Get Y behavior. Use 'All Products' for progressive cycling discounts.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.RuleTypeHelp.CartCondition"] = "Apply discount when cart conditions are met. Cart-condition tiers use the matched condition quantity.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.RuleTypeHelp.SubtotalBased"] = "Apply discount when cart subtotal matches configured ranges.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.Title"] = "Setup assistant",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.Default"] = "Select rule type and discount type first. Then save and configure the required sections. Linked rules inherit native limits like maximum discounted quantity from the parent discount.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.ProductBased"] = "Next step: add required products in the Products section.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.ProductBasedFreeItem"] = "Next step: add buy products and mark reward product in the Products section.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.ComboPricing"] = "Next step: add combo products in the Products section. Use fixed bundle price if needed.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.BuyXGetY"] = "Next steps: add products (or use All Products for progressive cycling) and then configure tiers for Buy X Get Y rewards.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.CartCondition"] = "Next step: configure cart conditions in the Conditions section. If you use tiers, their quantity bands follow the matched condition quantity.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupAssistant.SubtotalBased"] = "Next step: configure subtotal ranges in the Conditions section.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.DiscountTypeHelp.Percentage"] = "Percentage discount applies on the matched amount.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.DiscountTypeHelp.FixedAmount"] = "Fixed amount discount deducts a constant value.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.DiscountTypeHelp.FixedBundlePrice"] = "Fixed bundle price sets a target combo price (for combo pricing).",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.DiscountTypeHelp.FreeItem"] = "Free item discount requires reward product configuration.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Title"] = "Configuration checklist",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Products"] = "Products: {0} ({1})",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Tiers"] = "Tiers: {0} ({1})",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Conditions"] = "Conditions: {0} ({1})",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Status.Complete"] = "complete",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Status.Required"] = "required",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupChecklist.Status.NotRequired"] = "not required",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ProductsWithCount"] = "Products ({0})",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.TiersWithCount"] = "Tiers ({0})",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ConditionsWithCount"] = "Conditions ({0})",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.ExcludedProductsWithCount"] = "Excluded Products ({0})",

            // Excluded Products Section
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Description"] = "Products in this list will not receive the promotion discount, even if they match the promotion criteria.",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.AddExcludedProducts"] = "Add Excluded Products",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectProducts"] = "Select products to exclude from this promotion",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.SelectExcludedProducts"] = "Please select at least one product to exclude",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.BulkDelete"] = "Bulk Delete",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Added"] = "{0} product(s) have been added to the exclusion list.",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Deleted"] = "The excluded products have been removed.",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.AddPopup.Description"] = "Select one or more products to exclude from this promotion rule. Excluded products will not receive the discount even if they match the promotion criteria.",

            // Excluded Products Fields
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductId"] = "Product ID",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductName"] = "Product name",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Sku"] = "SKU",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.Price"] = "Price",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.ProductPictureUrl"] = "Product picture",
            ["Admin.NopStation.DiscountManagerPlus.ExcludedProducts.Fields.CreatedOn"] = "Added on",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupStatus.Ready"] = "Ready",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupStatus.NeedsConfiguration"] = "Needs setup",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupDetails.Ready"] = "All required sections are configured.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupDetails.Missing"] = "Missing setup: {0}.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Products"] = "products",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Tiers"] = "tiers",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.SetupSection.Conditions"] = "conditions",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Added"] = "Promotion rule added successfully.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Updated"] = "Promotion rule updated successfully.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Deleted"] = "Promotion rule deleted successfully.",

            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name"] = "Name",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.Hint"] = "The name of the promotion rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.Required"] = "Name is required.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Name.MaxLength"] = "Name must not exceed 400 characters.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName"] = "System name",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.Hint"] = "A unique identifier for this rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.Required"] = "System name is required.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SystemName.MaxLength"] = "System name must not exceed 400 characters.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType"] = "Rule type",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType.Hint"] = "Select the type of promotion rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.RuleType.Required"] = "Please select a rule type.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType"] = "Discount type",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.Hint"] = "Select the discount type (percentage or fixed amount).",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.Required"] = "Please select a discount type.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountType.InvalidForRuleType"] = "Selected discount type is not valid for this rule type.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Discount"] = "Discount",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope"] = "Discount scope",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope.Hint"] = "Choose whether discount is applied only on matched items or on the whole cart.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountScope.Required"] = "Please select a discount scope.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountValue"] = "Discount value",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountValue.Hint"] = "The discount value to apply.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.DiscountValue.Invalid"] = "Discount value must be zero or greater.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Priority"] = "Priority",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.Priority.Hint"] = "Lower values are evaluated first.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsActive"] = "Active",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsActive.Hint"] = "Enable or disable this rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Actions.OpenDiscount"] = "Open discount",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SetupStatus"] = "Setup status",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SetupStatus.Hint"] = "Shows whether required sections for this rule are configured.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SetupDetails"] = "Setup details",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SetupDetails.Hint"] = "Shows what is missing before this rule can apply correctly.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsExclusive"] = "Exclusive (stop further rules)",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsExclusive.Hint"] = "If checked and this rule is eligible, only this rule will be applied and all other promotion rules will be ignored.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LinkedDiscount"] = "Linked nopCommerce discount",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LinkedDiscount.Hint"] = "Optional. Link this promotion rule to an existing nopCommerce discount and make the plugin rule depend on that discount.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LinkedDiscount.Invalid"] = "The selected nopCommerce discount could not be found.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.CarryDefaultDiscount"] = "Carry default discount",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.CarryDefaultDiscount.Hint"] = "When enabled, the linked nopCommerce discount is applied together with this promotion rule. When disabled, only the plugin rule applies.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.StopFurtherRulesForMatchedLines"] = "Stop further rules for matched lines",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.StopFurtherRulesForMatchedLines.Hint"] = "When enabled, lines discounted by this rule won't receive any additional promotion rule discounts.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EnableStackedCumulativeMode"] = "Enable stacked cumulative mode",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EnableStackedCumulativeMode.Hint"] = "For Buy X Get Y rules, apply larger groups first (for example Buy 2 Get 1 Free) and then apply smaller groups (for example Buy 1 Get 50%) on leftover eligible quantity.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsFlashEnabled"] = "Enable flash controls",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.IsFlashEnabled.Hint"] = "Enable usage-window and cap controls for flash bundle campaigns.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitTotal"] = "Total usage limit",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitTotal.Hint"] = "Maximum successful usage count across all customers. 0 = unlimited.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitTotal.Invalid"] = "Usage limit must be 0 or greater.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer"] = "Per-customer usage limit",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.Hint"] = "Maximum successful usage count per customer. 0 = unlimited.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.Invalid"] = "Per-customer usage limit must be 0 or greater.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageLimitPerCustomer.InvalidRange"] = "Per-customer limit can't exceed total limit.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowStartUtc"] = "Usage window start",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowStartUtc.Hint"] = "Start datetime for flash usage controls.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowEndUtc"] = "Usage window end",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowEndUtc.Hint"] = "End datetime for flash usage controls.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UsageWindowEndUtc.InvalidRange"] = "Usage window end must be greater than or equal to usage window start.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.StartDate"] = "Start date",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.StartDate.Hint"] = "The start date for this rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EndDate"] = "End date",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EndDate.Hint"] = "The end date for this rule.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.EndDate.InvalidRange"] = "End date must be greater than or equal to start date.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SelectedStoreIds"] = "Limited to stores",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.SelectedStoreIds.Hint"] = "Optionally limit this rule to specific stores. Leave empty to make it available in all stores.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LimitedToStore"] = "Limited to store",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.LimitedToStore.Hint"] = "Select a store if this rule should only apply to a specific store.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.CreatedOn"] = "Created on",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Fields.UpdatedOn"] = "Updated on",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.CreatedOn"] = "Created on",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Order"] = "Order",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Customer"] = "Customer",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.OrderTotal"] = "Order total",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Discount"] = "Discount applied",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Fields.Source"] = "Usage source",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Source.Plugin"] = "Plugin rule usage",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.ParentDiscount.Note"] = "This rule is bound to the native nopCommerce discount. Usage history below is coming from {0}.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Order.Deleted"] = "Order is deleted",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.History.Customer.Deleted"] = "Deleted customer",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Tools.Validation.RuleNotFound"] = "Promotion rule was not found.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Tools.Validation.Generic"] = "An error occurred while processing your request.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.Name"] = "Name",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.Name.Hint"] = "Filter by rule name.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.RuleType"] = "Rule type",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.RuleType.Hint"] = "Filter by rule type.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.IsActive"] = "Active only",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Search.IsActive.Hint"] = "Show only active rules.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.ProductsRequiredForActivation"] = "To activate this rule, add required products first.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.TiersRequiredForActivation"] = "To activate this Buy X Get Y rule, add at least one tier.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.ConditionsRequiredForActivation"] = "To activate this rule, add at least one condition.",
            ["Admin.NopStation.DiscountManagerPlus.PromotionRules.Validation.RewardProductRequiredForFreeItem"] = "To activate Product Based + Free Item, mark one product as reward product.",

            ["Admin.NopStation.DiscountManagerPlus.Analytics"] = "Promotion Analytics Dashboard",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Search.CreatedFrom"] = "From",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Search.CreatedTo"] = "To",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Search.Store"] = "Store",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.RuleName"] = "Rule",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.RuleType"] = "Rule type",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.IsActive"] = "Active",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.UsageCount"] = "Usage count",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.ImpactedOrders"] = "Impacted orders",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.ImpactedCustomers"] = "Impacted customers",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.TotalDiscount"] = "Total discount given",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.TotalRevenue"] = "Revenue generated",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.AverageDiscountPerOrder"] = "Avg discount / order",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Fields.OrderImpactRate"] = "Order impact %",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalUsage"] = "Total rule uses",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.ImpactedOrders"] = "Impacted orders",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.ImpactedCustomers"] = "Impacted customers",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalDiscount"] = "Total discount given",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalRevenue"] = "Revenue generated",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.TotalOrders"] = "Total orders",
            ["Admin.NopStation.DiscountManagerPlus.Analytics.Summary.OrderImpactRate"] = "Order impact rate",

            ["Admin.NopStation.DiscountManagerPlus.RuleProducts"] = "Rule Products",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.AddNew"] = "Add product",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId"] = "Product",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductId.Hint"] = "Select a product.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ProductName"] = "Product name",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceType"] = "Source type",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceType.Hint"] = "Select what this rule product refers to.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.SourceName"] = "Source",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MinQuantity"] = "Min quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MinQuantity.Hint"] = "Minimum quantity required.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity"] = "Max quantity (0 = unlimited)",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity.Hint"] = "Maximum quantity allowed. Enter 0 for unlimited.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.MaxQuantity.Invalid"] = "Max quantity must be greater than or equal to min quantity.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsRewardProduct"] = "Is reward product",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsRewardProduct.Hint"] = "Mark as a reward product for BOGO rules.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsAllProducts"] = "All products",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.IsAllProducts.Hint"] = "When checked, this rule applies to all products on the platform instead of a specific product, category, manufacturer, or vendor.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeSelectionType"] = "Product attribute filter",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds"] = "Attribute values",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Hint"] = "Select attribute values this product row must match. Works for discounted products, buy products, and reward products.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Required"] = "Select attribute values for this rule product.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Choose"] = "Choose attribute values",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.Clear"] = "Clear",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.RewardAttributeValueIds.SelectProductFirst"] = "Select a product first to choose attribute values.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId"] = "Category",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.CategoryId.Hint"] = "Apply to all products in this category.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId"] = "Manufacturer",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.ManufacturerId.Hint"] = "Apply to all products from this manufacturer.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId"] = "Vendor",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.VendorId.Hint"] = "Apply to all products from this vendor.",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.Product.Choose"] = "Choose product",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Fields.Product.Remove"] = "Remove product",
            ["Admin.NopStation.DiscountManagerPlus.RuleProducts.Validation.RewardProductRequiresProductSource"] = "Reward products must be selected by product.",

            ["Admin.NopStation.DiscountManagerPlus.RuleTiers"] = "Rule Tiers",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.AddNew"] = "Add tier",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MinQuantity"] = "Min quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MinQuantity.Hint"] = "Minimum quantity for this tier. For cart-condition rules, this uses the matched condition quantity.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MinQuantity.Required"] = "Min quantity must be greater than 0.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity"] = "Max quantity (0 = unlimited)",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity.Hint"] = "Maximum quantity for this tier. Enter 0 for unlimited. For cart-condition rules, this uses the matched condition quantity.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.MaxQuantity.Invalid"] = "Max quantity must be 0 or greater than or equal to min quantity.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.QuantityMetric.CartCondition.Note"] = "For cart-condition rules, tier quantities use the quantity that actually matched the configured cart conditions. If the rule only uses context checks like country, role, payment method, or order count, total cart quantity is used as the fallback tier metric.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardQuantity"] = "Reward quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardQuantity.Hint"] = "Number of reward items.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardQuantity.Required"] = "Reward quantity must be greater than 0.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType"] = "Discount type",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType.Hint"] = "The discount type for this tier.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountType.Required"] = "Discount type is required.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountValue"] = "Discount value",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountValue.Hint"] = "The discount value for this tier.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.DiscountValue.Required"] = "Discount value must be greater than 0.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AutoAddReward"] = "Auto-add reward to cart",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AutoAddReward.Hint"] = "Automatically add reward product to cart.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AutoAddReward.Invalid"] = "Auto-add reward is only supported for free-item tiers.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct"] = "Reward product",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct.Hint"] = "The product to give as a reward.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct.Required"] = "Reward mode is required for free-item tiers.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct.Choose"] = "Choose product",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.RewardProduct.Remove"] = "Remove product",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts"] = "Applies to rule products",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts.Hint"] = "Optional. Select specific rule products for this tier. Leave empty to apply this tier to all rule products.",
            ["Admin.NopStation.DiscountManagerPlus.RuleTiers.Fields.AppliesToRuleProducts.All"] = "All rule products",

            ["Admin.NopStation.DiscountManagerPlus.RuleConditions"] = "Rule Conditions",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.AddNew"] = "Add condition",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.AddGroup"] = "Add group",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.EditDetails"] = "Edit condition",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SelectProduct"] = "Select product",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Group.Title"] = "Condition group",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Condition.Title"] = "Condition",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.InteractionTypeInGroup"] = "Join conditions with",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.RemoveGroup"] = "Remove group",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.GroupIsEmpty"] = "No conditions in this group.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Alert.FailedGet"] = "Failed to load conditions. Please try again.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator"] = "Condition",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionOperator.Hint"] = "Select the condition operator.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MinValue"] = "Min value",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MinValue.Hint"] = "Minimum value for this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MaxValue"] = "Max value",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.MaxValue.Hint"] = "Maximum value for this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct"] = "Required product",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredProduct.Hint"] = "Product that must be in the cart.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct"] = "Excluded product",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ExcludedProduct.Hint"] = "Product that must NOT be in the cart.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionRestrictionType"] = "Condition",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType"] = "Condition source",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType.Hint"] = "Select how this condition should evaluate dynamic source values.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceType.Invalid"] = "Invalid condition source type.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData"] = "Source values",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData.Hint"] = "Configure source entries with the builder below. DiscountManagerPlus stores the internal source format automatically.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionSourceData.Required"] = "Provide source values for this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.AddRow"] = "Add source row",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Empty"] = "No source values configured.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.EmptyState"] = "Add one or more source rows for the selected source type.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.LegacyWarning"] = "This condition contains legacy source data. It will be preserved until you change the source builder.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Entry"] = "Source entry",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Entry.Required"] = "Select a source entry before saving this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Attribute.Required"] = "Select an attribute before saving this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Options"] = "Values/options",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Preview"] = "Preview",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Entry.Placeholder"] = "Search or select a source entry",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Options.Placeholder"] = "Select one or more values",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Tokens.Placeholder"] = "Select or enter source values",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.OptionalPlaceholder"] = "Optional",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.MinQuantity"] = "Entry min quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.MaxQuantity"] = "Entry max quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.MinDays"] = "Minimum days",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.MaxDays"] = "Maximum days",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.AnyProduct"] = "Any product",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.SourceBuilder.Range.Invalid"] = "Source row max value must be greater than or equal to min value.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin"] = "Min total quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax"] = "Max total quantity",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin.Hint"] = "Optional. Use this when you need a minimum matched quantity. In cart-condition rules, this filters the matched condition quantity when the condition targets products, categories, vendors, or source values.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax.Hint"] = "Optional. Leave empty unless you need an upper matched quantity limit. In cart-condition rules, this filters the matched condition quantity when the condition targets products, categories, vendors, or source values.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMin.Invalid"] = "Min quantity must be 0 or greater.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityMax.Invalid"] = "Max quantity must be 0 or greater.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityRange"] = "Total quantity range",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.QuantityRange.Invalid"] = "Max quantity must be greater than or equal to min quantity.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCountryCodesCsv"] = "Required countries",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCountryCodesCsv.Hint"] = "Select one or more billing/shipping countries for this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredPaymentMethodsCsv"] = "Required payment methods",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredPaymentMethodsCsv.Hint"] = "Select one or more payment methods for this condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCouponCodesCsv"] = "Required coupon codes",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCouponCodesCsv.Hint"] = "Coupon eligibility is managed by the parent nopCommerce discount and is not configured as a DiscountManagerPlus condition.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin"] = "Min paid/completed order count",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax"] = "Max paid/completed order count",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin.Hint"] = "Optional. Use only this field when you need a minimum paid or completed order count.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax.Hint"] = "Optional. Leave empty unless you need an upper order-count limit.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMin.Invalid"] = "Min order count must be 0 or greater.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountMax.Invalid"] = "Max order count must be 0 or greater.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountRange"] = "Paid/completed order count range",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredOrderCountRange.Invalid"] = "Max order count must be greater than or equal to min order count.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequireSameLineMatch"] = "Require same-line match",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequireSameLineMatch.Hint"] = "When enabled, source/product/category/vendor checks must match on the same cart line.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode"] = "Attribute match mode",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.AttributeMatchMode.Hint"] = "Any = at least one option matches, All = all configured options must match.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup"] = "Logic group",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Hint"] = "Use the same number to keep conditions in one logic block.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ConditionGroup.Invalid"] = "Condition group must be greater than 0.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition"] = "Parent condition",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.Hint"] = "Optional. Select a parent condition to create nested logic in this group.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.None"] = "Root level (no parent)",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.Invalid"] = "Selected parent condition is invalid.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.GroupMismatch"] = "Parent condition must belong to the same logic group.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.SelfReference"] = "A condition cannot be parent of itself.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.ParentCondition.CyclicReference"] = "Parent condition creates a cyclic nested reference.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator"] = "Join with previous row",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator.Hint"] = "Choose how this row combines with previous rows in the same group.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.LogicalOperator.FirstInGroup"] = "Start group",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Summary"] = "Condition details",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Summary.Empty"] = "No condition criteria configured.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Helper.Title"] = "How logic groups work",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Helper.Description"] = "Rows in the same group are combined with AND/OR. Different groups are treated as alternatives (OR).",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Helper.QuickUse"] = "Quick set:",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Helper.NewGroup"] = "New G{0}",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Helper.PreviewPrefix"] = "Preview",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Helper.PreviewOperator"] = "Operator",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.GridHelper.Title"] = "How the rule is evaluated",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.GridHelper.Description"] = "Rows inside the same group follow AND/OR. Different groups are evaluated as OR alternatives.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.GridHelper.Example"] = "Example: G1 Shoes AND VIP, G2 First order => (Shoes AND VIP) OR (First order).",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory"] = "Required category",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCategory.Hint"] = "At least one cart item must belong to this category.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor"] = "Required vendor",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredVendor.Hint"] = "At least one cart item must belong to this vendor.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole"] = "Required customer role",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.RequiredCustomerRole.Hint"] = "Customer must belong to this role.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsFirstOrderOnly"] = "First order only",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsFirstOrderOnly.Hint"] = "Apply only when customer has no previous orders.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsNewCustomerOnly"] = "New customer only",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.IsNewCustomerOnly.Hint"] = "Apply only for non-guest customers created in the last 30 days.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Validation.CriteriaRequired"] = "For cart condition rules, configure at least one condition criteria.",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Product.Choose"] = "Choose product",
            ["Admin.NopStation.DiscountManagerPlus.RuleConditions.Fields.Product.Remove"] = "Remove product",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionLogicalOperator.And"] = "AND",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionLogicalOperator.Or"] = "OR",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionRestrictionType.Include"] = "Include",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionRestrictionType.Exclude"] = "Exclude",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.Products"] = "Products",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.Categories"] = "Categories",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.Manufacturers"] = "Manufacturers",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.Vendors"] = "Vendors",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.SpecificationAttributeOptions"] = "Specification attribute options",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.ProductAttributeValues"] = "Product attribute values",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.DeviceType"] = "Device type",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.SalesChannel"] = "Sales channel",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.CampaignSource"] = "Campaign source",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.ReferralSource"] = "Referral source",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.ConditionSourceType.ExpiryDays"] = "Expiry days",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.AttributeMatchMode.Any"] = "Any",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.AttributeMatchMode.All"] = "All",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.PromotionRuleType.ProductBased"] = "Product based",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.PromotionRuleType.ComboPricing"] = "Combo pricing",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.PromotionRuleType.BuyXGetY"] = "Buy X Get Y",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.PromotionRuleType.CartCondition"] = "Cart condition",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.PromotionRuleType.SubtotalBased"] = "Subtotal based",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RuleProductSourceType.AllProducts"] = "All products",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RuleProductSourceType.Product"] = "Product",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RuleProductSourceType.Category"] = "Category",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RuleProductSourceType.Manufacturer"] = "Manufacturer",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RuleProductSourceType.Vendor"] = "Vendor",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RewardAttributeSelectionType.Any"] = "Any attribute values",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.RewardAttributeSelectionType.SpecificValues"] = "Specific attribute values",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.DiscountScope.MatchedItemsOnly"] = "Matched items only",
            ["Enums.NopStation.Plugin.Misc.DiscountManagerPlus.Domain.DiscountScope.WholeCart"] = "Whole cart",

            ["Plugins.NopStation.DiscountManagerPlus.CartSavings"] = "You saved {0} with promotions!",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Title"] = "Promotion savings",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Total"] = "Total savings",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Eyebrow"] = "Cart promotion overview",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Intro"] = "Review the discounts already applied to this cart and finish any reward selection before checkout. Savings can change if cart quantities or eligible items change.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.TotalHint"] = "Estimated savings currently applied in this cart.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.PendingRewardsHint"] = "Reward items waiting for your option selection.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Metric.AppliedRules"] = "Applied rules",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Metric.PendingRewards"] = "Pending rewards",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Section.AppliedTitle"] = "Applied promotions",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Section.AppliedHint"] = "Each line below shows one promotion rule and the savings it is contributing to the cart right now.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Footer"] = "Promotion totals update automatically when cart products, quantities, or reward selections change.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.RuleLabel"] = "Triggered by",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.QuantityLabel"] = "Reward quantity",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Bogo"] = "BOGO",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Percentage"] = "Percent off",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Fixed"] = "Fixed off",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Badge.Discount"] = "Discount",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoFree"] = "Reward applied on {0} item(s) with free pricing.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.BogoDiscounted"] = "Reward discount applied on {0} item(s).",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Percentage"] = "Percentage discount calculated on eligible line subtotal.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Fixed"] = "Fixed discount distributed across eligible lines.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Detail.Default"] = "Promotion applied on eligible lines.",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Title"] = "Unlock a better offer",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Template"] = "Add {0} more eligible item(s) to unlock {1} in \"{2}\".",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Free"] = "a free item",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Percentage"] = "{0}% off on the next item",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Fixed"] = "{0} off on the next item",
            ["Plugins.NopStation.DiscountManagerPlus.CartSavings.Reminder.Offer.Discount"] = "an extra discount on the next item",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Title"] = "Choose your free reward",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Description"] = "Pick one free product and confirm to add it to your cart.",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.SelectOptions"] = "Select attributes",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.SelectAttributes"] = "Select attributes",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.SelectReward"] = "Choose reward",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Badge.Free"] = "Free",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.AddToCart"] = "Add reward",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.NotEligible"] = "This reward is no longer available for your cart.",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.AddFailed"] = "Could not add the reward to the cart.",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Error.SelectionRequired"] = "Select a reward item before continuing.",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout"] = "Please select your reward item(s) for: {0} before completing checkout.",
            ["Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout.Default"] = "Please select required reward item(s) before completing checkout.",
            ["Plugins.NopStation.DiscountManagerPlus.Requirement.NotEligible"] = "This discount requirement is not eligible for the current cart.",
            ["Plugins.NopStation.DiscountManagerPlus.Requirement.DefaultPipelineDisabled"] = "DiscountManagerPlus default discount integration is disabled.",
            ["Plugins.NopStation.DiscountManagerPlus.PromotionBadge"] = "Promotion available",
            ["Admin.NopStation.DiscountManagerPlus.Requirement.Fields.Conditions"] = "Advanced conditions",
            ["Admin.NopStation.DiscountManagerPlus.Requirement.Fields.Conditions.Hint"] = "Configure advanced DiscountManagerPlus-style conditions for this nopCommerce discount requirement.",
            ["Admin.NopStation.DiscountManagerPlus.Requirement.Create"] = "Create advanced requirement",
            ["Admin.NopStation.DiscountManagerPlus.Requirement.SaveBeforeEdit"] = "Save this requirement first, then add advanced conditions.",
            ["Admin.NopStation.DiscountManagerPlus.Requirement.DefaultPipelineDisabled"] = "Enable the default nopCommerce discount pipeline in DiscountManagerPlus configuration before this requirement can validate discounts.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.PageTitle"] = "Current offers",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Title"] = "All current offers",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Empty"] = "No active offers are available right now.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.LinkText"] = "View all offers",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.RuleType"] = "Rule type",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.ValidFrom"] = "Valid from",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.ValidTo"] = "Valid to",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.AutoApply"] = "This offer is applied automatically at checkout when conditions are met.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Discount.TierBased"] = "Tier-based discount",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Discount.FixedBundlePrice"] = "Bundle price {0}",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Discount.FreeItem"] = "Free item",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ProductBased"] = "Discount on selected products when quantity requirements are met.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ProductBasedFreeItem"] = "Buy selected products and get a free reward item.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Summary.ComboPricing"] = "Special combo pricing applies when required products are purchased together.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Summary.BuyXGetY"] = "Buy X and get Y rewards based on configured tiers.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Summary.CartCondition"] = "Cart-level discount applies when cart conditions are satisfied.",
            ["Plugins.NopStation.DiscountManagerPlus.Offers.Summary.SubtotalBased"] = "Discount applies when your cart subtotal reaches the configured range."
        };
    }

    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            AdminWidgetZones.DiscountDetailsBlock,
            PublicWidgetZones.HeaderAfter,
            PublicWidgetZones.ProductBoxAddinfoAfter,
            PublicWidgetZones.OrderSummaryContentBefore,
            PublicWidgetZones.OrderSummaryCartFooter,
            PublicWidgetZones.OrderSummaryContentAfter
        });
    }

    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone == AdminWidgetZones.DiscountDetailsBlock)
            return typeof(DiscountManagerPlusDiscountDetailsViewComponent);
        if (widgetZone == PublicWidgetZones.OrderSummaryContentBefore)
            return typeof(CartSavingsViewComponent);

        return typeof(PromotionBadgeViewComponent);
    }

    public async Task<DiscountRequirementValidationResult> CheckRequirementAsync(DiscountRequirementValidationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var result = new DiscountRequirementValidationResult { IsValid = false };
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(request.Store?.Id ?? 0);

        if (request.Customer == null || request.Customer.Deleted)
            return result;

        var requirementKind = await _discountManagerPlusRequirementService.GetRequirementKindAsync(request.DiscountRequirementId);
        var isValid = requirementKind switch
        {
            DiscountManagerPlusRequirementKind.LinkedDiscountCarry => !settings.IsEnabled ||
                await _discountManagerPlusService.EvaluateLinkedDiscountRequirementAsync(
                    request.DiscountRequirementId,
                    request.Customer,
                    request.Store?.Id ?? 0),
            _ => settings.IsEnabled &&
                settings.UseDefaultDiscountPipeline &&
                await _discountManagerPlusService.EvaluateDiscountRequirementAsync(
                    request.DiscountRequirementId,
                    request.Customer,
                    request.Store?.Id ?? 0)
        };

        if (isValid)
        {
            result.IsValid = true;
            return result;
        }

        if (!settings.IsEnabled || (requirementKind != DiscountManagerPlusRequirementKind.LinkedDiscountCarry && !settings.UseDefaultDiscountPipeline))
            result.UserError = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Requirement.DefaultPipelineDisabled");
        else
            result.UserError = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.Requirement.NotEligible");

        return result;
    }

    public string GetConfigurationUrl(int discountId, int? discountRequirementId)
    {
        var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

        return urlHelper.Action(
            "Configure",
            "DiscountManagerPlusRequirement",
            new { discountId, discountRequirementId },
            _webHelper.GetCurrentRequestProtocol());
    }

    #endregion
}
