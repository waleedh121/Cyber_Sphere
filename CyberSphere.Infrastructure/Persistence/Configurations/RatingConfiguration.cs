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

    public sealed class RatingConfiguration : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.ToTable("Ratings");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Stars)
                .IsRequired();

            builder.Property(r => r.Comment)
                .HasMaxLength(1000);

            builder.Property(r => r.CreatedAt).IsRequired();
            builder.Property(r => r.UpdatedAt).IsRequired();

            // ── Unique constraint ─────────────────────────────────────────────────
            // One rating per user per tool — enforced at DB level in addition to
            // the ExistsAsync guard in RateToolCommandHandler
            builder.HasIndex(r => new { r.UserId, r.ToolId }).IsUnique();

            // ── Index ─────────────────────────────────────────────────────────────
            // GetByToolIdAsync — paged ratings for a tool detail page
            builder.HasIndex(r => new { r.ToolId, r.CreatedAt });

            // ── Relationships ─────────────────────────────────────────────────────
            builder.HasOne(r => r.Tool)
                .WithMany()
                .HasForeignKey(r => r.ToolId)
                .OnDelete(DeleteBehavior.Cascade);      // ratings deleted with their tool

            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);     // keep ratings if user deleted
        }
    }

}
