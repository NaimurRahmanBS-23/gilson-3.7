namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

/// <summary>
/// Actionable tip for maximizing savings through cart optimization, such as
/// adding specific products or increasing quantities to trigger better discounts.
/// </summary>
public class SavingsMaximizationTip
{
    /// <summary>
    /// Unique identifier for this tip type
    /// </summary>
    public string TipId { get; set; }

    /// <summary>
    /// Human-readable message explaining the maximization opportunity
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// The type of maximization opportunity (add product, increase quantity, etc.)
    /// </summary>
    public MaximizationType MaximizationType { get; set; }

    /// <summary>
    /// The potential additional savings if customer acts on this tip
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// List of suggested product names to add (if applicable)
    /// </summary>
    public IList<string> SuggestedProducts { get; set; } = new List<string>();

    /// <summary>
    /// List of suggested product IDs to add (if applicable)
    /// </summary>
    public IList<int> SuggestedProductIds { get; set; } = new List<int>();

    /// <summary>
    /// The quantity of suggested products to add
    /// </summary>
    public int SuggestedQuantity { get; set; }

    /// <summary>
    /// Which product to increase quantity for (if applicable)
    /// </summary>
    public string TargetProductName { get; set; }

    /// <summary>
    /// The target product ID for quantity increase (if applicable)
    /// </summary>
    public int? TargetProductId { get; set; }

    /// <summary>
    /// Current quantity of target product in cart
    /// </summary>
    public int CurrentQuantity { get; set; }

    /// <summary>
    /// Recommended quantity to achieve for maximization
    /// </summary>
    public int RecommendedQuantity { get; set; }

    /// <summary>
    /// Which promotion/rule this tip relates to
    /// </summary>
    public string RelatedPromotionName { get; set; }

    /// <summary>
    /// The priority of this tip (higher = more impactful)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this is a quick win (easy to achieve, high impact)
    /// </summary>
    public bool IsQuickWin { get; set; }

    /// <summary>
    /// Estimated time to achieve this maximization (in minutes)
    /// </summary>
    public int? EstimatedTimeToAchieve { get; set; }

    /// <summary>
    /// Additional context or details about this tip
    /// </summary>
    public string AdditionalDetails { get; set; }

    /// <summary>
    /// Action URL for quick implementation (if applicable)
    /// </summary>
    public string ActionUrl { get; set; }

    /// <summary>
    /// Whether this tip has been acted upon by the customer
    /// </summary>
    public bool IsActedUpon { get; set; }

    /// <summary>
    /// Timestamp when this tip was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expiration time for this tip (if限时 offer)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Type of maximization opportunity available to the customer
/// </summary>
public enum MaximizationType
{
    /// <summary>
    /// Add a specific product to trigger additional discounts
    /// </summary>
    AddProduct = 1,

    /// <summary>
    /// Increase quantity of existing product to reach next discount tier
    /// </summary>
    IncreaseQuantity = 2,

    /// <summary>
    /// Add different product to diversify cart for better allocation
    /// </summary>
    AddDifferentProduct = 3,

    /// <summary>
    /// Replace existing product with better value alternative
    /// </summary>
    ReplaceProduct = 4,

    /// <summary>
    /// Bundle multiple products for combo discount
    /// </summary>
    CreateBundle = 5,

    /// <summary>
    /// Upgrade to premium product for better value
    /// </summary>
    UpgradeProduct = 6,

    /// <summary>
    /// General tip for maximizing overall savings
    /// </summary>
    GeneralOptimization = 7
}