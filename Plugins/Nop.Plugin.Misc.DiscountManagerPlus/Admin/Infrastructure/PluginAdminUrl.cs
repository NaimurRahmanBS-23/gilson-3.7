namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Infrastructure
{
    public static class PluginAdminUrl
    {
        public static string Combine(string url, string query)
        {
            if (string.IsNullOrEmpty(query))
                return url ?? string.Empty;
            if (string.IsNullOrEmpty(url))
                return query;
            return url + (url.IndexOf('?') >= 0 ? "&" : "?") + query;
        }
    }
}
