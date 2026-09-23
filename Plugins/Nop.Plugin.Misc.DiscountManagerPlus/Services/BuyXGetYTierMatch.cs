using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.DiscountManagerPlus.Domain;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    internal class BuyXGetYTierMatch
    {
        public BuyXGetYTierMatch()
        {
            BuyProducts = new List<PromotionRuleProduct>();
            MatchedBuyItems = new List<ShoppingCartItem>();
            RewardItems = new List<ShoppingCartItem>();
            RewardAvailableQuantitiesByLineId = new Dictionary<int, int>();
        }

        public PromotionRuleTier Tier { get; set; }
        public IList<PromotionRuleProduct> BuyProducts { get; set; }
        public IList<ShoppingCartItem> MatchedBuyItems { get; set; }
        public IList<ShoppingCartItem> RewardItems { get; set; }
        public IDictionary<int, int> RewardAvailableQuantitiesByLineId { get; set; }
        public int QualifyingBuyQuantity { get; set; }
        public int TotalEligibleQuantity { get; set; }
        public int RewardQuantity { get; set; }
        public bool HasExplicitRewardScope { get; set; }
    }
}
