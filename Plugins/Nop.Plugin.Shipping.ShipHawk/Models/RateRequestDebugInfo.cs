using System;

namespace Nop.Plugin.Shipping.ShipHawk.Models
{
    /// <summary>
    /// Thread-safe container for debug information captured during parallel rate requests.
    /// This information is logged AFTER parallel execution completes (sequential context).
    /// </summary>
    public class RateRequestDebugInfo
    {
        /// <summary>
        /// Warehouse code for identification in logs
        /// </summary>
        public string WarehouseCode { get; set; }

        /// <summary>
        /// Request URL (for tracing)
        /// </summary>
        public string RequestUrl { get; set; }

        /// <summary>
        /// Request body JSON (for tracing)
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Response JSON (for tracing)
        /// </summary>
        public string ResponseJson { get; set; }

        /// <summary>
        /// Error response from API (for tracing)
        /// </summary>
        public string ErrorResponse { get; set; }

        /// <summary>
        /// Exception that occurred during request (always logged)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Custom error message on failure
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Indicates if this debug info has any content worth logging
        /// </summary>
        public bool HasContent =>
            !string.IsNullOrEmpty(RequestUrl) ||
            !string.IsNullOrEmpty(RequestBody) ||
            !string.IsNullOrEmpty(ResponseJson) ||
            !string.IsNullOrEmpty(ErrorResponse) ||
            Exception != null;
    }
}