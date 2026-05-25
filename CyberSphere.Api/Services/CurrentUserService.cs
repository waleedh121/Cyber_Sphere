using CyberSphere.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace CyberSphere.Api.Services
{

    /// <summary>
    /// Reads the authenticated user's identity from the current request's JWT claims.
    /// Registered as Scoped in Program.cs.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            => _httpContextAccessor = httpContextAccessor;

        private ClaimsPrincipal? Principal =>
            _httpContextAccessor.HttpContext?.User;

        public Guid UserId
        {
            get
            {
                var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? Principal?.FindFirstValue("sub");

                return Guid.TryParse(value, out var id)
                    ? id
                    : throw new InvalidOperationException("UserId claim is missing or malformed.");
            }
        }

        public string UserName =>
            Principal?.FindFirstValue(ClaimTypes.Name)
            ?? Principal?.FindFirstValue("unique_name")
            ?? string.Empty;

        public string Role =>
            Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        public bool IsAuthenticated =>
            Principal?.Identity?.IsAuthenticated is true;
    }
}
