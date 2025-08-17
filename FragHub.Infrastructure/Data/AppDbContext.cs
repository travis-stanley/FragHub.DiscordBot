using FragHub.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<GamePlatform> GamePlatforms { get; set; }
        public DbSet<PlatformUser> PlatformUsers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships, constraints, table names, etc.
            modelBuilder.Entity<GamePlatform>().ToTable("GamePlatforms");
            modelBuilder.Entity<PlatformUser>().ToTable("PlatformUsers");

            modelBuilder.Entity<GamePlatform>().HasIndex(p => p.Type).IsUnique();
        }
    }

    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            DotNetEnv.Env.Load();
            DotNetEnv.Env.TraversePath().Load();

            var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING") ?? throw new InvalidOperationException("SQL_CONNECTION_STRING environment variable is not set.");
            optionsBuilder.UseSqlServer(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
