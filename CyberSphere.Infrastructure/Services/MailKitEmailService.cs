using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace CyberSphere.Infrastructure.Services
{
    /// <summary>
    /// Sends transactional emails via MailKit + SMTP.
    /// Configure SMTP credentials in appsettings.json → "EmailSettings".
    /// For development, point at MailHog (localhost:1025) with no auth.
    /// </summary>
    public sealed class MailKitEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<MailKitEmailService> _logger;

        public MailKitEmailService(IConfiguration configuration, ILogger<MailKitEmailService> logger)
        {
            _settings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>()
                ?? throw new InvalidOperationException("EmailSettings section is missing from configuration.");
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(
            string toEmail, string userName, string resetToken, CancellationToken ct = default)
        {
            var resetUrl = $"{_settings.AppBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

            var body = $"""
            <h2>Reset your CyberSphere password</h2>
            <p>Hi {userName},</p>
            <p>We received a request to reset your password. Click the link below to set a new password.
            This link expires in <strong>15 minutes</strong>.</p>
            <p><a href="{resetUrl}" style="padding:10px 20px;background:#1D9E75;color:white;text-decoration:none;border-radius:6px;">
              Reset password
            </a></p>
            <p>If you did not request this, you can safely ignore this email.</p>
            <hr/>
            <small>CyperSphere — Community Cybersecurity Platform</small>
            """;

            await SendAsync(toEmail, "Reset your CyperSphere password", body, ct);
        }

        public async Task SendWelcomeEmailAsync(
            string toEmail, string userName, CancellationToken ct = default)
        {
            var body = $"""
            <h2>Welcome to CyberSphere, {userName}!</h2>
            <p>Your account is ready. Start exploring the community marketplace,
            contribute tools, and build your cybersecurity profile.</p>
            <p><a href="{_settings.AppBaseUrl}" style="padding:10px 20px;background:#1D9E75;color:white;text-decoration:none;border-radius:6px;">
              Go to CyperSphere
            </a></p>
            <hr/>
            <small>CyperSphere — Community Cybersecurity Platform</small>
            """;

            await SendAsync(toEmail, $"Welcome to CyperSphere, {userName}!", body, ct);
        }

        public async Task SendToolApprovedEmailAsync(
            string toEmail, string userName, string toolName, CancellationToken ct = default)
        {
            var marketplaceUrl = $"{_settings.AppBaseUrl}/marketplace";

            var body = $"""
            <h2>Your tool has been approved! 🎉</h2>
            <p>Hi {userName},</p>
            <p>Your tool <strong>{toolName}</strong> has been reviewed and <strong style="color:#1D9E75;">approved</strong>
            by our admin team. It is now live in the CyperSphere Marketplace.</p>
            <p>Your community profile has been updated to reflect this contribution.</p>
            <p><a href="{marketplaceUrl}" style="padding:10px 20px;background:#1D9E75;color:white;text-decoration:none;border-radius:6px;">
              View in Marketplace
            </a></p>
            <hr/>
            <small>CyperSphere — Community Cybersecurity Platform</small>
            """;

            await SendAsync(toEmail, $"✅ Tool approved: {toolName}", body, ct);
        }

        public async Task SendToolRejectedEmailAsync(
            string toEmail, string userName, string toolName, string? adminNotes, CancellationToken ct = default)
        {
            var notesHtml = !string.IsNullOrWhiteSpace(adminNotes)
                ? $"<p><strong>Feedback from our team:</strong></p><blockquote style=\"border-left:3px solid #E53E3E;padding-left:12px;color:#555;\">{adminNotes}</blockquote>"
                : string.Empty;

            var body = $"""
            <h2>Tool review update</h2>
            <p>Hi {userName},</p>
            <p>Unfortunately, your tool <strong>{toolName}</strong> was <strong style="color:#E53E3E;">not approved</strong>
            at this time.</p>
            {notesHtml}
            <p>You can update your tool based on the feedback and resubmit it for review.
            Editing a rejected tool will automatically requeue it for admin review.</p>
            <hr/>
            <small>CyperSphere — Community Cybersecurity Platform</small>
            """;

            await SendAsync(toEmail, $"❌ Tool review: {toolName}", body, ct);
        }

        public async Task SendToolResubmittedEmailAsync(
            string toEmail, string adminEmail, string toolName, CancellationToken ct = default)
        {
            var reviewUrl = $"{_settings.AppBaseUrl}/admin/review";

            var body = $"""
            <h2>Tool resubmitted for review</h2>
            <p>The tool <strong>{toolName}</strong> has been updated and resubmitted for review.</p>
            <p><a href="{reviewUrl}" style="padding:10px 20px;background:#3B8BD4;color:white;text-decoration:none;border-radius:6px;">
              Go to Review Queue
            </a></p>
            <hr/>
            <small>CyperSphere — Community Cybersecurity Platform</small>
            """;

            await SendAsync(toEmail, $"🔄 Resubmitted for review: {toolName}", body, ct);
        }

        private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_settings.Host, _settings.Port,
                    _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable, ct);

                if (!string.IsNullOrEmpty(_settings.UserName))
                    await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, ct);

                await smtp.SendAsync(message, ct);
                await smtp.DisconnectAsync(quit: true, ct);
            }
            catch (Exception ex)
            {
                // Log but don't throw — email failure must never break the primary request flow.
                _logger.LogError(ex, "Failed to send email to {Email} with subject '{Subject}'", toEmail, subject);
            }
        }
    }

    public sealed class EmailSettings
    {
        public const string SectionName = "EmailSettings";
        public string Host { get; init; } = "localhost";
        public int Port { get; init; } = 1025;
        public bool UseSsl { get; init; } = false;
        public string UserName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string SenderEmail { get; init; } = "noreply@cypersphere.dev";
        public string SenderName { get; init; } = "CyperSphere";
        public string AppBaseUrl { get; init; } = "https://localhost:5001";
    }
}
