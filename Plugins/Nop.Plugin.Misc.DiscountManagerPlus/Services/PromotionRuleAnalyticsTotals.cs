namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class PromotionRuleAnalyticsTotals
    {
        public int UsageCount { get; set; }
        public int ImpactedOrdersCount { get; set; }
        public int ImpactedCustomersCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalRevenueAmount { get; set; }
    }
}
