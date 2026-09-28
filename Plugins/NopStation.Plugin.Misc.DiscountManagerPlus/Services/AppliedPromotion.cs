namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class AppliedPromotion
{
    public int PromotionRuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int RuleTypeId { get; set; }
    public int DiscountTypeId { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// NEW: The discount percentage or fixed value (e.g., 50 for 50% off)
    /// </summary>
    public decimal DiscountValue { get; set; }

    public bool IsExclusive { get; set; }
    public int? RewardProductId { get; set; }
    public int RewardQuantity { get; set; }
    public bool AutoAddReward { get; set; }
    public bool RequiresRewardSelection { get; set; }
    public IList<int> EligibleShoppingCartItemIds { get; set; } = new List<int>();
    public IDictionary<int, decimal> LineDiscounts { get; set; } = new Dictionary<int, decimal>();
    public IDictionary<int, int> DiscountedQuantitiesByLineId { get; set; } = new Dictionary<int, int>();

    public int? TargetProductId { get; set; }

    public string TargetProductName { get; set; } = string.Empty;

    public decimal BenefitValue { get; set; }

    public bool IsAutoUpgradeEligible { get; set; }
}
