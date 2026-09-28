using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.DiscountManagerPlus
{
    public class DiscountManagerPlusSettings : ISettings
    {
        public bool IsEnabled { get; set; }
        public bool EnableCartSavingsBreakdown { get; set; }
        public bool UseDefaultDiscountPipeline { get; set; }
        public bool EnableLogging { get; set; }
    }
}
