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
    public sealed class AiSessionConfiguration : IEntityTypeConfiguration<AiSession>
    {
        public void Configure(EntityTypeBuilder<AiSession> builder)
        {
            builder.ToTable("AiSessions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(s => s.StartedAt).IsRequired();

            // ── Indexes ───────────────────────────────────────────────────────────
            // Primary access pattern: GetByUserIdAsync — all sessions for a user
            builder.HasIndex(s => new { s.UserId, s.StartedAt });

            // ── Relationships ─────────────────────────────────────────────────────
            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);      // sessions deleted with user

            builder.HasMany(s => s.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);      // messages deleted with session
        }
    }
}
