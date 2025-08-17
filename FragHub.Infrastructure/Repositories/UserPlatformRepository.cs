using FragHub.Application.Repositories.Abstractions;
using FragHub.Domain.Users.Entities;
using FragHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Infrastructure.Repositories
{
    public class UserPlatformRepository(AppDbContext _context) : IUserPlatformRepository
    {
        public async Task<PlatformUser> AddPlatformUserAsync(NewPlatformUser platformUser)
        {
            var newUser = await _context.PlatformUsers.AddAsync(new PlatformUser
            {
                DiscordUserId = platformUser.DiscordUserId,
                GamePlatformType = platformUser.GamePlatformType,
                PlatformUserName = platformUser.PlatformUserName
            });
            await _context.SaveChangesAsync().ConfigureAwait(false);
            
            return newUser.Entity;
        }

        public async Task<PlatformUser[]> GetPlatformUsersAsync(ulong discordUserId)
        {
            return await _context.PlatformUsers.Where(p => p.DiscordUserId == discordUserId).ToArrayAsync().ConfigureAwait(false);
        }

        public async Task<PlatformUser[]> GetPlatformUsersAsync()
        {
            return await _context.PlatformUsers.ToArrayAsync().ConfigureAwait(false);
        }
    }
}
