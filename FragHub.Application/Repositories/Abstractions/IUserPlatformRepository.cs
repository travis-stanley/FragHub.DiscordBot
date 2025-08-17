using FragHub.Domain.Users.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Application.Repositories.Abstractions
{
    public interface IUserPlatformRepository
    {
        Task<PlatformUser[]> GetPlatformUsersAsync(ulong discordUserId);
        Task<PlatformUser[]> GetPlatformUsersAsync();

        Task<PlatformUser> AddPlatformUserAsync(NewPlatformUser platformUser);
    }
}
