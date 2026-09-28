using System.Threading;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Services.Customers;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class DiscountManagerPlusCartEventConsumer :
    IConsumer<EntityInsertedEvent<ShoppingCartItem>>,
    IConsumer<EntityUpdatedEvent<ShoppingCartItem>>,
    IConsumer<EntityDeletedEvent<ShoppingCartItem>>
{
    private static readonly AsyncLocal<bool> _synchronizingRewards = new();

    private readonly ICustomerService _customerService;
    private readonly IDiscountManagerPlusService _discountManagerPlusService;

    public DiscountManagerPlusCartEventConsumer(
        ICustomerService customerService,
        IDiscountManagerPlusService discountManagerPlusService)
    {
        _customerService = customerService;
        _discountManagerPlusService = discountManagerPlusService;
    }

    public async Task HandleEventAsync(EntityInsertedEvent<ShoppingCartItem> eventMessage)
    {
        await SynchronizeAsync(eventMessage?.Entity);
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<ShoppingCartItem> eventMessage)
    {
        await SynchronizeAsync(eventMessage?.Entity);
    }

    public async Task HandleEventAsync(EntityDeletedEvent<ShoppingCartItem> eventMessage)
    {
        await SynchronizeAsync(eventMessage?.Entity);
    }

    private async Task SynchronizeAsync(ShoppingCartItem item)
    {
        if (item == null || _synchronizingRewards.Value)
            return;

        if (item.ShoppingCartType != ShoppingCartType.ShoppingCart || item.CustomerId <= 0)
            return;

        var customer = await _customerService.GetCustomerByIdAsync(item.CustomerId);
        if (customer == null || customer.Deleted)
            return;

        try
        {
            _synchronizingRewards.Value = true;
            await _discountManagerPlusService.SynchronizeAutoAddedRewardsAsync(customer, item.StoreId);
        }
        finally
        {
            _synchronizingRewards.Value = false;
        }
    }
}
