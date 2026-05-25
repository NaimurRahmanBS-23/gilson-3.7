using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;
using Nop.Services.Logging;

namespace Bss.Nop.Plugin.Custom.Extensions
{
    public static class LoggingExtensions
    {
        public static void Xml(this ILogger logger, string message, string xmlOutput)
        {
            logger.InsertLog(LogLevel.Debug, message, xmlOutput);
        }
    }

}
