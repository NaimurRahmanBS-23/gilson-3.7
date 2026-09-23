namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public class DiscountManagerPlusRequirementModel
    {
        public DiscountManagerPlusRequirementModel()
        {
            ConditionSearchModel = new PromotionRuleConditionSearchModel();
        }

        public int DiscountId { get; set; }

        public int RequirementId { get; set; }

        public bool DefaultDiscountPipelineEnabled { get; set; }

        public PromotionRuleConditionSearchModel ConditionSearchModel { get; set; }
    }
}
