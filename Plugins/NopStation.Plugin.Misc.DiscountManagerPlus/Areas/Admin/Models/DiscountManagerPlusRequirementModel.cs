namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public class DiscountManagerPlusRequirementModel
{
    public int DiscountId { get; set; }

    public int RequirementId { get; set; }

    public bool DefaultDiscountPipelineEnabled { get; set; }

    public PromotionRuleConditionSearchModel ConditionSearchModel { get; set; } = new();
}
