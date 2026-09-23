using System.Collections.Generic;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class ComboSetMatchSummary
    {
        public ComboSetMatchSummary()
        {
            QuantitiesByLineId = new Dictionary<int, int>();
        }

        public int SetCount { get; set; }
        public decimal MatchedSubtotal { get; set; }
        public IDictionary<int, int> QuantitiesByLineId { get; set; }
    }
}
