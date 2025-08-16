using FragHub.Application.Music.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Music.Entities;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;

namespace FragHub.DiscordAdapter.Music.Players;

public class PlayerService(IAudioService _audioService) : IPlayerService
{
    /// <summary>
    /// Gets the list of tracks sent to the player.
    /// </summary>
    public IEnumerable<Track> Tracks => _tracks;
    private readonly List<Track> _tracks = [];


    /// <summary>
    /// Handle a play command to play a track in the specified voice channel.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task PlayAsync(PlayTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }        

        var track = await GetTrackAsync(command.Query).ConfigureAwait(false) ?? throw new InvalidOperationException($"Track not found for query: {command.Query}");
        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");
        _tracks.Add(track);

        await player.PlayAsync(command, track).ConfigureAwait(false);
    }





    /// <summary>
    ///     Get a music player for the specified options.
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <remarks>
    ///     Music modules are responsible for verifying user in voice, etc before using this method to retrieve a player instance.
    /// </remarks>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<IMusicPlayer?> GetPlayerAsync(ulong guildId, ulong voiceChannelId, ulong userId, bool connectToVoiceChannel = true)
    {
        var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

        var customPlayerOptions = new CustomPlayerOptions();

        var result = await _audioService.Players.RetrieveAsync<CustomPlayer, CustomPlayerOptions>(guildId, voiceChannelId, playerFactory: CreatePlayer, options: Options.Create(customPlayerOptions), retrieveOptions: retrieveOptions).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to retrieve player: {result.Status}");

        // Set the player options such as volume, etc.
        await result.Player.SetVolumeAsync(0.25f).ConfigureAwait(false);        

        return result.Player;
    }

    private static ValueTask<CustomPlayer> CreatePlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new CustomPlayer(properties));
    }

    private async Task<Track?> GetTrackAsync(string query, CancellationToken cancellationToken = default)
    {
        var track = await _audioService.Tracks.LoadTrackAsync(query, TrackSearchMode.YouTubeMusic, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (track is null || track.Uri is null) { return null; }

        return new Track()
        {
            Uri = track.Uri,
            Title = track.Title,
            Author = track.Author,
            Duration = track.Duration,
            ArtworkUri = track.ArtworkUri,
            SourceName = track.SourceName
        };
    }
}
