namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class PromotionRuleAnalyticsSummary
    {
        public int PromotionRuleId { get; set; }
        public string RuleName { get; set; }
        public int RuleTypeId { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public int ImpactedOrdersCount { get; set; }
        public int ImpactedCustomersCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalRevenueAmount { get; set; }
    }
}
