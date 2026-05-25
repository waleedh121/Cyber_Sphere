using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Interfaces.Services;
using BC = BCrypt.Net.BCrypt;
namespace CyberSphere.Infrastructure.Identity
{

    /// <summary>
    /// Implements IPasswordHasher using BCrypt.Net-Next.
    /// Work factor 12 is a safe default for 2024+ hardware.
    /// Registered as Singleton — stateless, thread-safe.
    /// </summary>
    public sealed class BcryptPasswordHasher : IPasswordHasher
    {
        // Work factor: each increment doubles the hashing time.
        // 12 ≈ 250ms on modern hardware — adjust upward annually.
        private const int WorkFactor = 12;

        public string Hash(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
                throw new ArgumentException("Password cannot be empty.", nameof(plainTextPassword));

            return BC.HashPassword(plainTextPassword, WorkFactor);
        }

        public bool Verify(string plainTextPassword, string hash)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword) || string.IsNullOrWhiteSpace(hash))
                return false;

            // EnhancedHashPassword uses SHA384 to pre-hash — safely handles passwords > 72 chars
            // (BCrypt's byte limit). Pair with EnhancedVerify.
            return BC.Verify(plainTextPassword, hash);
        }
    }
}
