namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public interface IDiscountManagerPlusRequirementService
    {
        DiscountManagerPlusRequirementKind GetRequirementKind(int discountRequirementId);
        void MarkAdvancedConditionsRequirement(int discountRequirementId);
        void MarkLinkedDiscountCarryRequirement(int discountRequirementId);
        void MarkLinkedDiscountWrapperRequirement(int discountRequirementId);
        void DeleteRequirementMetadata(int discountRequirementId);
        void SyncLinkedDiscountRequirements(int discountId);
        void CleanupManagedDiscountRequirementWrappers();
    }
}
