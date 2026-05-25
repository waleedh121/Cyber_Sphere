using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Enums
{
    public enum ToolStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum DifficultyLevel
    {
        Beginner = 0,
        Intermediate = 1,
        Advanced = 2,
        Expert = 3
    }

    public enum ReviewDecision
    {
        Approved = 0,
        Rejected = 1
    }
}
