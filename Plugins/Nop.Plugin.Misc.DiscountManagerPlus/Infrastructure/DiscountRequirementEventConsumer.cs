using Nop.Core.Domain.Discounts;
using Nop.Core.Events;
using Nop.Plugin.Misc.DiscountManagerPlus.Services;
using Nop.Services.Events;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Infrastructure
{
    public class DiscountRequirementEventConsumer : IConsumer<EntityDeleted<DiscountRequirement>>
    {
        private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;

        public DiscountRequirementEventConsumer(IDiscountManagerPlusRequirementService discountManagerPlusRequirementService)
        {
            _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
        }

        public void HandleEvent(EntityDeleted<DiscountRequirement> eventMessage)
        {
            if (eventMessage == null || eventMessage.Entity == null)
                return;

            _discountManagerPlusRequirementService.DeleteRequirementMetadata(eventMessage.Entity.Id);
        }
    }
}
