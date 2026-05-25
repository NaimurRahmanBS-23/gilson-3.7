using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routes;

namespace Bss.Nop.Plugin.Custom.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            ViewEngines.Engines.Insert(0, new BssCustomViewEngine());

            #region Admin

            routes.MapRoute("Bss.Nop.Plugin.Custom.Admin",
                "Admin/Plugin/ConfigureMiscPlugin",
                new {controller = "BssCustomAdmin", action = "Configure"},
                new[] {"Bss.Nop.Plugin.Custom.Controllers"}
            );

            routes.MapRoute("Bss.Nop.Plugin.Custom.Settings.Save",
                "Admin/Plugins/Custom/Settings/Save",
                new
                {
                    controller = "BssCustomAdmin",
                    action = "Save",
                    form = UrlParameter.Optional,
                    model = UrlParameter.Optional
                },
                new[] {"Bss.Nop.Plugin.Custom.Controllers"}
            );
            routes.MapRoute("Bss.Nop.Plugin.Custom.Admin.GetUSPSConfig",
                "Plugins/Bss/Admin/GetUSPSConfig",
                  new
                   {
                    controller = "USPSTab",
                    action = "GetUSPSConfig",
                     productId = UrlParameter.Optional
                  },
                new[] { "Bss.Nop.Plugin.Custom.Admin.Controllers" }
            );

            routes.MapRoute("Bss.Nop.Plugin.Custom.Admin.SaveUSPSConfig",
               "Plugins/Bss/Admin/SaveUSPSConfig",
                    new { controller = "USPSTab", action = "SaveUSPSConfig" },
                new[] { "Bss.Nop.Plugin.Custom.Admin.Controllers" }
                );
            #endregion


        }

        public int Priority
        {
            get
            {
                return 1;   // Override all registered routes.
            }
        }
    }
}
