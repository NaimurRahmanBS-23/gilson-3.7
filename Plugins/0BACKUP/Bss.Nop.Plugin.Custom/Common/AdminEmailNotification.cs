using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Messages;
using Nop.Core.Infrastructure;
using Nop.Services.Messages;

namespace Bss.Nop.Plugin.Custom.Common
{
    public static class AdminEmailNotification
    {
        public static void SendEmail(string subject, string body)
        {
            var emailAccountService = EngineContext.Current.Resolve<IEmailAccountService>();
            var emailAccountSettings = EngineContext.Current.Resolve<EmailAccountSettings>();

            // Get default email account
            var emailAccount = emailAccountService.GetEmailAccountById(emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount == null)
                emailAccount = emailAccountService.GetAllEmailAccounts().FirstOrDefault();
            if (emailAccount == null)
                throw new Exception("No email account could be loaded");

            var queuedEmailService = EngineContext.Current.Resolve<IQueuedEmailService>();
            queuedEmailService.InsertQueuedEmail(new QueuedEmail
            {
                From = emailAccount.Email,
                FromName = emailAccount.DisplayName,
                To = emailAccount.Email,
                ToName = emailAccount.DisplayName,
                Priority = QueuedEmailPriority.High,
                Subject = subject,
                Body = body,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = emailAccount.Id
            });
        }
    }
}
