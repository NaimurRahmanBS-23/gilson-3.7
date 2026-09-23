using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public partial class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Configuration.Fields.IsEnabled")]
        public bool IsEnabled { get; set; }
        public bool IsEnabled_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Configuration.Fields.MaxRuleEvaluationTimeMs")]
        public int MaxRuleEvaluationTimeMs { get; set; }
        public bool MaxRuleEvaluationTimeMs_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnablePromotionBadge")]
        public bool EnablePromotionBadge { get; set; }
        public bool EnablePromotionBadge_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnableCartSavingsBreakdown")]
        public bool EnableCartSavingsBreakdown { get; set; }
        public bool EnableCartSavingsBreakdown_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Configuration.Fields.UseDefaultDiscountPipeline")]
        public bool UseDefaultDiscountPipeline { get; set; }
        public bool UseDefaultDiscountPipeline_OverrideForStore { get; set; }

        [NopResourceDisplayName("Admin.NopStation.DiscountManagerPlus.Configuration.Fields.EnableLogging")]
        public bool EnableLogging { get; set; }
        public bool EnableLogging_OverrideForStore { get; set; }

        public int ActiveStoreScopeConfiguration { get; set; }
    }
}
