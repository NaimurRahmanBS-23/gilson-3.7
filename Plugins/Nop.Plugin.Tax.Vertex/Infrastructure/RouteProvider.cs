using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Tax.Vertex.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(
            name: "Plugin.Tax.Vertex.CustomerTaxExemption",
            url: "customer/tax-exemption",
            defaults: new { controller = "VertexCert", action = "Index", area = "" },
            namespaces: new[] { "Nop.Plugin.Tax.Vertex.Controllers" }
        );
    }

        public int Priority
        {
            get { return 0; }
        }
    }


}