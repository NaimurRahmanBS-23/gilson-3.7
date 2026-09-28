using Nop.Core.Domain.Catalog;
using Nop.Web.Framework.Models;

namespace NopStation.Plugin.Misc.DiscountManagerPlus.Areas.Admin.Models;

public record PromotionRuleRewardAttributePopupModel : BaseNopModel
{
    public PromotionRuleRewardAttributePopupModel()
    {
        ProductAttributes = new List<RewardProductAttributeModel>();
        Warnings = new List<string>();
        RewardAttributeValueIds = string.Empty;
    }

    public int ProductId { get; set; }

    public string RewardAttributeValueIds { get; set; } = string.Empty;

    public IList<RewardProductAttributeModel> ProductAttributes { get; set; } = new List<RewardProductAttributeModel>();

    public IList<string> Warnings { get; set; } = new List<string>();

    public partial record RewardProductAttributeModel : BaseNopEntityModel
    {
        public RewardProductAttributeModel()
        {
            Values = new List<RewardProductAttributeValueModel>();
        }

        public int ProductAttributeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string TextPrompt { get; set; } = string.Empty;

        public bool IsRequired { get; set; }

        public bool HasCondition { get; set; }

        public IList<string> AllowedFileExtensions { get; set; } = new List<string>();

        public AttributeControlType AttributeControlType { get; set; }

        public IList<RewardProductAttributeValueModel> Values { get; set; } = new List<RewardProductAttributeValueModel>();
    }

    public partial record RewardProductAttributeValueModel : BaseNopEntityModel
    {
        public string Name { get; set; } = string.Empty;

        public bool IsPreSelected { get; set; }

        public string PriceAdjustment { get; set; } = string.Empty;

        public decimal PriceAdjustmentValue { get; set; }

        public bool CustomerEntersQty { get; set; }

        public int Quantity { get; set; }
    }
}
