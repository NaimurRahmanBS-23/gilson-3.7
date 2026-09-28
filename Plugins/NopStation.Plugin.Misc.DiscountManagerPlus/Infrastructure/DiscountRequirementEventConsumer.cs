using Nop.Core.Domain.Discounts;
using Nop.Core.Events;
using Nop.Services.Events;
using NopStation.Plugin.Misc.DiscountManagerPlus.Services;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Infrastructure;

public class DiscountRequirementEventConsumer : IConsumer<EntityDeletedEvent<DiscountRequirement>>
{
    private readonly IDiscountManagerPlusRequirementService _discountManagerPlusRequirementService;

    public DiscountRequirementEventConsumer(IDiscountManagerPlusRequirementService discountManagerPlusRequirementService)
    {
        _discountManagerPlusRequirementService = discountManagerPlusRequirementService;
    }

    public async Task HandleEventAsync(EntityDeletedEvent<DiscountRequirement> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        await _discountManagerPlusRequirementService.DeleteRequirementMetadataAsync(eventMessage.Entity.Id);
    }
}
