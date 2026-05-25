using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;

namespace Bss.Nop.Plugin.Custom.Attributes
{
    // Capture all controller exceptions for methods decorated with [JsonErrorHandler]
    // and log and return the appropriate error response.
    public class JsonErrorHandlerAttribute : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.RequestContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.HttpContext.Response.StatusCode = 500;    // 500 required to invoke jQuery ajax error function
                filterContext.ExceptionHandled = true;
                filterContext.Result = new JsonResult
                {
                    Data = new
                    {
                        success = false,
                        errorMessage = filterContext.Exception.Message
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };

                // Log to nop error log
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.Error("JsonErrorHandler: " + filterContext.Exception.Message, filterContext.Exception);
            }
            else
            {
                throw filterContext.Exception;
            }
        }
    }
}
