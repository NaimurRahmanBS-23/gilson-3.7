using Nop.Core.Infrastructure;
using Nop.Services.Configuration;
using Nop.Services.Logging;

namespace Nop.Plugin.Misc.DiscountManagerPlus
{
    public static class DiscountManagerPlusLog
    {
        public static void Information(ILogger logger, string message)
        {
            if (logger == null || string.IsNullOrWhiteSpace(message) || !IsEnabled())
                return;

            logger.Information(message);
        }

        public static bool IsEnabled()
        {
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var settings = settingService.LoadSetting<DiscountManagerPlusSettings>();
            return settings != null && settings.EnableLogging;
        }
    }
}
