using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CyberSphere.Infrastructure.Persistence.Configurations
{

    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);

            // Core fields
            builder.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>() // stored as "User" / "Admin" — readable in DB
                .HasMaxLength(20);

            // Community profile fields
            builder.Property(u => u.Bio)
                .HasMaxLength(500);

            builder.Property(u => u.ProfilePictureUrl)
                .HasMaxLength(1000);

            builder.Property(u => u.GitHubUrl)
                .HasMaxLength(500);

            builder.Property(u => u.TotalToolsUploaded)
                .IsRequired()
                .HasDefaultValue(0);

            // ── Auth tokens ─────────────────────────────────────────────────────────────
            builder.Property(u => u.RefreshToken)
                .HasMaxLength(500);

            builder.Property(u => u.RefreshTokenExpiry);

            builder.Property(u => u.PasswordResetToken)
                .HasMaxLength(200);

            builder.Property(u => u.PasswordResetTokenExpiry);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            // ── Unique indexes ─────────────────────────────────────────────────────────
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => u.UserName).IsUnique();

            // ── Relationships ─────────────────────────────────────────────────────────────
            builder.HasMany(u => u.Tools)
                .WithOne(t => t.Owner)
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
