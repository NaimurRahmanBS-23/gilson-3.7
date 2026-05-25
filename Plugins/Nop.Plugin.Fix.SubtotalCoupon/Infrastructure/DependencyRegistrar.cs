using Autofac;
using Autofac.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Fix.SubtotalCoupon.Services;
using Nop.Services.Catalog;

namespace Nop.Plugin.Fix.SubtotalCoupon.Infrastructure
{
    /// <summary>
    /// Replaces the built-in IPriceCalculationService registration with our
    /// patched implementation using Autofac's InstancePerLifetimeScope so the
    /// override follows nopCommerce's standard per-request lifetime.
    ///
    /// Because nopCommerce processes plugin registrars after core registrars,
    /// this registration automatically wins (last registration wins in Autofac).
    /// No core files need to be modified.
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// A high order value ensures this registrar runs after all core
        /// registrars, so our registration takes precedence.
        /// </summary>
        public int Order => 10;

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            // Replace IPriceCalculationService with our fixed implementation.
            // Autofac resolves the last registration for a given service type,
            // so this override is transparent to every consumer of the interface.
            builder.RegisterType<FixedPriceCalculationService>()
                   .As<IPriceCalculationService>()
                   .InstancePerLifetimeScope();
        }
    }
}
