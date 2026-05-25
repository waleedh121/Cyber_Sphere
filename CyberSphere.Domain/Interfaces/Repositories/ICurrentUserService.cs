using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Interfaces.Repositories;

    /// <summary>
    /// Extracts the authenticated user's identity from the current HTTP context.
    /// Implemented in WebAPI — injected into Application handlers via DI.
    /// </summary>
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string UserName { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
    }

