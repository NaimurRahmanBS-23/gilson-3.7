using System.Collections.Generic;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class ConditionMatchResult
    {
        public ConditionMatchResult()
        {
            MatchedShoppingCartItemIds = new List<int>();
        }

        public bool IsMatched { get; set; }
        public bool HasQuantityMetric { get; set; }
        public int MatchedQuantity { get; set; }
        public IList<int> MatchedShoppingCartItemIds { get; set; }
    }
}
