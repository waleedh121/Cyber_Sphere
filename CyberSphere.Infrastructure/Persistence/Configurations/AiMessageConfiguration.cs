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

    public sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
    {
        public void Configure(EntityTypeBuilder<AiMessage> builder)
        {
            builder.ToTable("AiMessages");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(8000);     // max message + response content

            builder.Property(m => m.ErrorMessage)
                .HasMaxLength(500);

            builder.Property(m => m.CreatedAt).IsRequired();

            // ── Indexes ───────────────────────────────────────────────────────────
            // GetByIdWithMessagesAsync — chronological message load per session
            builder.HasIndex(m => new { m.SessionId, m.CreatedAt });


            // ── Relations ─────────────────────────────────────────────────────────
            builder.HasOne(m => m.Session)
                .WithMany(s => s.Messages)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade); 
        }
    }

}
