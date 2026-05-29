using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberSphere.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CyberSphere.Infrastructure.Persistence;

namespace CyberSphere.Infrastructure.Services
{
    /// <summary>
    /// Seeds lookup data (Categories) on application startup.
    /// Idempotent — safe to call on every start.
    /// Invoked from Program.cs after app.Build().
    /// </summary>
    public static class DatabaseSeeder
    {
        private static readonly (string Name, string Slug, string Description)[] DefaultCategories =
        [
            ("Reconnaissance",    "recon",          "Passive and active information gathering tools"),
        ("Scanning",          "scanning",       "Network and vulnerability scanning tools"),
        ("Exploitation",      "exploitation",   "Penetration testing and exploit frameworks"),
        ("Post-Exploitation", "post-exploit",   "Tools used after initial access is established"),
        ("Password Attacks",  "passwords",      "Password cracking, spraying, and brute-force tools"),
        ("Wireless",          "wireless",       "Wi-Fi and Bluetooth security tools"),
        ("Forensics",         "forensics",      "Digital forensics and incident response tools"),
        ("Reverse Engineering","reverse-eng",   "Binary analysis, disassemblers, and debuggers"),
        ("Web Application",   "web-app",        "Web vulnerability scanners and proxies"),
        ("OSINT",             "osint",          "Open-source intelligence and data aggregation tools"),
        ("Cryptography",      "cryptography",   "Encryption, decryption, and key management tools"),
        ("Social Engineering","social-eng",     "Phishing frameworks and awareness testing tools"),
    ];

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                await db.Database.MigrateAsync();

                if (!await db.Categories.AnyAsync())
                {
                    var categories = DefaultCategories
                        .Select(c => Category.Create(c.Name, c.Slug, c.Description))
                        .ToList();

                    await db.Categories.AddRangeAsync(categories);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Seeded {Count} categories", categories.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database seeding failed");
                throw;
            }
        }
    }
}
