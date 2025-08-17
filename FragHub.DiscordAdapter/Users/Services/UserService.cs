using FragHub.Application.Repositories.Abstractions;
using FragHub.Application.Users.Abstractions;
using FragHub.Application.Users.Commands;
using FragHub.Domain.Music.Entities;
using FragHub.Domain.Users.Entities;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;

namespace FragHub.DiscordAdapter.Users.Services;

public class UserService(IUserPlatformRepository _userPlatformRepo) : IUserService
{

    /// <summary>
    /// Register a Discord user with a game platform user.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task LinkPlatformUserAsync(LinkPlatformUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        ArgumentNullException.ThrowIfNull(command.PlatformUserName, nameof(command.PlatformUserName));
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId)); }

        var newPlatformUser = new NewPlatformUser
        {
            GamePlatformType = command.GamePlatformType,
            PlatformUserName = command.PlatformUserName,
            DiscordUserId = command.UserId.Value
        };
        await _userPlatformRepo.AddPlatformUserAsync(newPlatformUser);
    }
}
