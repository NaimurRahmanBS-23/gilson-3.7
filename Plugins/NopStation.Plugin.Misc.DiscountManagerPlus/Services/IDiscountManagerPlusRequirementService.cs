using Nop.Core.Domain.Discounts;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public interface IDiscountManagerPlusRequirementService
{
    Task<DiscountManagerPlusRequirementKind> GetRequirementKindAsync(int discountRequirementId);

    Task MarkAdvancedConditionsRequirementAsync(int discountRequirementId);

    Task MarkLinkedDiscountCarryRequirementAsync(int discountRequirementId);

    Task MarkLinkedDiscountWrapperRequirementAsync(int discountRequirementId);

    Task DeleteRequirementMetadataAsync(int discountRequirementId);

    Task SyncLinkedDiscountRequirementsAsync(int discountId);

    Task CleanupManagedDiscountRequirementWrappersAsync();
}
