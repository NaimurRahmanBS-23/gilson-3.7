using Nop.Core.Domain.Discounts;
using Nop.Services.Configuration;
using Nop.Services.Discounts;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class DiscountManagerPlusRequirementService : IDiscountManagerPlusRequirementService
{
    private readonly IDiscountService _discountService;
    private readonly IPromotionRuleService _promotionRuleService;
    private readonly ISettingService _settingService;

    public DiscountManagerPlusRequirementService(
        IDiscountService discountService,
        IPromotionRuleService promotionRuleService,
        ISettingService settingService)
    {
        _discountService = discountService;
        _promotionRuleService = promotionRuleService;
        _settingService = settingService;
    }

    public async Task<DiscountManagerPlusRequirementKind> GetRequirementKindAsync(int discountRequirementId)
    {
        if (discountRequirementId <= 0)
            return DiscountManagerPlusRequirementKind.None;

        var key = string.Format(DiscountManagerPlusDefaults.DiscountRequirementKindSettingKey, discountRequirementId);
        var storedKind = await _settingService.GetSettingByKeyAsync(key, (int)DiscountManagerPlusRequirementKind.None);
        if (Enum.IsDefined(typeof(DiscountManagerPlusRequirementKind), storedKind) && storedKind > 0)
            return (DiscountManagerPlusRequirementKind)storedKind;

        var discountRequirement = await _discountService.GetDiscountRequirementByIdAsync(discountRequirementId);
        if (discountRequirement == null)
            return DiscountManagerPlusRequirementKind.None;

        if (discountRequirement.DiscountRequirementRuleSystemName == DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName)
        {
            var conditions = await _promotionRuleService.GetRuleConditionsByDiscountRequirementIdAsync(discountRequirementId);
            if (conditions.Any())
                return DiscountManagerPlusRequirementKind.AdvancedConditions;
        }

        return DiscountManagerPlusRequirementKind.None;
    }

    public Task MarkAdvancedConditionsRequirementAsync(int discountRequirementId)
    {
        return MarkRequirementKindAsync(discountRequirementId, DiscountManagerPlusRequirementKind.AdvancedConditions);
    }

    public Task MarkLinkedDiscountCarryRequirementAsync(int discountRequirementId)
    {
        return MarkRequirementKindAsync(discountRequirementId, DiscountManagerPlusRequirementKind.LinkedDiscountCarry);
    }

    public Task MarkLinkedDiscountWrapperRequirementAsync(int discountRequirementId)
    {
        return MarkRequirementKindAsync(discountRequirementId, DiscountManagerPlusRequirementKind.LinkedDiscountWrapper);
    }

    public async Task DeleteRequirementMetadataAsync(int discountRequirementId)
    {
        if (discountRequirementId <= 0)
            return;

        var key = string.Format(DiscountManagerPlusDefaults.DiscountRequirementKindSettingKey, discountRequirementId);
        await _settingService.SetSettingAsync(key, string.Empty);
    }

    public async Task SyncLinkedDiscountRequirementsAsync(int discountId)
    {
        if (discountId <= 0)
            return;

        var discount = await _discountService.GetDiscountByIdAsync(discountId);
        if (discount == null)
            return;

        var linkedRules = await _promotionRuleService.GetPromotionRulesByDiscountIdAsync(discountId);
        var requirements = await _discountService.GetAllDiscountRequirementsAsync(discountId);
        var wrapper = await GetManagedWrapperAsync(requirements);
        var carryRequirements = await GetManagedCarryRequirementsAsync(requirements);

        if (!linkedRules.Any())
        {
            foreach (var carryRequirement in carryRequirements)
                await _discountService.DeleteDiscountRequirementAsync(carryRequirement, false);

            if (wrapper != null)
            {
                var remainingChildren = (await _discountService.GetDiscountRequirementsByParentAsync(wrapper))
                    .Where(x => !carryRequirements.Any(y => y.Id == x.Id))
                    .ToList();

                if (!remainingChildren.Any())
                {
                    await _discountService.DeleteDiscountRequirementAsync(wrapper, false);
                    await DeleteRequirementMetadataAsync(wrapper.Id);
                }
            }

            return;
        }

        wrapper ??= await CreateWrapperAsync(discountId);

        var rootRequirements = requirements
            .Where(x => !x.ParentId.HasValue && x.Id != wrapper.Id)
            .ToList();

        foreach (var rootRequirement in rootRequirements)
        {
            rootRequirement.ParentId = wrapper.Id;
            await _discountService.UpdateDiscountRequirementAsync(rootRequirement);
        }

        var primaryCarryRequirement = carryRequirements.FirstOrDefault();
        if (primaryCarryRequirement == null)
        {
            primaryCarryRequirement = new DiscountRequirement
            {
                DiscountId = discountId,
                ParentId = wrapper.Id,
                IsGroup = false,
                DiscountRequirementRuleSystemName = DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName
            };

            await _discountService.InsertDiscountRequirementAsync(primaryCarryRequirement);
        }
        else if (primaryCarryRequirement.ParentId != wrapper.Id)
        {
            primaryCarryRequirement.ParentId = wrapper.Id;
            await _discountService.UpdateDiscountRequirementAsync(primaryCarryRequirement);
        }

        await MarkLinkedDiscountCarryRequirementAsync(primaryCarryRequirement.Id);

        foreach (var extraCarryRequirement in carryRequirements.Skip(1))
            await _discountService.DeleteDiscountRequirementAsync(extraCarryRequirement, false);
    }

    public async Task CleanupManagedDiscountRequirementWrappersAsync()
    {
        var requirements = await _discountService.GetAllDiscountRequirementsAsync();
        foreach (var requirement in requirements)
        {
            var kind = await GetRequirementKindAsync(requirement.Id);
            if (kind != DiscountManagerPlusRequirementKind.LinkedDiscountWrapper)
                continue;

            var childRequirements = await _discountService.GetDiscountRequirementsByParentAsync(requirement);
            if (!childRequirements.Any())
                await _discountService.DeleteDiscountRequirementAsync(requirement, false);

            await DeleteRequirementMetadataAsync(requirement.Id);
        }
    }

    private async Task MarkRequirementKindAsync(int discountRequirementId, DiscountManagerPlusRequirementKind kind)
    {
        if (discountRequirementId <= 0)
            return;

        var key = string.Format(DiscountManagerPlusDefaults.DiscountRequirementKindSettingKey, discountRequirementId);
        await _settingService.SetSettingAsync(key, (int)kind);
    }

    private async Task<DiscountRequirement> GetManagedWrapperAsync(IList<DiscountRequirement> requirements)
    {
        foreach (var requirement in requirements.Where(x => !x.ParentId.HasValue && x.IsGroup))
        {
            if (await GetRequirementKindAsync(requirement.Id) == DiscountManagerPlusRequirementKind.LinkedDiscountWrapper)
                return requirement;
        }

        return null;
    }

    private async Task<IList<DiscountRequirement>> GetManagedCarryRequirementsAsync(IList<DiscountRequirement> requirements)
    {
        var result = new List<DiscountRequirement>();
        foreach (var requirement in requirements.Where(x => !x.IsGroup && x.DiscountRequirementRuleSystemName == DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName))
        {
            if (await GetRequirementKindAsync(requirement.Id) == DiscountManagerPlusRequirementKind.LinkedDiscountCarry)
                result.Add(requirement);
        }

        return result;
    }

    private async Task<DiscountRequirement> CreateWrapperAsync(int discountId)
    {
        var wrapper = new DiscountRequirement
        {
            DiscountId = discountId,
            IsGroup = true,
            InteractionType = RequirementGroupInteractionType.And,
            DiscountRequirementRuleSystemName = DiscountManagerPlusDefaults.LinkedDiscountWrapperSystemName
        };

        await _discountService.InsertDiscountRequirementAsync(wrapper);
        await MarkLinkedDiscountWrapperRequirementAsync(wrapper.Id);
        return wrapper;
    }

}
