using System.Data.Entity;
using System.Web.Mvc;
using Autofac;
using Autofac.Core;
using Bss.Nop.Plugin.Custom.Data;
using Bss.Nop.Plugin.Custom.ServiceOverrides;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Services.Shipping;
using Nop.Web.Framework.Mvc;

namespace Bss.Nop.Plugin.Custom.Infrastructure
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        private const string CONTEXT_NAME = "nop_object_context_bsscustom";

        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //data context
            this.RegisterPluginDataContext<BssCustomObjectContext>(builder, CONTEXT_NAME);

            // register factory overrides
            builder.RegisterType<BssShippingService>().As<IShippingService>()
                .WithParameter(ResolvedParameter.ForNamed<ICacheManager>("nop_cache_static"))
                .InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 100; }
        }
    }
}
