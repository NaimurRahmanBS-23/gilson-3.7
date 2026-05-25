using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.Vertex
{
    public class VertexSettings : ISettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthUrl { get; set; }
        public string TokenUrl { get; set; }
        public string ApiUrl { get; set; }        
        // OAuth parameters
        public string GrantType { get; set; } = "client_credentials";
        public string Audience { get; set; } = "verx://migration-api";
        
        public string SellerCode { get; set; }        
        public string SellerStreet1 { get; set; }
        public string SellerCity { get; set; }
        public string SellerState { get; set; }
        public string SellerPostal { get; set; }
        public string SellerCountry { get; set; }

        public string EcwClientId { get; set; }
        public string EcwClientSecret { get; set; }

        public string PartitionUuid { get; set; }


        // Logging controls
        public bool EnableVerboseLogging { get; set; } = false;
        public bool LogBodies { get; set; } = false;         // only honored if verbose is true
        public int BodySampleBytes { get; set; } = 2000;     // truncate big payloads
        public bool LogPiiCityStateZip { get; set; } = true; // allow city, state, zip in logs
    }
}
