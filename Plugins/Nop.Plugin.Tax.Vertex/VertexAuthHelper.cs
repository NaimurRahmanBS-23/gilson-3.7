using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;
using Nop.Services.Logging;

namespace Nop.Plugin.Tax.Vertex
{
    public static class VertexAuthHelper
    {
        public static string GetVertexOAuthToken()
        {
            VertexSettings vertexSettings = null;

            try
            {
                var settingService = EngineContext.Current.Resolve<ISettingService>();
                vertexSettings = settingService.LoadSetting<VertexSettings>();

                if (vertexSettings == null)
                {
                    LogAuthError("Vertex auth failed: VertexSettings could not be loaded.", null);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(vertexSettings.ClientId) ||
                    string.IsNullOrWhiteSpace(vertexSettings.ClientSecret) ||
                    string.IsNullOrWhiteSpace(vertexSettings.AuthUrl))
                {
                    LogAuthError(
                        "Vertex auth failed: One or more required settings are missing.",
                        "ClientId empty=" + string.IsNullOrWhiteSpace(vertexSettings.ClientId)
                        + " | ClientSecret empty=" + string.IsNullOrWhiteSpace(vertexSettings.ClientSecret)
                        + " | AuthUrl empty=" + string.IsNullOrWhiteSpace(vertexSettings.AuthUrl));
                    return null;
                }

                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                    var postData = new NameValueCollection
                    {
                        { "client_id",     vertexSettings.ClientId },
                        { "client_secret", vertexSettings.ClientSecret },
                        { "grant_type",    vertexSettings.GrantType },
                        { "audience",      vertexSettings.Audience }
                    };

                    byte[] responseBytes;
                    try
                    {
                        responseBytes = client.UploadValues(vertexSettings.AuthUrl, "POST", postData);
                    }
                    catch (WebException webEx)
                    {
                        var statusDesc = webEx.Response is HttpWebResponse httpResp
                            ? "HTTP " + (int)httpResp.StatusCode + " " + httpResp.StatusDescription
                            : "no HTTP response";

                        LogAuthError(
                            "Vertex auth HTTP request failed: " + statusDesc,
                            "AuthUrl=" + vertexSettings.AuthUrl + " | " + webEx.Message,
                            webEx);
                        return null;
                    }

                    var responseBody = Encoding.UTF8.GetString(responseBytes);

                    dynamic json;
                    try
                    {
                        json = JsonConvert.DeserializeObject(responseBody);
                    }
                    catch (Exception parseEx)
                    {
                        LogAuthError(
                            "Vertex auth response JSON parse failed.",
                            "AuthUrl=" + vertexSettings.AuthUrl + " | ResponseBody=" + Truncate(responseBody, 500),
                            parseEx);
                        return null;
                    }

                    string token = null;
                    try { token = (string)json.access_token; } catch { }

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        LogAuthError(
                            "Vertex auth succeeded but access_token was missing or empty.",
                            "AuthUrl=" + vertexSettings.AuthUrl + " | ResponseBody=" + Truncate(responseBody, 500));
                        return null;
                    }

                    return token;
                }
            }
            catch (Exception ex)
            {
                LogAuthError(
                    "Vertex auth unexpected exception.",
                    "AuthUrl=" + (vertexSettings != null ? vertexSettings.AuthUrl : "unknown"),
                    ex);
                return null;
            }
        }

        private static void LogAuthError(string shortMessage, string detail = null, Exception ex = null)
        {
            try
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                var fullMessage = detail;

                if (ex != null)
                    fullMessage = (string.IsNullOrEmpty(fullMessage) ? "" : fullMessage + "\r\n\r\n") + ex.ToString();

                // Use a lightweight exception to carry fullMessage into the FullMessage DB column.
                // nopCommerce writes exception.ToString() into FullMessage; passing null leaves it blank.
                var carrier = string.IsNullOrEmpty(fullMessage)
                    ? null
                    : new Exception(fullMessage, ex);

                logger.Error(shortMessage, carrier, null);
            }
            catch { }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max) + "...(truncated)";
        }
    }
}
