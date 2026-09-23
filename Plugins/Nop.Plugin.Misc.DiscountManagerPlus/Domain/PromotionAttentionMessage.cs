using System.Collections.Generic;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Domain
{
    public class PromotionAttentionMessage
    {
        public PromotionAttentionMessage()
        {
            ProductNames = new List<string>();
            ChosenItemReasons = new List<string>();
            SuggestedProductNames = new List<string>();
            RelatedTipIds = new List<string>();
        }

        public int RuleId { get; set; }
        public string RuleName { get; set; }
        public string Message { get; set; }
        public AttentionMessageType MessageType { get; set; }
        public int RequiredAdditionalItems { get; set; }
        public decimal PotentialAdditionalDiscount { get; set; }
        public string ActionUrl { get; set; }
        public IList<string> ProductNames { get; set; }
        public bool IsDualOfferScenario { get; set; }
        public string DiscountChoiceExplanation { get; set; }
        public IList<string> ChosenItemReasons { get; set; }
        public decimal AdditionalSavingsPotential { get; set; }
        public IList<string> SuggestedProductNames { get; set; }
        public int ScenarioComplexity { get; set; }
        public ScenarioType ScenarioType { get; set; }
        public bool IsAllProductsScenario { get; set; }
        public int DisplayPriority { get; set; }
        public bool IsHighlighted { get; set; }
        public string MessageCategory { get; set; }
        public IList<string> RelatedTipIds { get; set; }
    }

    public enum AttentionMessageType
    {
        Info,
        Boost,
        Opportunity,
        Warning
    }
}
