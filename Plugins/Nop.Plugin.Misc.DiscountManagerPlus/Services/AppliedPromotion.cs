using System.Collections.Generic;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class AppliedPromotion
    {
        public AppliedPromotion()
        {
            RuleName = string.Empty;
            TargetProductName = string.Empty;
            EligibleShoppingCartItemIds = new List<int>();
            LineDiscounts = new Dictionary<int, decimal>();
            DiscountedQuantitiesByLineId = new Dictionary<int, int>();
        }

        public int PromotionRuleId { get; set; }
        public string RuleName { get; set; }
        public int RuleTypeId { get; set; }
        public int DiscountTypeId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountValue { get; set; }
        public bool IsExclusive { get; set; }
        public int? RewardProductId { get; set; }
        public int RewardQuantity { get; set; }
        public bool AutoAddReward { get; set; }
        public bool RequiresRewardSelection { get; set; }
        public IList<int> EligibleShoppingCartItemIds { get; set; }
        public IDictionary<int, decimal> LineDiscounts { get; set; }
        public IDictionary<int, int> DiscountedQuantitiesByLineId { get; set; }
        public int? TargetProductId { get; set; }
        public string TargetProductName { get; set; }
        public decimal BenefitValue { get; set; }
        public bool IsAutoUpgradeEligible { get; set; }
    }
}
