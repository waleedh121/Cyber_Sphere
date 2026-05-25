using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Infrastructure.Identity
{
    /// <summary>
    /// Bound from appsettings.json → "JwtSettings".
    /// Injected as IOptions&lt;JwtSettings&gt; in JwtTokenService.
    /// </summary>
    public sealed class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        /// <summary>Secret key — minimum 32 characters. Store in user-secrets / environment variable in production.</summary>
        public string SecretKey { get; init; } = string.Empty;

        /// <summary>Token issuer (your API's base URL).</summary>
        public string Issuer { get; init; } = string.Empty;

        /// <summary>Token audience (your frontend's URL).</summary>
        public string Audience { get; init; } = string.Empty;

        /// <summary>Access token lifetime in minutes (default 15).</summary>
        public int AccessTokenExpiryMinutes { get; init; } = 15;

        /// <summary>Refresh token lifetime in days (default 7).</summary>
        public int RefreshTokenExpiryDays { get; init; } = 7;
    }
}
