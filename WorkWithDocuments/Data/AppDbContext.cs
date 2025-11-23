using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design; // Потрібно для IDesignTimeDbContextFactory
using Microsoft.Extensions.Configuration;
using System.IO;
using WorkWithDocuments.Models;

namespace WorkWithDocuments.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ProcessedFile> ProcessedFiles { get; set; }
    }

    // --- ДОДАНО ЦЕЙ КЛАС ДЛЯ ВИПРАВЛЕННЯ ПОМИЛКИ MIGRATION ---
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Отримуємо конфігурацію з appsettings.json вручну
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseSqlServer(connectionString);

            return new AppDbContext(builder.Options);
        }
    }
}
