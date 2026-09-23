namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    internal class ItemPricingInfo
    {
        public int LineId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineSubtotal { get; set; }
    }
}
