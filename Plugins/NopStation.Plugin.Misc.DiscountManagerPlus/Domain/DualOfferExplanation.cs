namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

/// <summary>
/// Comprehensive explanation of how dual-offer discounts were applied to a cart,
/// including reasoning for item selection and detailed breakdown of savings.
/// </summary>
public class DualOfferExplanation
{
    /// <summary>
    /// Whether this is a dual-offer scenario (multiple promotions applied simultaneously)
    /// </summary>
    public bool IsDualOfferScenario { get; set; }

    /// <summary>
    /// Human-readable explanation text describing how discounts were applied
    /// </summary>
    public string ExplanationText { get; set; }

    /// <summary>
    /// Detailed breakdown of each discount choice made, including which product received
    /// which discount and why.
    /// </summary>
    public IList<DiscountChoiceDetail> DiscountChoices { get; set; } = new List<DiscountChoiceDetail>();

    /// <summary>
    /// Total savings achieved through coordinated discount allocation
    /// </summary>
    public decimal TotalSavings { get; set; }

    /// <summary>
    /// List of reasoning points explaining the allocation decisions
    /// </summary>
    public IList<string> ChoiceReasoning { get; set; } = new List<string>();

    /// <summary>
    /// The detected scenario type (single-product, multi-product, etc.)
    /// </summary>
    public ScenarioType ScenarioType { get; set; }

    /// <summary>
    /// Total number of different products in the cart
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// Total number of units (quantity) across all products
    /// </summary>
    public int TotalUnits { get; set; }

    /// <summary>
    /// Whether this was an "all products" store-wide promotion scenario
    /// </summary>
    public bool IsAllProductsScenario { get; set; }

    /// <summary>
    /// The percentage of total cart value saved through dual offers
    /// </summary>
    public decimal SavingsPercentage { get; set; }

    /// <summary>
    /// Additional context about the promotion rules that were applied
    /// </summary>
    public IList<string> AppliedPromotionNames { get; set; } = new List<string>();

    /// <summary>
    /// Whether there are additional maximization opportunities available
    /// </summary>
    public bool HasMaximizationOpportunities { get; set; }

    /// <summary>
    /// The potential additional savings if customer acts on maximization tips
    /// </summary>
    public decimal PotentialAdditionalSavings { get; set; }
}

/// <summary>
/// Detailed information about a specific discount choice made during coordination.
/// </summary>
public class DiscountChoiceDetail
{
    /// <summary>
    /// The name of the promotion/rule that provided this discount
    /// </summary>
    public string PromotionName { get; set; }

    /// <summary>
    /// The type of discount applied (e.g., "100% off", "50% off")
    /// </summary>
    public string DiscountType { get; set; }

    /// <summary>
    /// The name of the product that received this discount
    /// </summary>
    public string ProductName { get; set; }

    /// <summary>
    /// The specific unit number of the product that was discounted (when applicable)
    /// </summary>
    public int? UnitNumber { get; set; }

    /// <summary>
    /// The original price of the item before discount
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// The discount amount that was applied
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// The final price after discount was applied
    /// </summary>
    public decimal FinalPrice { get; set; }

    /// <summary>
    /// Human-readable reasoning explaining why this specific item was chosen
    /// </summary>
    public string Reasoning { get; set; }

    /// <summary>
    /// The priority level of this discount (higher priorities get processed first)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this was a free item (100% discount)
    /// </summary>
    public bool IsFreeItem { get; set; }

    /// <summary>
    /// The reward quantity this discount represents
    /// </summary>
    public int RewardQuantity { get; set; }
}

/// <summary>
/// Scenario type enumeration for context-aware messaging
/// </summary>
public enum ScenarioType
{
    /// <summary>
    /// Single product with only one unit in cart
    /// </summary>
    SingleProductSingleUnit = 1,

    /// <summary>
    /// Single product with multiple units in cart
    /// </summary>
    SingleProductMultipleUnits = 2,

    /// <summary>
    /// Multiple products with mixed quantities (some have multiple units)
    /// </summary>
    MultipleProductsMixedUnits = 3,

    /// <summary>
    /// Multiple products, all with single units
    /// </summary>
    MultipleProductsAllSingleUnits = 4
}