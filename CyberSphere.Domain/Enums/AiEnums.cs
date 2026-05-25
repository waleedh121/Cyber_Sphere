using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Enums
{
    public enum AiMessageRole
    {
        User = 0,
        Assistant = 1,
        System = 2
    }

    public enum AiSessionStatus
    {
        Active = 0,
        Ended = 1
    }
}
