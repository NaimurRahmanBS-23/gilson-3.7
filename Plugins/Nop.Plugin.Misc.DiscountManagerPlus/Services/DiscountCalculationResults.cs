using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Services
{
    public class ComboSetCountResult
    {
        public ComboSetCountResult()
        {
        }

        public ComboSetCountResult(int setCount, decimal matchedSubtotal)
        {
            SetCount = setCount;
            MatchedSubtotal = matchedSubtotal;
        }

        public int SetCount { get; set; }
        public decimal MatchedSubtotal { get; set; }
    }

    public class RewardDiscountResult
    {
        public RewardDiscountResult()
        {
            LineDiscounts = new Dictionary<int, decimal>();
        }

        public RewardDiscountResult(decimal discountAmount, Dictionary<int, decimal> lineDiscounts)
        {
            DiscountAmount = discountAmount;
            LineDiscounts = lineDiscounts ?? new Dictionary<int, decimal>();
        }

        public decimal DiscountAmount { get; set; }
        public Dictionary<int, decimal> LineDiscounts { get; set; }
    }

    public class RewardDiscountQuantizedResult
    {
        public RewardDiscountQuantizedResult()
        {
            LineDiscounts = new Dictionary<int, decimal>();
            DiscountedQuantities = new Dictionary<int, int>();
        }

        public RewardDiscountQuantizedResult(decimal discountAmount, Dictionary<int, decimal> lineDiscounts, Dictionary<int, int> discountedQuantities)
        {
            DiscountAmount = discountAmount;
            LineDiscounts = lineDiscounts ?? new Dictionary<int, decimal>();
            DiscountedQuantities = discountedQuantities ?? new Dictionary<int, int>();
        }

        public decimal DiscountAmount { get; set; }
        public Dictionary<int, decimal> LineDiscounts { get; set; }
        public Dictionary<int, int> DiscountedQuantities { get; set; }

        public Dictionary<int, int> DiscountedQuantitiesByLineId
        {
            get { return DiscountedQuantities; }
            set { DiscountedQuantities = value; }
        }
    }

    public class DiscountMapsResult
    {
        public DiscountMapsResult()
        {
            LineDiscountMap = new Dictionary<int, decimal>();
            RuleDiscountMap = new Dictionary<int, decimal>();
        }

        public DiscountMapsResult(Dictionary<int, decimal> lineDiscountMap, Dictionary<int, decimal> ruleDiscountMap)
        {
            LineDiscountMap = lineDiscountMap ?? new Dictionary<int, decimal>();
            RuleDiscountMap = ruleDiscountMap ?? new Dictionary<int, decimal>();
        }

        public Dictionary<int, decimal> LineDiscountMap { get; set; }
        public Dictionary<int, decimal> RuleDiscountMap { get; set; }
    }

    public class CappedLineDiscountResult
    {
        public CappedLineDiscountResult()
        {
            LineDiscounts = new Dictionary<int, decimal>();
            QuantitiesByLineId = new Dictionary<int, int>();
        }

        public CappedLineDiscountResult(Dictionary<int, decimal> lineDiscounts, Dictionary<int, int> quantitiesByLineId)
        {
            LineDiscounts = lineDiscounts ?? new Dictionary<int, decimal>();
            QuantitiesByLineId = quantitiesByLineId ?? new Dictionary<int, int>();
        }

        public Dictionary<int, decimal> LineDiscounts { get; set; }
        public Dictionary<int, int> QuantitiesByLineId { get; set; }
    }

    internal class PricedCartLine
    {
        public ShoppingCartItem Item { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
