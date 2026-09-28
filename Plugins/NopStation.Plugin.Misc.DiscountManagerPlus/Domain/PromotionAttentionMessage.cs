namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

/// <summary>
/// Represents an attention/boost message for promotions with enhanced dual-offer support
/// </summary>
public class PromotionAttentionMessage
{
    public int RuleId { get; set; }
    public string RuleName { get; set; }
    public string Message { get; set; }
    public AttentionMessageType MessageType { get; set; }
    public int RequiredAdditionalItems { get; set; }
    public decimal PotentialAdditionalDiscount { get; set; }
    public string ActionUrl { get; set; }
    public IList<string> ProductNames { get; set; } = new List<string>();

    // NEW: Dual-offer specific properties
    /// <summary>
    /// Whether this message is related to a dual-offer scenario (multiple simultaneous promotions)
    /// </summary>
    public bool IsDualOfferScenario { get; set; }

    /// <summary>
    /// Detailed explanation of why specific items were chosen for discounts
    /// </summary>
    public string DiscountChoiceExplanation { get; set; }

    /// <summary>
    /// List of reasoning points explaining the allocation decisions
    /// </summary>
    public IList<string> ChosenItemReasons { get; set; } = new List<string>();

    /// <summary>
    /// Additional potential savings beyond current discounts
    /// </summary>
    public decimal AdditionalSavingsPotential { get; set; }

    /// <summary>
    /// Suggested product names for maximizing savings
    /// </summary>
    public IList<string> SuggestedProductNames { get; set; } = new List<string>();

    /// <summary>
    /// Scenario complexity level (1 = single product, 2 = multiple products, etc.)
    /// </summary>
    public int ScenarioComplexity { get; set; }

    /// <summary>
    /// The detected scenario type for contextual messaging
    /// </summary>
    public ScenarioType ScenarioType { get; set; }

    /// <summary>
    /// Whether this is an "all products" store-wide promotion scenario
    /// </summary>
    public bool IsAllProductsScenario { get; set; }

    /// <summary>
    /// Priority level for message display (higher = more important)
    /// </summary>
    public int DisplayPriority { get; set; }

    /// <summary>
    /// Whether this message should be highlighted/prominently displayed
    /// </summary>
    public bool IsHighlighted { get; set; }

    /// <summary>
    /// Message category for organizing different types of tips
    /// </summary>
    public string MessageCategory { get; set; }

    /// <summary>
    /// Associated maximization tips related to this message
    /// </summary>
    public IList<string> RelatedTipIds { get; set; } = new List<string>();
}

/// <summary>
/// Types of attention messages
/// </summary>
public enum AttentionMessageType
{
    Info,
    Boost,
    Opportunity,
    Warning
}
