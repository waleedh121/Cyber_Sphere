using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Entities
{
    public sealed class ToolCommand
    {
        public Guid Id { get; private set; }
        public Guid ToolId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Syntax { get; private set; } = string.Empty;
        public string? Example { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public Tool Tool { get; private set; } = null!;

        private ToolCommand() { }

        public static ToolCommand Create(Guid toolId, string name, string description, string syntax, string? example = null)
        {
            return new ToolCommand
            {
                Id = Guid.NewGuid(),
                ToolId = toolId,
                Name = name.Trim(),
                Description = description.Trim(),
                Syntax = syntax.Trim(),
                Example = example?.Trim()
            };
        }

        public void Update(string name, string description, string syntax, string? example)
        {
            Name = name.Trim();
            Description = description.Trim();
            Syntax = syntax.Trim();
            Example = example?.Trim();
        }
    }
}
