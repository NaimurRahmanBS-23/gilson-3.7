using Nop.Core.Configuration;

namespace NopStation.Plugin.Misc.DiscountManagerPlus;

public class DiscountManagerPlusSettings : ISettings
{
    public bool IsEnabled { get; set; }
    public int MaxRuleEvaluationTimeMs { get; set; }
    public bool EnablePromotionBadge { get; set; }
    public bool EnableCartSavingsBreakdown { get; set; } = true;
    public bool UseDefaultDiscountPipeline { get; set; }
}
