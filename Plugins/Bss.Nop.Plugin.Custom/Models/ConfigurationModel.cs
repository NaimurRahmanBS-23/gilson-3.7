using Nop.Web.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bss.Nop.Plugin.Custom.Models
{
    public class ConfigurationModel
    {
        public BssCustomSettings Settings { get; set; }

        public int SelectedTabIndex { get; set; }

        public string AuthenticationGuid { get; set; }
        [NopResourceDisplayName("Plugins.Bss.Fields.AdditionalHandlingCharge")]
        public decimal AdditionalHandlingCharge { get; set; }
        [NopResourceDisplayName("Plugins.Bss.Fields.FedexShippingAdjustment")]
        public decimal FedexShippingAdjustment { get; set; }
        [NopResourceDisplayName("Plugins.Bss.Fields.FedexFreightShippingAdjustment")]
        public decimal FedexFreightShippingAdjustment { get; set; }


    }
}
