using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Discounts;
using Nop.Services.Configuration;
using Nop.Services.Discounts;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
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

        public DiscountManagerPlusRequirementKind GetRequirementKind(int discountRequirementId)
        {
            if (discountRequirementId <= 0)
                return DiscountManagerPlusRequirementKind.None;

            var key = string.Format(DiscountManagerPlusDefaults.DiscountRequirementKindSettingKey, discountRequirementId);
            var storedKind = _settingService.GetSettingByKey(key, (int)DiscountManagerPlusRequirementKind.None);
            if (Enum.IsDefined(typeof(DiscountManagerPlusRequirementKind), storedKind) && storedKind > 0)
                return (DiscountManagerPlusRequirementKind)storedKind;

            var discountRequirement = FindDiscountRequirementById(discountRequirementId);
            if (discountRequirement == null)
                return DiscountManagerPlusRequirementKind.None;

            if (discountRequirement.DiscountRequirementRuleSystemName == DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName)
            {
                var conditions = _promotionRuleService.GetRuleConditionsByDiscountRequirementId(discountRequirementId);
                if (conditions.Any())
                    return DiscountManagerPlusRequirementKind.AdvancedConditions;
            }

            return DiscountManagerPlusRequirementKind.None;
        }

        public void MarkAdvancedConditionsRequirement(int discountRequirementId)
        {
            MarkRequirementKind(discountRequirementId, DiscountManagerPlusRequirementKind.AdvancedConditions);
        }

        public void MarkLinkedDiscountCarryRequirement(int discountRequirementId)
        {
            MarkRequirementKind(discountRequirementId, DiscountManagerPlusRequirementKind.LinkedDiscountCarry);
        }

        public void MarkLinkedDiscountWrapperRequirement(int discountRequirementId)
        {
            MarkRequirementKind(discountRequirementId, DiscountManagerPlusRequirementKind.LinkedDiscountWrapper);
        }

        public void DeleteRequirementMetadata(int discountRequirementId)
        {
            if (discountRequirementId <= 0)
                return;

            var key = string.Format(DiscountManagerPlusDefaults.DiscountRequirementKindSettingKey, discountRequirementId);
            _settingService.SetSetting(key, string.Empty);
        }

        public void SyncLinkedDiscountRequirements(int discountId)
        {
            if (discountId <= 0)
                return;

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                return;

            var linkedRules = _promotionRuleService.GetPromotionRulesByDiscountId(discountId);
            var requirements = GetDiscountRequirements(discount);
            var wrapper = GetManagedWrapper(requirements);
            var carryRequirements = GetManagedCarryRequirements(requirements);

            if (!linkedRules.Any())
            {
                foreach (var carryRequirement in carryRequirements)
                    _discountService.DeleteDiscountRequirement(carryRequirement);

                if (wrapper != null)
                {
                    var remainingChildren = GetSiblingRequirements(discount, wrapper, carryRequirements);
                    if (!remainingChildren.Any())
                    {
                        _discountService.DeleteDiscountRequirement(wrapper);
                        DeleteRequirementMetadata(wrapper.Id);
                    }
                }

                return;
            }

            if (wrapper == null)
                wrapper = CreateWrapper(discount);

            var primaryCarryRequirement = carryRequirements.FirstOrDefault();
            if (primaryCarryRequirement == null)
            {
                primaryCarryRequirement = new DiscountRequirement
                {
                    DiscountId = discountId,
                    DiscountRequirementRuleSystemName = DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName
                };

                discount.DiscountRequirements.Add(primaryCarryRequirement);
                _discountService.UpdateDiscount(discount);
            }

            MarkLinkedDiscountCarryRequirement(primaryCarryRequirement.Id);

            foreach (var extraCarryRequirement in carryRequirements.Skip(1))
                _discountService.DeleteDiscountRequirement(extraCarryRequirement);
        }

        public void CleanupManagedDiscountRequirementWrappers()
        {
            var requirements = GetAllDiscountRequirements();
            foreach (var requirement in requirements)
            {
                var kind = GetRequirementKind(requirement.Id);
                if (kind != DiscountManagerPlusRequirementKind.LinkedDiscountWrapper)
                    continue;

                var discount = _discountService.GetDiscountById(requirement.DiscountId);
                var siblings = discount != null
                    ? discount.DiscountRequirements.Where(x => x.Id != requirement.Id).ToList()
                    : new List<DiscountRequirement>();
                if (!siblings.Any())
                    _discountService.DeleteDiscountRequirement(requirement);

                DeleteRequirementMetadata(requirement.Id);
            }
        }

        private void MarkRequirementKind(int discountRequirementId, DiscountManagerPlusRequirementKind kind)
        {
            if (discountRequirementId <= 0)
                return;

            var key = string.Format(DiscountManagerPlusDefaults.DiscountRequirementKindSettingKey, discountRequirementId);
            _settingService.SetSetting(key, (int)kind);
        }

        private DiscountRequirement GetManagedWrapper(IList<DiscountRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                if (requirement.DiscountRequirementRuleSystemName == DiscountManagerPlusDefaults.LinkedDiscountWrapperSystemName &&
                    GetRequirementKind(requirement.Id) == DiscountManagerPlusRequirementKind.LinkedDiscountWrapper)
                    return requirement;
            }

            return null;
        }

        private IList<DiscountRequirement> GetManagedCarryRequirements(IList<DiscountRequirement> requirements)
        {
            var result = new List<DiscountRequirement>();
            foreach (var requirement in requirements.Where(x => x.DiscountRequirementRuleSystemName == DiscountManagerPlusDefaults.DiscountRequirementRuleSystemName))
            {
                if (GetRequirementKind(requirement.Id) == DiscountManagerPlusRequirementKind.LinkedDiscountCarry)
                    result.Add(requirement);
            }

            return result;
        }

        private DiscountRequirement CreateWrapper(Discount discount)
        {
            var wrapper = new DiscountRequirement
            {
                DiscountId = discount.Id,
                DiscountRequirementRuleSystemName = DiscountManagerPlusDefaults.LinkedDiscountWrapperSystemName
            };

            discount.DiscountRequirements.Add(wrapper);
            _discountService.UpdateDiscount(discount);
            MarkLinkedDiscountWrapperRequirement(wrapper.Id);
            return wrapper;
        }

        private DiscountRequirement FindDiscountRequirementById(int discountRequirementId)
        {
            foreach (var requirement in GetAllDiscountRequirements())
            {
                if (requirement.Id == discountRequirementId)
                    return requirement;
            }

            return null;
        }

        private IList<DiscountRequirement> GetDiscountRequirements(Discount discount)
        {
            if (discount == null || discount.DiscountRequirements == null)
                return new List<DiscountRequirement>();

            return discount.DiscountRequirements.ToList();
        }

        private IList<DiscountRequirement> GetAllDiscountRequirements()
        {
            var result = new List<DiscountRequirement>();
            var discounts = _discountService.GetAllDiscounts(null, "", "", true);
            foreach (var discount in discounts)
            {
                if (discount.DiscountRequirements == null)
                    continue;

                result.AddRange(discount.DiscountRequirements);
            }

            return result;
        }

        private IList<DiscountRequirement> GetSiblingRequirements(
            Discount discount,
            DiscountRequirement wrapper,
            IList<DiscountRequirement> carryRequirements)
        {
            if (discount == null || discount.DiscountRequirements == null)
                return new List<DiscountRequirement>();

            return discount.DiscountRequirements
                .Where(x => x.Id != wrapper.Id && !carryRequirements.Any(y => y.Id == x.Id))
                .ToList();
        }
    }
}
