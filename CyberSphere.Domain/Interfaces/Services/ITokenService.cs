using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Domain.Interfaces.Services
{

    /// <summary>
    /// Generates and validates JWT access tokens and opaque refresh tokens.
    /// Implemented in Infrastructure/Identity — Domain has no knowledge of JWT libraries.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>Generates a signed JWT access token for the given user.</summary>
        string GenerateAccessToken(User user);

        /// <summary>Generates a cryptographically random refresh token string.</summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Returns the expiry duration for refresh tokens.
        /// Used by the command handler to set User.RefreshTokenExpiresAt.
        /// </summary>
        DateTime RefreshTokenExpiresAt();
    }
}
