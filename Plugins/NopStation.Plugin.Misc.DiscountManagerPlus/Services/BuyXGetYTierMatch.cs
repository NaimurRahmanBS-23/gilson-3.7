using Nop.Core.Domain.Orders;
using NopStation.Plugin.Misc.DiscountManagerPlus.Domain;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Services;

internal class BuyXGetYTierMatch
{
    public PromotionRuleTier Tier { get; set; } = default!;
    public IList<PromotionRuleProduct> BuyProducts { get; set; } = new List<PromotionRuleProduct>();
    public IList<ShoppingCartItem> MatchedBuyItems { get; set; } = new List<ShoppingCartItem>();
    public IList<ShoppingCartItem> RewardItems { get; set; } = new List<ShoppingCartItem>();
    public IDictionary<int, int> RewardAvailableQuantitiesByLineId { get; set; } = new Dictionary<int, int>();
    public int QualifyingBuyQuantity { get; set; }
    public int TotalEligibleQuantity { get; set; }
    public int RewardQuantity { get; set; }
    public bool HasExplicitRewardScope { get; set; }
}
