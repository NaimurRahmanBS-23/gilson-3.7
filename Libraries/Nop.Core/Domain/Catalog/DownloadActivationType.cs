namespace Nop.Core.Domain.Catalog
{
    /// <summary>
    /// Represents a download activation type
    /// </summary>
    public enum DownloadActivationType
    {
        /// <summary>
        /// Automatically
        /// </summary>
        Automatically = 0,
        /// <summary>
        /// When order is paid
        /// </summary>
        WhenOrderIsPaid = 1,
        /// <summary>
        /// Manually
        /// </summary>
        Manually = 10,
    }
}
