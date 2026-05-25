using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Infrastructure.Services
{
    /// <summary>
    /// Bound from appsettings.json → "VmSettings".
    /// Stores the SSH service account credentials used to connect to VMs.
    /// In production these should come from a secrets vault, not appsettings.
    /// </summary>
    public sealed class VmSettings
    {
        public const string SectionName = "VmSettings";

        /// <summary>
        /// SSH username used to connect to VMs.
        /// This is the service account on the VM, not the CyperSphere user.
        /// </summary>
        public string SshServiceUser { get; init; } = "cypersphere";

        /// <summary>
        /// Path to the private key file used for SSH authentication.
        /// Recommended: RSA 4096-bit or Ed25519 key.
        /// </summary>
        public string SshPrivateKeyPath { get; init; } = "/etc/cypersphere/vm_key";

        /// <summary>SSH connection timeout in seconds.</summary>
        public int SshTimeoutSeconds { get; init; } = 15;

        /// <summary>
        /// If true, host key verification is skipped.
        /// NEVER set to true in production — used only for local dev VMs.
        /// </summary>
        public bool SkipHostKeyVerification { get; init; } = false;
    }
}
