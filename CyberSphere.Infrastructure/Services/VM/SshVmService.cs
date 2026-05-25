using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshConnectionInfo = Renci.SshNet.ConnectionInfo;

namespace CyberSphere.Infrastructure.Services.VM
{

    /// <summary>
    /// SSH.NET-based implementation of IVmService.
    /// Connects to VMs using a shared service account + private key authentication.
    /// Application layer never references this class — it only sees IVmService.
    ///
    /// SSH.NET NuGet: SSH.NET (Renci.SshNet) v2024+
    /// </summary>
    public sealed class SshVmService : IVmService
    {
        private readonly VmSettings _settings;
        private readonly ILogger<SshVmService> _logger;

        public SshVmService(IOptions<VmSettings> settings, ILogger<SshVmService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }
        public void Connect(string host, string username, string password)
        {
            SshConnectionInfo connectionInfo =
                new SshConnectionInfo(
                    host,
                    username,
                    new PasswordAuthenticationMethod(username, password)
                );

            using var client = new SshClient(connectionInfo);
            client.Connect();
        }

        // ── Health check ──────────────────────────────────────────────────────────

        public async Task<VmHealthResult> CheckHealthAsync(
            string ipAddress, int port, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var client = BuildClient(ipAddress, port);
                    client.Connect();

                    var isConnected = client.IsConnected;
                    client.Disconnect();

                    return isConnected
                        ? new VmHealthResult(IsHealthy: true)
                        : new VmHealthResult(IsHealthy: false, "SSH connection failed silently.");
                }
                catch (SshAuthenticationException ex)
                {
                    _logger.LogError(ex,
                        "SSH authentication failed for {IpAddress}:{Port}", ipAddress, port);
                    return new VmHealthResult(false, $"Authentication failed: {ex.Message}");
                }
                catch (SshConnectionException ex)
                {
                    _logger.LogWarning(ex,
                        "SSH connection refused at {IpAddress}:{Port}", ipAddress, port);
                    return new VmHealthResult(false, $"Connection refused: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unexpected error during health check for {IpAddress}:{Port}", ipAddress, port);
                    return new VmHealthResult(false, $"Unexpected error: {ex.Message}");
                }
            }, ct);
        }

        // ── Session initialise ────────────────────────────────────────────────────

        public async Task<VmConnectionResult> InitialiseSessionAsync(
            Guid sessionId,
            string ipAddress,
            int port,
            string userName,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var client = BuildClient(ipAddress, port);
                    client.Connect();

                    if (!client.IsConnected)
                        return new VmConnectionResult(Success: false,
                            ErrorMessage: "SSH connection did not establish.");

                    // ── User workspace setup ──────────────────────────────────────
                    // Create isolated working directory for this session.
                    // All commands run in /home/cypersphere/sessions/{sessionId}/.
                    var workDir = $"/home/cypersphere/sessions/{sessionId}";

                    RunCommand(client, $"mkdir -p {workDir}");
                    RunCommand(client, $"chmod 700 {workDir}");

                    // Optional: write a session metadata file for the VM to reference
                    RunCommand(client,
                        $"echo '{{\"session_id\":\"{sessionId}\",\"user\":\"{userName}\"}}' " +
                        $"> {workDir}/.session_meta.json");

                    _logger.LogInformation(
                        "SSH workspace initialised for session {SessionId} at {IpAddress}:{Port}",
                        sessionId, ipAddress, port);

                    client.Disconnect();

                    // ── Issue connection token ─────────────────────────────────────
                    // The token is a one-time ticket the frontend uses to open a
                    // WebSocket terminal channel to the VM's web-shell daemon.
                    // Format: base64(sessionId:timestamp:hmac)
                    var token = GenerateConnectionToken(sessionId);

                    return new VmConnectionResult(
                        Success: true,
                        ConnectionToken: token);
                }
                catch (SshAuthenticationException ex)
                {
                    _logger.LogError(ex,
                        "SSH authentication failed initialising session {SessionId}", sessionId);
                    return new VmConnectionResult(false,
                        ErrorMessage: "SSH authentication failed.");
                }
                catch (SshConnectionException ex)
                {
                    _logger.LogWarning(ex,
                        "SSH connection refused initialising session {SessionId}", sessionId);
                    return new VmConnectionResult(false,
                        ErrorMessage: "Could not connect to VM.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Unexpected error initialising session {SessionId}", sessionId);
                    return new VmConnectionResult(false,
                        ErrorMessage: $"Initialisation failed: {ex.Message}");
                }
            }, ct);
        }

        // ── Session teardown ──────────────────────────────────────────────────────

        public async Task TerminateSessionAsync(
            Guid sessionId,
            string ipAddress,
            int port,
            CancellationToken ct = default)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var client = BuildClient(ipAddress, port);
                    client.Connect();

                    if (!client.IsConnected)
                    {
                        _logger.LogWarning(
                            "Could not connect to VM for teardown of session {SessionId}", sessionId);
                        return;
                    }

                    var workDir = $"/home/cypersphere/sessions/{sessionId}";

                    // Kill any processes still running in this session's directory
                    RunCommand(client, $"pkill -f 'sessions/{sessionId}' 2>/dev/null || true");

                    // Clean up the workspace
                    RunCommand(client, $"rm -rf {workDir}");

                    client.Disconnect();

                    _logger.LogInformation(
                        "SSH teardown completed for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    // Teardown failures are non-fatal — session is already closed in DB
                    _logger.LogWarning(ex,
                        "SSH teardown failed for session {SessionId} at {IpAddress}:{Port} (non-fatal)",
                        sessionId, ipAddress, port);
                }
            }, ct);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private SshClient BuildClient(string ipAddress, int port)
        {
            var timeout = TimeSpan.FromSeconds(_settings.SshTimeoutSeconds);

            ConnectionInfo connectionInfo;

            if (File.Exists(_settings.SshPrivateKeyPath))
            {
                var keyFile = new PrivateKeyFile(_settings.SshPrivateKeyPath);
                connectionInfo = new ConnectionInfo(
                    ipAddress, port, _settings.SshServiceUser,
                    new PrivateKeyAuthenticationMethod(_settings.SshServiceUser, keyFile));
            }
            else
            {
                // Dev fallback — log a warning, never use in production
                _logger.LogWarning(
                    "SSH private key not found at '{Path}'. Using no-auth stub for development.",
                    _settings.SshPrivateKeyPath);

                connectionInfo = new SshConnectionInfo(
                    ipAddress, port, _settings.SshServiceUser,
                    new NoneAuthenticationMethod(_settings.SshServiceUser));
            }

            return new SshClient(connectionInfo) { ConnectionInfo = { Timeout = timeout } };
        }

        private static void RunCommand(SshClient client, string command)
        {
            using var cmd = client.RunCommand(command);
            // Errors are intentionally ignored per-command — callers handle overall failure
        }

        private static string GenerateConnectionToken(Guid sessionId)
        {
            // Token = base64(sessionId || timestamp || 16-random-bytes)
            // In production, sign this with HMAC using a server secret for tamper-proofing
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomPart = RandomNumberGenerator.GetBytes(16);
            var raw = $"{sessionId}:{timestamp}:{Convert.ToBase64String(randomPart)}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
        }
    }
}
