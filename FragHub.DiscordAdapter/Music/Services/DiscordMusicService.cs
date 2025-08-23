using Discord;
using Discord.WebSocket;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Application.Users.Commands;
using FragHub.DiscordAdapter.Music.Players;
using FragHub.DiscordAdapter.Music.Remotes;
using FragHub.Domain.Music.Entities;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FragHub.DiscordAdapter.Music.Services;

public class DiscordMusicService(ILogger<IMusicService> _logger, IAudioService _audioService, DiscordSocketClient _discordClient, IVariableService _variableService, IMusicRecommendationService _musicRecommendationService) : IMusicService
{
    private readonly Dictionary<string, List<ICommand>> _commands = [];
    private readonly Dictionary<string, List<IMusicRemote>> _remotes = [];
    private readonly Dictionary<string, List<Track>> _tracks = [];
    private readonly Dictionary<string, Queue<Track>> _queuedTracks = [];
    private readonly Dictionary<string, List<Track>> _recommendations = [];

    #region Remotes

    public IEnumerable<T> GetRemotes<T>(string playerId) where T : IMusicRemote
    {
        return _remotes.TryGetValue(playerId, out List<IMusicRemote>? value) ? value.OfType<T>() ?? []: [];
    }
    public IEnumerable<T> AddRemote<T>(string playerId, T remote) where T : IMusicRemote
    {
        ArgumentNullException.ThrowIfNull(remote, nameof(remote));
        if (!_remotes.TryGetValue(playerId, out List<IMusicRemote>? value)) { value = []; _remotes[playerId] = value; }
        value.Add(remote);

        return GetRemotes<T>(playerId);
    }

    #endregion 

    #region Events

    public async Task NotifyPlayerTracked(string id)
    {
        _logger.LogInformation("Player tracked: {PlayerId}", id);
        var remotes = GetRemotes<DiscordMusicRemote>(id);
        foreach (var remote in remotes) { await remote.OnPlayerTracked(); remote.PlayerSettings.PlayerState = Application.Music.Abstractions.PlayerState.Stopped; }
    }
    public async Task NotifyStateChanged(string id, Application.Music.Abstractions.PlayerState state)
    {
        _logger.LogInformation("Player state changed: {PlayerId}, State: {State}", id, state);
        var remotes = GetRemotes<DiscordMusicRemote>(id);
        foreach (var remote in remotes) { await remote.OnStateChanged(state); remote.PlayerSettings.PlayerState = state; }
    }
    public async Task NotifyTrackStarted(string id)
    {
        _logger.LogInformation("Track started: {PlayerId}", id);
        var playerQueue = GetQueuedTracks(id);
        var nextTrack = playerQueue.Dequeue();
        AddTrack(id, nextTrack);
        await RefreshRecommendations(id, nextTrack);

        var remotes = GetRemotes<DiscordMusicRemote>(id);
        foreach (var remote in remotes) { await remote.OnTrackStarted(); remote.PlayerSettings.PlayerState = Application.Music.Abstractions.PlayerState.Playing; }
    }
    public async Task NotifyInteractionHandled(string id)
    {
        _logger.LogInformation("Interaction handled: {PlayerId}", id);

        var remotes = GetRemotes<DiscordMusicRemote>(id);
        foreach (var remote in remotes) { await remote.OnInteractionHandled(); }
    }

    #endregion


    #region Command Management

    public IEnumerable<ICommand> GetCommands(string playerId) => _commands.TryGetValue(playerId, out List<ICommand>? value) ? [.. value] : [];

    public void AddCommand(string playerId, ICommand command)
    {
           ArgumentNullException.ThrowIfNull(command, nameof(command));
            if (!_commands.TryGetValue(playerId, out List<ICommand>? value)) { value = []; _commands[playerId] = value; }
            value.Add(command);
    }

    #endregion


    #region Track Management

    public Track[] GetTracks(string playerId) => _tracks.TryGetValue(playerId, out List<Track>? value) ? [.. value] : [];   

    private void AddTrack(string playerId, Track track)
    {
        ArgumentNullException.ThrowIfNull(track, nameof(track));
        if (!_tracks.TryGetValue(playerId, out List<Track>? value)) { value = []; _tracks[playerId] = value; }
        value.Add(track);
    }

    public Queue<Track> GetQueuedTracks(string playerId) => _queuedTracks.TryGetValue(playerId, out Queue<Track>? value) ? value : [];

    private void EnqueueTrack(string playerId, Track track)
    {
        ArgumentNullException.ThrowIfNull(track, nameof(track));
        if (!_queuedTracks.TryGetValue(playerId, out Queue<Track>? value)) { value = new Queue<Track>(); _queuedTracks[playerId] = value; }
        value.Enqueue(track);
    }
    #endregion


    #region Recommendations

    public Track[] GetRecommendations(string playerId) => _recommendations.TryGetValue(playerId, out List<Track>? value) ? [.. value] : [];
    private void SetRecommendations(string playerId, Track[] tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks, nameof(tracks));
        if (!_recommendations.TryGetValue(playerId, out List<Track>? value)) { value = []; _recommendations[playerId] = value; }
        _recommendations[playerId] = [.. tracks];
    }

    private async Task RefreshRecommendations(string playerId, Track? playingTrack)
    {
        if (playingTrack == null) { return; }
        var tracks = await _musicRecommendationService.GetRecommendations(5, playingTrack).ConfigureAwait(false);

        if (tracks == null) { return; }
        SetRecommendations(playerId, [.. tracks]);
    }

    #endregion


    #region Commands

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

        var track = await GetTrackAsync(command).ConfigureAwait(false) ?? throw new InvalidOperationException($"Track not found for query: {command.Query}");
        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");
        
        EnqueueTrack(command.GuildId.ToString(), track);        

        var remotes = GetRemotes<DiscordMusicRemote>(command.GuildId.ToString());
        foreach (var remote in remotes) { remote.PlayerSettings.PlayerState = Application.Music.Abstractions.PlayerState.Playing; }

        await player.PlayAsync(command, track).ConfigureAwait(false);
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task StopAsync(StopTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var currentTrack = GetTracks(command.GuildId.ToString()).LastOrDefault();
        if (currentTrack != null) { currentTrack.WasPlayed = false; currentTrack.LastInteractedUserId = command.UserId; }

        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");        

        var remotes = GetRemotes<DiscordMusicRemote>(command.GuildId.ToString());
        foreach (var remote in remotes) { remote.PlayerSettings.PlayerState = Application.Music.Abstractions.PlayerState.Stopped; }

        await player.StopAsync(command).ConfigureAwait(false);
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task SkipAsync(SkipTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var currentTrack = GetTracks(command.GuildId.ToString()).LastOrDefault();
        if (currentTrack != null) { currentTrack.WasPlayed = false; currentTrack.WasSkipped = true; currentTrack.LastInteractedUserId = command.UserId; }

        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");

        await player.SkipAsync(command).ConfigureAwait(false);
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task PauseAsync(PauseTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var currentTrack = GetTracks(command.GuildId.ToString()).LastOrDefault();
        if (currentTrack != null) { currentTrack.WasPlayed = false; currentTrack.LastInteractedUserId = command.UserId; }

        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");        

        var remotes = GetRemotes<DiscordMusicRemote>(command.GuildId.ToString());
        foreach (var remote in remotes) { remote.PlayerSettings.PlayerState = Application.Music.Abstractions.PlayerState.Paused; }

        await player.PauseAsync(command).ConfigureAwait(false);
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task ResumeAsync(ResumeTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var currentTrack = GetTracks(command.GuildId.ToString()).LastOrDefault();
        if (currentTrack != null) { currentTrack.WasPlayed = true; currentTrack.LastInteractedUserId = command.UserId; }

        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");        

        var remotes = GetRemotes<DiscordMusicRemote>(command.GuildId.ToString());
        foreach (var remote in remotes) { remote.PlayerSettings.PlayerState = Application.Music.Abstractions.PlayerState.Playing; }

        await player.ResumeAsync(command).ConfigureAwait(false);
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task SetShuffleAsync(ShuffleTracksCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");        

        var remotes = GetRemotes<DiscordMusicRemote>(command.GuildId.ToString());
        foreach (var remote in remotes) { remote.PlayerSettings.IsShuffling = command.Enabled; }

        player.SetShuffle(command, command.Enabled);
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task MoveToTopOfQueueAsync(MoveToTopOfQueueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var player = await GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false) ?? throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}");        

        var playerQueue = GetQueuedTracks(command.GuildId.ToString());
        var track = playerQueue.Where(t => t.Identifier == command.TrackIdentifier).FirstOrDefault();
        if (track != null)
        {
            var newQueue = new Queue<Track>();
            newQueue.Enqueue(track);
            foreach (var other in playerQueue)
            {
                if (other.Identifier == command.TrackIdentifier) { continue; }
                newQueue.Enqueue(other);
            }
            playerQueue = newQueue;
        }
        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    public async Task AddRecommendationAsync(AddRecommendationCommand command)
    {
        var playCmd = new PlayTrackCommand() { GuildId = command.GuildId, UserId = command.UserId, VoiceChannelId = command.VoiceChannelId, TextChannelId = command.TextChannelId, SourceType = command.SourceType, Query = command.Query };
        await PlayAsync(playCmd).ConfigureAwait(false);

        await NotifyInteractionHandled(command.GuildId.ToString()).ConfigureAwait(false);
    }

    #endregion

    #region Player Management

    /// <summary>
    /// Player factory method to create a LavalinkPlayer instance.
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static ValueTask<Players.LavalinkPlayer> CreatePlayer(IPlayerProperties<Players.LavalinkPlayer, Players.LavalinkPlayerOptions> properties, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new Players.LavalinkPlayer(properties));
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

        var customPlayerOptions = new Players.LavalinkPlayerOptions(this);

        var result = await _audioService.Players.RetrieveAsync<Players.LavalinkPlayer, Players.LavalinkPlayerOptions>(guildId, voiceChannelId, playerFactory: CreatePlayer, options: Options.Create(customPlayerOptions), retrieveOptions: retrieveOptions).ConfigureAwait(false);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to retrieve player: {result.Status}");

        // create and assign a DiscordMusicRemote to the player for handling interactions, etc.
        var remotes = GetRemotes<DiscordMusicRemote>(guildId.ToString());
        if (remotes == null || !remotes.Any())
        {
            var botTextChannelName = _variableService.GetVariable(Config.DiscordConfig.TextChannelName) ?? string.Empty;
            var remote = new DiscordMusicRemote(_logger, this, new LavalinkPlayerSettings(), _discordClient, guildId.ToString(), botTextChannelName);
            remotes = AddRemote(guildId.ToString(), remote);
        }

        // Set the player options such as volume, etc.
        await result.Player.SetVolumeAsync(0.25f).ConfigureAwait(false);
        foreach (var item in remotes) { item.PlayerSettings.Volume = 0.25f; }

        return result.Player;
    }

    /// <summary>
    /// Gets a track from the audio service based on the play command query.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Track?> GetTrackAsync(PlayTrackCommand command, CancellationToken cancellationToken = default)
    {
        var track = await _audioService.Tracks.LoadTrackAsync(command.Query, TrackSearchMode.YouTubeMusic, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (track is null || track.Uri is null) { return null; }

        return new Track()
        {
            Identifier = track.Identifier,
            Uri = track.Uri,
            Title = track.Title,
            Author = track.Author,
            Duration = track.Duration,
            ArtworkUri = track.ArtworkUri,
            SourceName = track.SourceName,
            GuildId = command.GuildId,
            RequestedUserId = command.UserId,
            SourceType = command.SourceType,
            VoiceChannelId = command.VoiceChannelId ?? 0
        };
    }

    #endregion

}
