using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class CheckoutPendingRewardSelectionFilter : IAsyncActionFilter
{
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;

    public CheckoutPendingRewardSelectionFilter(
        ISettingService settingService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IShoppingCartService shoppingCartService,
        IDiscountManagerPlusService discountManagerPlusService,
        ILocalizationService localizationService,
        INotificationService notificationService)
    {
        _settingService = settingService;
        _storeContext = storeContext;
        _workContext = workContext;
        _shoppingCartService = shoppingCartService;
        _discountManagerPlusService = discountManagerPlusService;
        _localizationService = localizationService;
        _notificationService = notificationService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!IsCheckoutConfirmationPost(context))
        {
            await next();
            return;
        }

        var store = await _storeContext.GetCurrentStoreAsync();
        var settings = await _settingService.LoadSettingAsync<DiscountManagerPlusSettings>(store.Id);
        if (!settings.IsEnabled)
        {
            await next();
            return;
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || customer.Deleted)
        {
            await next();
            return;
        }

        await _discountManagerPlusService.SynchronizeAutoAddedRewardsAsync(customer, store.Id);

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
        {
            await next();
            return;
        }

        var pendingPromotions = (await _discountManagerPlusService.EvaluateCartAsync(cart, store.Id))
            .Where(x => x.RequiresRewardSelection)
            .ToList();
        if (!pendingPromotions.Any())
        {
            await next();
            return;
        }

        var pendingRuleNames = pendingPromotions
            .Select(x => x.RuleName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var warningTemplate = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout");
        var fallbackWarning = await _localizationService.GetResourceAsync("Plugins.NopStation.DiscountManagerPlus.RewardSelection.Warning.Checkout.Default");
        var warning = string.IsNullOrWhiteSpace(warningTemplate) || !pendingRuleNames.Any()
            ? fallbackWarning
            : string.Format(warningTemplate, string.Join(", ", pendingRuleNames.Take(3)));

        if (IsOpcConfirmRequest(context))
        {
            context.Result = new JsonResult(new { error = 1, message = warning });
            return;
        }

        _notificationService.ErrorNotification(warning);
        context.Result = new RedirectResult("~/cart");
    }

    private static bool IsCheckoutConfirmationPost(ActionExecutingContext context)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
            return false;

        var controller = context.RouteData.Values.TryGetValue("controller", out var controllerObj)
            ? controllerObj?.ToString()
            : string.Empty;
        if (!controller.Equals("Checkout", StringComparison.OrdinalIgnoreCase))
            return false;

        var action = context.RouteData.Values.TryGetValue("action", out var actionObj)
            ? actionObj?.ToString()
            : string.Empty;

        return action.Equals("Confirm", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("OpcConfirmOrder", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOpcConfirmRequest(ActionExecutingContext context)
    {
        var action = context.RouteData.Values.TryGetValue("action", out var actionObj)
            ? actionObj?.ToString()
            : string.Empty;

        if (action.Equals("OpcConfirmOrder", StringComparison.OrdinalIgnoreCase))
            return true;

        var requestedWith = context.HttpContext.Request.Headers["X-Requested-With"].ToString();
        return requestedWith.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }
}
