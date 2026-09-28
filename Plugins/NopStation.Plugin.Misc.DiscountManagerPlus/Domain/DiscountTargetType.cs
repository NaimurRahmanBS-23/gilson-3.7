namespace NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

public enum DiscountTargetType
{
    /// <summary>
    /// Discount applies only to items matching the rule condition (default)
    /// </summary>
    SameProduct = 0,

    /// <summary>
    /// Discount applies to cheapest item in the matched category
    /// </summary>
    CheapestInCategory = 1,

    /// <summary>
    /// Discount applies to cheapest item in entire cart
    /// </summary>
    CheapestInCart = 2
}
