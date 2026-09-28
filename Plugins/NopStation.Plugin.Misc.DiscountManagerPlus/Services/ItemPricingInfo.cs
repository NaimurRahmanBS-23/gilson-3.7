namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

/// <summary>
/// Helper class for discount allocation calculations
/// </summary>
internal class ItemPricingInfo
{
    public int LineId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineSubtotal { get; set; }
}
