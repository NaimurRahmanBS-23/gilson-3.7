using System;
using System.Web.Routing;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Web.Framework.Menu;
using System.Linq;
using Bss.Nop.Plugin.Custom.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Tasks;
using Nop.Core.Domain.Tasks;

namespace Bss.Nop.Plugin.Custom
{
    public class BssCustomPlugin : BasePlugin, IMiscPlugin, IAdminMenuPlugin
    {
        #region Fields
        private readonly ISettingService _settingService;
        private readonly BssCustomObjectContext _objectContext;
        #endregion

        #region Ctor
        public BssCustomPlugin(ISettingService settingService, BssCustomObjectContext objectContext)
        {
            _settingService = settingService;
            _objectContext = objectContext;
        }
        #endregion

        #region Methods
        public void ManageSiteMap(SiteMapNode rootNode)
        {
            var routeValues = new RouteValueDictionary();
            routeValues.Add("area", null);
            routeValues.Add("systemname", "bayshore.custom");

            var menuItem = new SiteMapNode()
            {
                SystemName = "Bss.Nop.Plugin.Custom",
                Title = "Bayshore Custom Shipping",
                ControllerName = "BssCustomAdmin",
                ActionName = "Configure",
                Visible = true,
                RouteValues = routeValues
            };

            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "Third party plugins");

            if (pluginNode != null)
            {
                pluginNode.ChildNodes.Add(menuItem);
            }
            else
            {
                rootNode.ChildNodes.Add(menuItem);
            }
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "BssCustomAdmin";
            routeValues = new RouteValueDictionary { { "Namespaces", "Bss.Nop.Plugin.Custom.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            //database objects
            _objectContext.Install();

            base.Install();
        }

        public override void Uninstall()
        {
            //database objects
            _objectContext.Uninstall();

            base.Uninstall();
        }
        #endregion
    }
}
