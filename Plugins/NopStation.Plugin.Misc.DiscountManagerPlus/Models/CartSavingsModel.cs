using Nop.Web.Models.Catalog;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Models;

public class CartSavingsModel
{
    public string SummaryText { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TotalLabel { get; set; } = string.Empty;

    public decimal TotalSavings { get; set; }

    public string TotalSavingsFormatted { get; set; } = string.Empty;

    public IList<CartSavingsItemModel> Items { get; set; } = new List<CartSavingsItemModel>();

    public string PendingRewardsTitle { get; set; } = string.Empty;

    public string PendingRewardsDescription { get; set; } = string.Empty;

    public string PendingRewardsSelectText { get; set; } = string.Empty;

    public string PendingRewardsAddText { get; set; } = string.Empty;

    public IList<CartRewardSelectionModel> PendingRewards { get; set; } = new List<CartRewardSelectionModel>();

    public string ReminderTitle { get; set; } = string.Empty;

    public IList<string> Reminders { get; set; } = new List<string>();

    /// <summary>
    /// Messages about excluded products from store-wide offers
    /// </summary>
    public IList<string> ExcludedProductWarnings { get; set; } = new List<string>();

    /// <summary>
    /// Messages about multiple simultaneous discounts applied
    /// </summary>
    public IList<string> MultipleDiscountNotices { get; set; } = new List<string>();

    /// <summary>
    /// Details about which items were selected for cheapest-item discounts
    /// </summary>
    public IList<string> CheapestItemSelectionDetails { get; set; } = new List<string>();

    // NEW: Enhanced dual-offer messaging properties

    /// <summary>
    /// Attention messages generated for dual-offer scenarios
    /// </summary>
    public IList<PromotionAttentionMessage> AttentionMessages { get; set; } = new List<PromotionAttentionMessage>();

    /// <summary>
    /// Whether there are any attention messages to display
    /// </summary>
    public bool HasAttentionMessages => AttentionMessages?.Any() == true;

    /// <summary>
    /// Comprehensive explanation of dual-offer discount allocation
    /// </summary>
    public DualOfferExplanation DualOfferExplanation { get; set; }

    /// <summary>
    /// Whether this is a dual-offer scenario
    /// </summary>
    public bool IsDualOfferScenario => DualOfferExplanation?.IsDualOfferScenario == true;

    /// <summary>
    /// Maximization tips for additional savings opportunities
    /// </summary>
    public IList<SavingsMaximizationTip> MaximizationTips { get; set; } = new List<SavingsMaximizationTip>();

    /// <summary>
    /// Whether there are maximization tips available
    /// </summary>
    public bool HasMaximizationTips => MaximizationTips?.Any() == true;

    /// <summary>
    /// Potential additional savings from maximization tips
    /// </summary>
    public decimal PotentialAdditionalSavings => MaximizationTips?.Sum(t => t.PotentialSavings) ?? 0;
}

public class CartSavingsItemModel
{
    public int PromotionRuleId { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public string BadgeText { get; set; } = string.Empty;

    public string DetailText { get; set; } = string.Empty;

    public bool IsBogoStyle { get; set; }

    public decimal DiscountAmount { get; set; }

    public string DiscountAmountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// NEW: Target product that received the discount (for transparency)
    /// </summary>
    public string TargetProductName { get; set; } = string.Empty;

    /// <summary>
    /// NEW: Full display text "Applied to: [Product Name]"
    /// </summary>
    public string AppliedToText { get; set; } = string.Empty;

    /// <summary>
    /// NEW: Type of discount (e.g., "50% off", "Free", "$10 off")
    /// </summary>
    public string DiscountType { get; set; } = string.Empty;

    /// <summary>
    /// NEW: Details about how the cheapest item was selected
    /// </summary>
    public string TargetSelectionText { get; set; } = string.Empty;

    /// <summary>
    /// NEW: Whether this is part of a coordinated multi-discount scenario
    /// </summary>
    public bool IsCoordinatedDiscount { get; set; }

    /// <summary>
    /// NEW: Priority level of this discount (for coordinated scenarios)
    /// </summary>
    public int DiscountPriority { get; set; }
}

public class CartRewardSelectionModel
{
    public int PromotionRuleId { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public int RewardQuantity { get; set; }

    public int MaxSelectableQuantity { get; set; }

    public string FormId { get; set; } = string.Empty;

    public string PopupId { get; set; } = string.Empty;

    public string AddToCartUrl { get; set; } = string.Empty;

    public IList<CartRewardOptionModel> Options { get; set; } = new List<CartRewardOptionModel>();
}

public class CartRewardOptionModel
{
    public int RewardProductId { get; set; }

    public string RewardProductName { get; set; } = string.Empty;

    public string RewardProductOldPrice { get; set; } = string.Empty;

    public string RewardProductPrice { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsSelected { get; set; }

    public ProductDetailsModel Product { get; set; } = new();
}
