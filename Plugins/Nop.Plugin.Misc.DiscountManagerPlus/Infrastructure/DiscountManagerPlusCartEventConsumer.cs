using System.Web;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Customers;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class DiscountManagerPlusCartEventConsumer :
        IConsumer<EntityInserted<ShoppingCartItem>>,
        IConsumer<EntityUpdated<ShoppingCartItem>>,
        IConsumer<EntityDeleted<ShoppingCartItem>>
    {
        private const string SynchronizingRewardsKey = "NopStation.DiscountManagerPlus.SynchronizingRewards";

        private readonly ICustomerService _customerService;
        private readonly IDiscountManagerPlusService _discountManagerPlusService;

        public DiscountManagerPlusCartEventConsumer(
            ICustomerService customerService,
            IDiscountManagerPlusService discountManagerPlusService)
        {
            _customerService = customerService;
            _discountManagerPlusService = discountManagerPlusService;
        }

        public void HandleEvent(EntityInserted<ShoppingCartItem> eventMessage)
        {
            Synchronize(eventMessage != null ? eventMessage.Entity : null);
        }

        public void HandleEvent(EntityUpdated<ShoppingCartItem> eventMessage)
        {
            Synchronize(eventMessage != null ? eventMessage.Entity : null);
        }

        public void HandleEvent(EntityDeleted<ShoppingCartItem> eventMessage)
        {
            Synchronize(eventMessage != null ? eventMessage.Entity : null);
        }

        private void Synchronize(ShoppingCartItem item)
        {
            if (item == null || IsSynchronizing())
                return;

            if (item.ShoppingCartType != ShoppingCartType.ShoppingCart || item.CustomerId <= 0)
                return;

            var customer = _customerService.GetCustomerById(item.CustomerId);
            if (customer == null || customer.Deleted)
                return;

            try
            {
                SetSynchronizing(true);
                _discountManagerPlusService.SynchronizeAutoAddedRewards(customer, item.StoreId);
            }
            finally
            {
                SetSynchronizing(false);
            }
        }

        private static bool IsSynchronizing()
        {
            var context = HttpContext.Current;
            return context != null && context.Items.Contains(SynchronizingRewardsKey);
        }

        private static void SetSynchronizing(bool value)
        {
            var context = HttpContext.Current;
            if (context == null)
                return;

            if (value)
                context.Items[SynchronizingRewardsKey] = true;
            else
                context.Items.Remove(SynchronizingRewardsKey);
        }
    }
}
