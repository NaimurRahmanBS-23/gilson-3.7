using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;
using NopStation.Plugin.Misc.Core.Controllers;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Controllers;


public class DiscountManagerPlusController : NopStationAdminController
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public DiscountManagerPlusController(
        ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    #endregion

    #region Methods

    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_CONFIGURATION)]
    public async Task<IActionResult> Configure()
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(storeScope);

        var model = new ConfigurationModel
        {
            IsEnabled = settings.IsEnabled,
            MaxRuleEvaluationTimeMs = settings.MaxRuleEvaluationTimeMs,
            EnablePromotionBadge = settings.EnablePromotionBadge,
            EnableCartSavingsBreakdown = settings.EnableCartSavingsBreakdown,
            UseDefaultDiscountPipeline = settings.UseDefaultDiscountPipeline,
            ActiveStoreScopeConfiguration = storeScope
        };

        if (storeScope > 0)
        {
            model.IsEnabled_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.IsEnabled, storeScope);
            model.MaxRuleEvaluationTimeMs_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.MaxRuleEvaluationTimeMs, storeScope);
            model.EnablePromotionBadge_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.EnablePromotionBadge, storeScope);
            model.EnableCartSavingsBreakdown_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.EnableCartSavingsBreakdown, storeScope);
            model.UseDefaultDiscountPipeline_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.UseDefaultDiscountPipeline, storeScope);
        }

        return View(model);
    }

    [HttpPost]
    [CheckPermission(DiscountManagerPlusPermissionProvider.MANAGE_CONFIGURATION)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(storeScope);

        settings.IsEnabled = model.IsEnabled;
        settings.MaxRuleEvaluationTimeMs = model.MaxRuleEvaluationTimeMs;
        settings.EnablePromotionBadge = model.EnablePromotionBadge;
        settings.EnableCartSavingsBreakdown = model.EnableCartSavingsBreakdown;
        settings.UseDefaultDiscountPipeline = model.UseDefaultDiscountPipeline;

        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.IsEnabled, model.IsEnabled_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.MaxRuleEvaluationTimeMs, model.MaxRuleEvaluationTimeMs_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.EnablePromotionBadge, model.EnablePromotionBadge_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.EnableCartSavingsBreakdown, model.EnableCartSavingsBreakdown_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.UseDefaultDiscountPipeline, model.UseDefaultDiscountPipeline_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.Configuration.Saved"));

        return await Configure();
    }

    #endregion
}
