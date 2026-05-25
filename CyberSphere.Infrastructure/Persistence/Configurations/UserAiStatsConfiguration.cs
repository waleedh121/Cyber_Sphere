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
    public sealed class UserAiStatsConfiguration : IEntityTypeConfiguration<UserAiStats>
    {
        public void Configure(EntityTypeBuilder<UserAiStats> builder)
        {
            builder.ToTable("UserAiStats");

            // UserId is both PK and FK — one row per user, no surrogate key needed
            builder.HasKey(s => s.UserId);

            builder.Property(s => s.TotalRequests).HasDefaultValue(0).IsRequired();
            builder.Property(s => s.TotalTokensUsed).HasDefaultValue(0).IsRequired();
            builder.Property(s => s.TotalSessions).HasDefaultValue(0).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();

            // ── Relationship ──────────────────────────────────────────────────────
            builder.HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<UserAiStats>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
