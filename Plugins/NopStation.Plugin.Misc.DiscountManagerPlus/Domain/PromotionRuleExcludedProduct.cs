using Nop.Core;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

/// <summary>
/// Represents a product excluded from a promotion rule
/// </summary>
public class PromotionRuleExcludedProduct : BaseEntity
{
    /// <summary>
    /// Gets or sets the promotion rule identifier
    /// </summary>
    public int PromotionRuleId { get; set; }

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the exclusion was created
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}
