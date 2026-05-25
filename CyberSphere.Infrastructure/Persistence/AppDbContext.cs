using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CyberSphere.Infrastructure.Persistence
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Tool> Tools => Set<Tool>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ToolCommand> ToolCommands => Set<ToolCommand>();
        public DbSet<ToolReview> ToolReviews => Set<ToolReview>();
        public DbSet<Rating> Ratings => Set<Rating>();

        // ── VM Sessions ───────────────────────────────────────────────────────────
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<Server> Servers => Set<Server>();

        // ── AI Session ────────────────────────────────────────────────────────────
        public DbSet<AiSession> AiSessions => Set<AiSession>();
        public DbSet<AiMessage> AiMessages => Set<AiMessage>();
        public DbSet<UserAiStats> UserAiStats => Set<UserAiStats>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all IEntityTypeConfiguration<T> classes in this assembly automatically
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
