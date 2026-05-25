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
    public sealed class ToolConfiguration : IEntityTypeConfiguration<Tool>
    {
        public void Configure(EntityTypeBuilder<Tool> builder)
        {
            builder.ToTable("Tools");
            builder.HasKey(t => t.Id);

            // ── Content ───────────────────────────────────────────────────────────
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(t => t.GitHubUrl)
                .HasMaxLength(500);

            builder.Property(t => t.Tags)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            // ── Enums stored as strings ───────────────────────────────────────────
            builder.Property(t => t.DifficultyLevel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // ── Stats ─────────────────────────────────────────────────────────────
            builder.Property(t => t.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(t => t.AverageRating)
                .HasPrecision(4, 2)
                .HasDefaultValue(0m);

            builder.Property(t => t.RatingCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            // ── Indexes ───────────────────────────────────────────────────────────
            // Unique name prevents duplicate tool names across the platform
            builder.HasIndex(t => t.Name).IsUnique();

            // Composite index supports the most common marketplace filter pattern
            builder.HasIndex(t => new { t.Status, t.CategoryId });

            // Supports owner profile page query (GetApprovedByOwnerAsync)
            builder.HasIndex(t => new { t.OwnerId, t.Status });

            // ── Relationships ─────────────────────────────────────────────────────
            builder.HasOne(t => t.Owner)
                .WithMany(u => u.Tools)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);     // keep tools if user deleted

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Tools)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);     // cannot delete a category with tools

            builder.HasMany(t => t.Commands)
                .WithOne(c => c.Tool)
                .HasForeignKey(c => c.ToolId)
                .OnDelete(DeleteBehavior.Cascade);      // commands die with their tool
        }
    }
}
