using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberSphere.Domain.Entities
{
    public sealed class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public string? Description { get; private set; }

        // ── Navigation ────────────────────────────────────────────────────────────
        public ICollection<Tool> Tools { get; private set; } = new List<Tool>();

        private Category() { }

        public static Category Create(string name, string slug, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
            if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Category slug is required.", nameof(slug));

            return new Category
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Slug = slug.ToLowerInvariant().Trim(),
                Description = description?.Trim()
            };
        }

        public void Update(string name, string slug, string? description)
        {
            Name = name.Trim();
            Slug = slug.ToLowerInvariant().Trim();
            Description = description?.Trim();
        }
    }
}
