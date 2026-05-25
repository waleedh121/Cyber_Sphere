using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using CyberSphere.Domain.Entities;

namespace CyberSphere.Infrastructure.Persistence.Configurations
{
    public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.ToTable("Sessions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(s => s.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(s => s.ConnectionToken)
                .HasMaxLength(500);

            builder.Property(s => s.VmIpAddress)
                .HasMaxLength(45);         // IPv6 max length

            builder.Property(s => s.TerminationReason)
                .HasMaxLength(500);

            builder.Property(s => s.StartedAt).IsRequired();

            // ── Indexes ───────────────────────────────────────────────────────────

            // HasActiveSessionOfTypeAsync + GetActiveByUserIdAsync
            builder.HasIndex(s => new { s.UserId, s.Status, s.Type });

            // GetActiveByServerIdAsync — used when taking a server offline
            builder.HasIndex(s => new { s.ServerId, s.Status });

            // Admin paged history (most recent first)
            builder.HasIndex(s => s.StartedAt);

            // ── Relationships ─────────────────────────────────────────────────────
            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);    // keep session history if user deleted

            builder.HasOne(s => s.Server)
                .WithMany(srv => srv.Sessions)
                .HasForeignKey(s => s.ServerId)
                .OnDelete(DeleteBehavior.Restrict);    // keep session history if server deleted
        }
    }
}
