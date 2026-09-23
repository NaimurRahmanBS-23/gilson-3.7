using System.Collections.Generic;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class DualOfferExplanation
    {
        public DualOfferExplanation()
        {
            DiscountChoices = new List<DiscountChoiceDetail>();
            ChoiceReasoning = new List<string>();
            AppliedPromotionNames = new List<string>();
        }

        public bool IsDualOfferScenario { get; set; }
        public string ExplanationText { get; set; }
        public IList<DiscountChoiceDetail> DiscountChoices { get; set; }
        public decimal TotalSavings { get; set; }
        public IList<string> ChoiceReasoning { get; set; }
        public ScenarioType ScenarioType { get; set; }
        public int TotalProducts { get; set; }
        public int TotalUnits { get; set; }
        public bool IsAllProductsScenario { get; set; }
        public decimal SavingsPercentage { get; set; }
        public IList<string> AppliedPromotionNames { get; set; }
        public bool HasMaximizationOpportunities { get; set; }
        public decimal PotentialAdditionalSavings { get; set; }
    }

    public class DiscountChoiceDetail
    {
        public string PromotionName { get; set; }
        public string DiscountType { get; set; }
        public string ProductName { get; set; }
        public int? UnitNumber { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public string Reasoning { get; set; }
        public int Priority { get; set; }
        public bool IsFreeItem { get; set; }
        public int RewardQuantity { get; set; }
    }

    public enum ScenarioType
    {
        SingleProductSingleUnit = 1,
        SingleProductMultipleUnits = 2,
        MultipleProductsMixedUnits = 3,
        MultipleProductsAllSingleUnits = 4
    }
}
