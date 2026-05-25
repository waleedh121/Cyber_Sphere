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
    public sealed class ToolReviewConfiguration : IEntityTypeConfiguration<ToolReview>
    {
        public void Configure(EntityTypeBuilder<ToolReview> builder)
        {
            builder.ToTable("ToolReviews");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Decision)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(r => r.Notes)
                .HasMaxLength(1000);

            builder.Property(r => r.ReviewedAt)
                .IsRequired();

            // ── Indexes ───────────────────────────────────────────────────────────
            // GetByToolIdAsync and GetLatestByToolIdAsync — most common access pattern
            builder.HasIndex(r => new { r.ToolId, r.ReviewedAt });

            // ── Relationships ─────────────────────────────────────────────────────
            builder.HasOne(r => r.Tool)
                .WithMany()
                .HasForeignKey(r => r.ToolId)
                .OnDelete(DeleteBehavior.Cascade);    // reviews deleted with their tool

            builder.HasOne(r => r.Admin)
                .WithMany()
                .HasForeignKey(r => r.AdminId)
                .OnDelete(DeleteBehavior.Restrict);   // keep review history if admin deleted
        }
    }
}
