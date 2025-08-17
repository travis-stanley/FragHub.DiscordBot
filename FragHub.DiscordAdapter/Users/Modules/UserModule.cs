using Discord;
using Discord.Interactions;
using FragHub.Application;
using FragHub.Application.Abstractions;
using FragHub.Application.Users.Abstractions;
using FragHub.Application.Users.Commands;
using FragHub.Domain.Env;
using FragHub.Domain.Users.Entities;
using Microsoft.Extensions.Logging;

namespace FragHub.DiscordAdapter.Users.Modules;

[RequireContext(ContextType.Guild)]
[Group("users", "User related commands")]
public sealed class UserModule(ILogger<UserModule> _logger, CommandDispatcher _commandDispatcher) : InteractionModuleBase<SocketInteractionContext>
{

    #region Validation
        
    // throw validation methods here if needed

    #endregion


    #region Slash Commands

    /// <summary>
    /// Register a command to....
    /// </summary>
    /// <returns></returns>
    [SlashCommand("link", description: "Link your discord user to a game platform user", runMode: RunMode.Async)]
    public async Task Link(GamePlatformType platform, string platformUsername)
    {
        // acknowledge the command, or it will timeout after 3 seconds
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received link command: from {User} for linking {PlatformUser} on {Platform}", Context.User.Username, platformUsername, platform);

        // validations
        ArgumentNullException.ThrowIfNull(platformUsername, nameof(platformUsername));

        // craft command payload
        var cmd = GetCommand<LinkPlatformUserCommand>(Context);
        cmd.GamePlatformType = platform;
        cmd.PlatformUserName = platformUsername;

        // dispatch command
        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        // follow up with the user
        await FollowupAsync($"Account linked").ConfigureAwait(false);
    }

    /// <summary>
    /// Utility method to create a command instance with the necessary context information.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static T GetCommand<T>(SocketInteractionContext context) where T : Command, new()
    {
        return new T
        {
            GuildId = context.Guild.Id,
            UserId = context.User.Id
        };
    }
    #endregion



}
