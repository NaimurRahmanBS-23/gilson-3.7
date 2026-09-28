namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

public class ConditionMatchResult
{
    public bool IsMatched { get; set; }
    public bool HasQuantityMetric { get; set; }
    public int MatchedQuantity { get; set; }
    public IList<int> MatchedShoppingCartItemIds { get; set; } = new List<int>();
}
