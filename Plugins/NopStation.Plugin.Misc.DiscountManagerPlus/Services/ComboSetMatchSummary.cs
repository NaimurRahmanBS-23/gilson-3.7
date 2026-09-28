namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class ComboSetMatchSummary
{
    public int SetCount { get; set; }
    public decimal MatchedSubtotal { get; set; }
    public IDictionary<int, int> QuantitiesByLineId { get; set; } = new Dictionary<int, int>();
}
