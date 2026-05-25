using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using Newtonsoft.Json;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Services.Shipping;

namespace Bss.Nop.Plugin.Custom
{
    public class BssCustomSettings : ISettings
    {
        // Admin settings
        public decimal AdditionalHandlingCharge { get; set; }
        public decimal FedexShippingAdjustment { get; set; }
        public decimal FedexFreightShippingAdjustment { get; set; }
        public bool IsAdditionalHandlingChargePercent { get; set; }
        public bool IsFedexShippingAdjustmentPercent { get; set; }
        public bool IsFedexFreightShippingAdjustmentPercent { get; set; }

    }
}
