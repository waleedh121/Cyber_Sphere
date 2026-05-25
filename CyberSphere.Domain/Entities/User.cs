using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Enums;
using System.Security.Claims;

namespace CyberSphere.Domain.Entities
{ 

    public class User
    {
        // ── Identity ─────────────────────────────────────────────────────────────
        public Guid Id { get; private set; }
        public string UserName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public Role Role { get; private set; }

        // ── Community profile ─────────────────────────────────────────────────────
        public string? Bio { get; private set; }
        public string? ProfilePictureUrl { get; private set; }
        public int TotalToolsUploaded { get; private set; }
        public string? GitHubUrl { get; private set; }

        // ── Auth tokens ───────────────────────────────────────────────────────────
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiry { get; private set; }
        public string? PasswordResetToken { get; private set; }
        public DateTime? PasswordResetTokenExpiry { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public ICollection<Tool> Tools { get; private set; } = new List<Tool>();

        private User() { }

        // ── Factory ───────────────────────────────────────────────────────────────

        public static User Create(string userName, string email, string passwordHash, Role role = Role.User)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email.ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = role,
                TotalToolsUploaded = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ── Profile ───────────────────────────────────────────────────────────────

        public void UpdateProfile(string? bio, string? profilePictureUrl, string? gitHubUrl)
        {
            Bio = bio;
            ProfilePictureUrl = profilePictureUrl;
            GitHubUrl = gitHubUrl;
        }

        // ── Refresh token ─────────────────────────────────────────────────────────

        public void SetRefreshToken(string token, DateTime expiry)
        {
            RefreshToken = token;
            RefreshTokenExpiry = expiry;
        }

        public void RevokeRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiry = null;
        }

        public bool IsRefreshTokenValid(string token) =>
            RefreshToken == token && RefreshTokenExpiry > DateTime.UtcNow;

        // ── Password reset ────────────────────────────────────────────────────────

        public void SetPasswordResetToken(string token, DateTime expiry)
        {
            PasswordResetToken = token;
            PasswordResetTokenExpiry = expiry;
        }

        public void ClearPasswordResetToken()
        {
            PasswordResetToken = null;
            PasswordResetTokenExpiry = null;
        }

        public void UpdatePasswordHash(string newPasswordHash) =>
            PasswordHash = newPasswordHash;

        public bool IsPasswordResetTokenValid(string token) =>
            PasswordResetToken == token && PasswordResetTokenExpiry > DateTime.UtcNow;

        // ── Tools ─────────────────────────────────────────────────────────────────

        public void IncrementToolsUploaded() => TotalToolsUploaded++;
        public void DecrementToolsUploaded() => TotalToolsUploaded = Math.Max(0, TotalToolsUploaded - 1);
    }
}
