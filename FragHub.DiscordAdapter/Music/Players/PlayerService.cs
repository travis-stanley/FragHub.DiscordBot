using Lavalink4NET;
using Lavalink4NET.Players;
using Microsoft.Extensions.Options;

using FragHub.Application.Music.Abstractions;

namespace FragHub.Discord.Music.Players;

public class PlayerService(IAudioService _audioService) : IPlayerService
{
    /// <summary>
    ///     Get a music player for the specified options.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <remarks>
    ///     Music modules are responsible for verifying user in voice, etc before using this method to retrieve a player instance.
    /// </remarks>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<IMusicPlayer?> GetPlayerAsync(ulong guildId, ulong voiceChannelId, ulong userId, bool connectToVoiceChannel = true)
    {
        var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

        var customPlayerOptions = new CustomPlayerOptions();
        //var playerOptions = new CustomPlayerOptions(controllerChannel); // todo -- is channel context needed here?

        var result = await _audioService.Players.RetrieveAsync<CustomPlayer, CustomPlayerOptions>(guildId, voiceChannelId, playerFactory: CreatePlayer, options: Options.Create(customPlayerOptions), retrieveOptions: retrieveOptions).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to retrieve player: {result.Status}");

        // Set the player options such as volume, etc.
        await result.Player.SetVolumeAsync(0.5f).ConfigureAwait(false);

        return result.Player;
    }

    static ValueTask<CustomPlayer> CreatePlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new CustomPlayer(properties));
    }
}
