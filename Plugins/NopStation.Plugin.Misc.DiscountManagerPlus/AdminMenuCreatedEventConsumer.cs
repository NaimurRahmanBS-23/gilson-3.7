using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Menu;
using NopStation.Plugin.Misc.Core;
using NopStation.Plugin.Misc.Core.Infrastructure;

namespace NopStation.Plugin.Misc.DiscountManagerPlus;

public class AdminMenuCreatedEventConsumer : IConsumer<AdminMenuEvent>
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IPermissionService _permissionService;

    #endregion

    #region Ctor

    public AdminMenuCreatedEventConsumer(
        ILocalizationService localizationService,
        IPermissionService permissionService)
    {
        _localizationService = localizationService;
        _permissionService = permissionService;
    }

    #endregion

    #region Methods

    public async Task HandleEventAsync(AdminMenuEvent createdEvent)
    {
        var canManageConfiguration = await _permissionService.AuthorizeAsync(DiscountManagerPlusPermissionProvider.MANAGE_CONFIGURATION);
        var canManageRules = await _permissionService.AuthorizeAsync(DiscountManagerPlusPermissionProvider.MANAGE_PROMOTION_RULES);

        if (!canManageConfiguration && !canManageRules)
            return;

        var menuItem = new NopStationAdminMenuItem
        {
            Title = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.Menu.DiscountManagerPlus"),
            Visible = true,
            IconClass = "fas fa-tags"
        };

        if (canManageRules)
        {
            var rulesNode = new AdminMenuItem
            {
                Visible = true,
                IconClass = "far fa-circle",
                Url = "~/Admin/PromotionRule/List",
                SystemName = "DiscountManagerPlus.Rules",
                Title = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.Menu.PromotionRules")
            };
            menuItem.ChildNodes.Add(rulesNode);

            var analyticsNode = new AdminMenuItem
            {
                Visible = true,
                IconClass = "far fa-circle",
                Url = "~/Admin/PromotionAnalytics/List",
                SystemName = "DiscountManagerPlus.Analytics",
                Title = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.Menu.Analytics")
            };
            menuItem.ChildNodes.Add(analyticsNode);
        }

        if (canManageConfiguration)
        {
            var configNode = new AdminMenuItem
            {
                Visible = true,
                IconClass = "far fa-circle",
                Url = "~/Admin/DiscountManagerPlus/Configure",
                SystemName = "DiscountManagerPlus.Configuration",
                Title = await _localizationService.GetResourceAsync("Admin.NopStation.DiscountManagerPlus.Menu.Configuration")
            };
            menuItem.ChildNodes.Add(configNode);
        }

        if (await _permissionService.AuthorizeAsync(CorePermissionProvider.SHOW_DOCUMENTATIONS))
        {
            var documentation = new AdminMenuItem
            {
                Title = await _localizationService.GetResourceAsync("Admin.NopStation.Common.Menu.Documentation"),
                Url = "https://www.nop-station.com/discount-manager-plus-documentation?utm_source=admin-panel&utm_medium=products&utm_campaign=discount-manager-plus",
                Visible = true,
                IconClass = "far fa-circle",
                OpenUrlInNewTab = true
            };
            menuItem.ChildNodes.Add(documentation);
        }

        createdEvent.PluginChildNodes.Add(menuItem);
    }

    #endregion
}
