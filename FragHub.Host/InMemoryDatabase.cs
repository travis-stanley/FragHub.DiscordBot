using FragHub.Domain.Users.Entities;
using FragHub.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Host
{
    internal class InMemoryDatabase
    {
        public static void Seed(IHost app)
        {
            // Initialize the in-memory database with some default data
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.GamePlatforms.Add(new GamePlatform { Name = "Electronic Arts", Type = GamePlatformType.ElectronicArts, IsEnabled = true });
            context.SaveChanges();
        }
    }
}
