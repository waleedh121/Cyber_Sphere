using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Interfaces.Services
{
    /// <summary>
    /// Abstracts password hashing so the Domain stays independent of BCrypt.
    /// Implemented in Infrastructure/Identity using BCrypt.Net-Next.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Returns a BCrypt hash of the plain-text password.</summary>
        string Hash(string plainTextPassword);

        /// <summary>Verifies a plain-text password against a stored hash.</summary>
        bool Verify(string plainTextPassword, string hash);
    }
}
