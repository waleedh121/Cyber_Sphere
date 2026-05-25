using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CyberSphere.Infrastructure.Persistence.Configurations
{
    public sealed class ToolCommandConfiguration : IEntityTypeConfiguration<ToolCommand>
    {
        public void Configure(EntityTypeBuilder<ToolCommand> builder)
        {
            builder.ToTable("ToolCommands");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(c => c.Syntax)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(c => c.Example)
                .HasMaxLength(1000);

            // ── Index ─────────────────────────────────────────────────────────────
            // Supports bulk load of all commands for a given tool
            builder.HasIndex(c => c.ToolId);
        }
    }
}
