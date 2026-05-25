using System.Web.Routing;
using Nop.Core.Plugins;

namespace Nop.Plugin.Fix.SubtotalCoupon
{
    /// <summary>
    /// Plugin entry point.
    ///
    /// This plugin has no administration UI or database tables — it is a pure
    /// service-layer patch.  All work is done in:
    ///   • Services/FixedPriceCalculationService.cs  — the corrected logic
    ///   • Infrastructure/DependencyRegistrar.cs     — the IoC override
    ///
    /// Installation / Uninstallation simply delegate to the base class so that
    /// nopCommerce records the plugin as installed/uninstalled in its plugin list.
    /// </summary>
    public class SubtotalCouponFixPlugin : BasePlugin
    {
        public override void Install()
        {
            base.Install();
        }

        public override void Uninstall()
        {
            base.Uninstall();
        }
    }
}
