using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Interfaces.Services
{
    /// <summary>
    /// Sends transactional emails (password reset, notifications).
    /// Implemented in Infrastructure/Services using MailKit.
    /// </summary>
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, CancellationToken ct = default);
        Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken ct = default);

        Task SendToolApprovedEmailAsync(string toEmail, string userName, string toolName, CancellationToken ct = default);
        Task SendToolRejectedEmailAsync(string toEmail, string userName, string toolName, string? adminNotes, CancellationToken ct = default);
        Task SendToolResubmittedEmailAsync(string toEmail, string adminEmail, string toolName, CancellationToken ct = default);
    }

}
