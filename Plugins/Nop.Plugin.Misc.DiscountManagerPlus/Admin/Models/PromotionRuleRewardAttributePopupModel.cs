using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Misc.DiscountManagerPlus.Admin.Models
{
    public class PromotionRuleRewardAttributePopupModel : BaseNopModel
    {
        public PromotionRuleRewardAttributePopupModel()
        {
            ProductAttributes = new List<RewardProductAttributeModel>();
            Warnings = new List<string>();
            RewardAttributeValueIds = string.Empty;
        }

        public int ProductId { get; set; }

        public string RewardAttributeValueIds { get; set; }

        public IList<RewardProductAttributeModel> ProductAttributes { get; set; }

        public IList<string> Warnings { get; set; }

        public partial class RewardProductAttributeModel : BaseNopEntityModel
        {
            public RewardProductAttributeModel()
            {
                Values = new List<RewardProductAttributeValueModel>();
                Name = string.Empty;
                TextPrompt = string.Empty;
                AllowedFileExtensions = new List<string>();
            }

            public int ProductAttributeId { get; set; }

            public string Name { get; set; }

            public string TextPrompt { get; set; }

            public bool IsRequired { get; set; }

            public bool HasCondition { get; set; }

            public IList<string> AllowedFileExtensions { get; set; }

            public AttributeControlType AttributeControlType { get; set; }

            public IList<RewardProductAttributeValueModel> Values { get; set; }
        }

        public partial class RewardProductAttributeValueModel : BaseNopEntityModel
        {
            public RewardProductAttributeValueModel()
            {
                Name = string.Empty;
                PriceAdjustment = string.Empty;
            }

            public string Name { get; set; }

            public bool IsPreSelected { get; set; }

            public string PriceAdjustment { get; set; }

            public decimal PriceAdjustmentValue { get; set; }

            public bool CustomerEntersQty { get; set; }

            public int Quantity { get; set; }
        }
    }
}
