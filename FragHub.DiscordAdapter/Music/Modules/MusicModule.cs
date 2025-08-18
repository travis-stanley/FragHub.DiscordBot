using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using FragHub.Application;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Env;
using FragHub.Application.Music.Abstractions;
using Discord.WebSocket;

namespace FragHub.DiscordAdapter.Music.Modules;

public enum OnOffCL
{
    On, Off
}

[RequireContext(ContextType.Guild)]
[Group("music", "Music commands for playing and managing tracks")]
public sealed class MusicModule(ILogger<MusicModule> _logger, CommandDispatcher _commandDispatcher, IVariableService _variableService, IMusicService _musicService) : InteractionModuleBase<SocketInteractionContext>
{
    List<ulong> _embedMessageIds = [];
    ulong? _textChannelId;

    #region Validation

    /// <summary>
    /// Checks if the user is in a voice channel before executing any music command.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private async Task<IVoiceState?> IsUserInVoiceAsync(SocketInteractionContext context)
    {
        // check if user is in a voice channel
        if (context.User is not IVoiceState voiceState || voiceState.VoiceChannel is null)
        {
            await FollowupAsync("You must be in a voice channel to use music commands.").ConfigureAwait(false);
            return null;
        }
        return voiceState;
    }

    #endregion

    #region Utility Methods

    private async Task<SocketTextChannel?> GetTextChannelForPlayer()
    {
        // get the bot text channel from the environment variables
        var botTextChannelName = _variableService.GetVariable(DiscordAdapter.Config.DiscordConfig.TextChannelName);
        if (string.IsNullOrWhiteSpace(botTextChannelName))
        {
            await FollowupAsync("No text channel configured for the music player.").ConfigureAwait(false);
            return null;
        }

        // find the text channel by name
        var textChannel = Context.Guild.TextChannels.Where(w => w.Name.Contains(botTextChannelName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
        if (textChannel == null)
        {
            await FollowupAsync("No text channel found for the music player.").ConfigureAwait(false);
            return null;
        }

        return textChannel;
    }

    #endregion


    #region Slash Commands

    /// <summary>
    /// Register a command to play a song, artist, or URL.
    /// </summary>
    /// <param name="song_artist_url_etc"></param>
    /// <returns></returns>
    [SlashCommand("play", description: "Play or add a song to the queue", runMode: RunMode.Async)]
    public async Task Play(string song_artist_url_etc)
    {
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received play command: from {User} - {Input}", Context.User.Username, song_artist_url_etc);

        // validate input
        if (string.IsNullOrWhiteSpace(song_artist_url_etc))
        {
            await FollowupAsync("Provide a song, artist, or URL to play.").ConfigureAwait(false);
            return;
        }

        // check if user is in a voice channel
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return; }

        var cmd = GetCommand<PlayTrackCommand>(Context, voiceState);
        cmd.Query = song_artist_url_etc;
        cmd.SourceType = Domain.Music.Entities.SourceType.UserProvided;        

        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        var track = _musicService.Tracks.LastOrDefault();
        if (track is null || track.Uri is null)
        {
            await FollowupAsync($"No track found for query: {song_artist_url_etc}").ConfigureAwait(false);
            return;
        }

        await RebuildPlayer();

        await FollowupAsync($"Adding {track.Title} by {track.Author} to the play list").ConfigureAwait(false);
    }

    /// <summary>
    /// Register a command to stop the current track.
    /// </summary>
    /// <returns></returns>
    [SlashCommand("stop", description: "Stop playing the current track", runMode: RunMode.Async)]
    public async Task Stop()
    {
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received stop command: from {User}", Context.User.Username);

        // check if user is in a voice channel
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return; }

        var cmd = GetCommand<StopTrackCommand>(Context, voiceState);
        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        await RebuildPlayer();

        await FollowupAsync($"Track stopped").ConfigureAwait(false);
    }

    /// <summary>
    /// Register a command to skip the current track.
    /// </summary>
    /// <returns></returns>
    [SlashCommand("skip", description: "Skip playing the current track", runMode: RunMode.Async)]
    public async Task Skip()
    {
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received skip command: from {User}", Context.User.Username);

        // check if user is in a voice channel
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return; }

        var cmd = GetCommand<SkipTrackCommand>(Context, voiceState);
        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        await RebuildPlayer();

        await FollowupAsync($"Track skipped").ConfigureAwait(false);
    }

    /// <summary>
    /// Register a command to pause the current track.
    /// </summary>
    /// <returns></returns>
    [SlashCommand("pause", description: "Pause playing the current track", runMode: RunMode.Async)]
    public async Task Pause()
    {
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received pause command: from {User}", Context.User.Username);

        // check if user is in a voice channel
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return; }

        var cmd = GetCommand<PauseTrackCommand>(Context, voiceState);
        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        await RebuildPlayer();

        await FollowupAsync($"Track paused").ConfigureAwait(false);
    }

    /// <summary>
    /// Register a command to resume the current track.
    /// </summary>
    /// <returns></returns>
    [SlashCommand("resume", description: "Resume playing the current track", runMode: RunMode.Async)]
    public async Task Resume()
    {
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received resume command: from {User}", Context.User.Username);

        // check if user is in a voice channel
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return; }

        var cmd = GetCommand<ResumeTrackCommand>(Context, voiceState);
        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        await RebuildPlayer();

        await FollowupAsync($"Track resuming").ConfigureAwait(false);
    }

    /// <summary>
    /// Register a command to enable shuffling the playlist.
    /// </summary>
    /// <returns></returns>
    [SlashCommand("shuffle", description: "Resume playing the current track", runMode: RunMode.Async)]
    public async Task Resume(OnOffCL shuffle)
    {
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        _logger.LogInformation("Received shuffle command: from {User} - {Enable}", Context.User.Username, shuffle);

        // check if user is in a voice channel
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return; }

        var cmd = GetCommand<ShuffleTracksCommand>(Context, voiceState);
        cmd.Enabled = shuffle == OnOffCL.On;
        await _commandDispatcher.DispatchAsync(cmd).ConfigureAwait(false);

        await RebuildPlayer();

        var state = cmd.Enabled ? "enabled" : "disabled";
        await FollowupAsync($"Shuffed {state}").ConfigureAwait(false);
    }


    /// <summary>
    /// Utility method to create a command instance with the necessary context information.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="voiceState"></param>
    /// <returns></returns>
    private static T GetCommand<T>(SocketInteractionContext context, IVoiceState voiceState) where T : Command, new()
    {
        return new T
        {
            GuildId = context.Guild.Id,
            UserId = context.User.Id,
            TextChannelId = context.Channel.Id,
            VoiceChannelId = voiceState?.VoiceChannel?.Id
        };
    }

    #endregion


    #region Music Player Controller

    private async Task RebuildPlayer()
    {        
        _logger.LogInformation("Rebuilding music player embed.");
        // Get the current player embed
        var embed = await GetPlayerEmbed();
        if (embed is null)
        {
            _logger.LogError("No embed available to rebuild player.");
            return;
        }

        var textChannel = await GetTextChannelForPlayer().ConfigureAwait(false);
        if (textChannel is null)
        {
            _logger.LogError("No text channel found for the music player.");    
            return;
        }

        // Send the embed to the text channel
        var msg = await textChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        _logger.LogInformation("Player embed sent to text channel {TextChannelId} with message ID {MessageId}.", textChannel.Id, msg.Id);

        _embedMessageIds.Add(msg.Id);
    }

    private async Task BreakdownPlayer()
    {
        _logger.LogInformation("Breaking down player, clearing tracks and stopping playback.");

        var textChannel = await GetTextChannelForPlayer().ConfigureAwait(false);
        if (textChannel is null)
        {
            _logger.LogError("No text channel found for the music player.");
            return;
        }

        foreach (var messageId in _embedMessageIds)
        {
            var message = await textChannel.GetMessageAsync(messageId).ConfigureAwait(false);
            if (message is not null)
            {
                await message.DeleteAsync().ConfigureAwait(false);
                _logger.LogInformation("Deleted player embed message with ID {MessageId}.", messageId);
            }
        }
        _embedMessageIds.Clear();
    }

    private async Task<Embed?> GetPlayerEmbed()
    {
        var tracks = _musicService.Tracks.LastOrDefault();
        if (tracks is null || tracks.Uri is null)
        {
            _logger.LogError("No tracks available to rebuild player.");
            return null;
        }
        _logger.LogInformation("Rebuilding player with track: {TrackTitle} by {TrackAuthor}", tracks.Title, tracks.Author);

        var lastTrack = _musicService.Tracks.LastOrDefault();
        if (lastTrack is null) { await BreakdownPlayer(); return null; }

        var title = $":musical_note:  Music Playing  :musical_note:  in <#{lastTrack.VoiceChannelId}>";
        var currentArt = lastTrack.ArtworkUri is not null ? lastTrack.ArtworkUri.ToString() : "";
        var currentTrack = lastTrack.Title;
        var duration = lastTrack.Duration.ToString(@"hh\:mm\:ss");
        var embedUrl = lastTrack.Uri?.ToString();
        var thumbnailUrl = "https://media.tenor.com/vqpt7EB_tooAAAAi/music-clu.gif";

        var requestedBy = $"<@{lastTrack.RequestedUserId}>";
        if (lastTrack.SourceType == Domain.Music.Entities.SourceType.RecommendedByLastfm) { requestedBy = "Lastfm"; }          

        var embedfieldbuilds = new List<EmbedFieldBuilder>
        {
            new()
            {
                Name = "Now Playing",
                Value = $"{currentTrack}",
                IsInline = false
            },
            new()
            {
                Name = "Artists",
                Value = lastTrack.Author ?? "Unknown",
                IsInline = false
            },
            new()
            {
                Name = "Duration",
                Value = duration,
                IsInline = true
            },
            new()
            {
                Name = "Added By",
                Value = requestedBy,
                IsInline = true
            },
            new()
            {
                Name = "Link",
                Value = $"[SourceUrl]({embedUrl})",
                IsInline = true
            }
        };

        return new EmbedBuilder().WithColor(15835392).WithTitle(title).WithFields(embedfieldbuilds).WithThumbnailUrl(thumbnailUrl).WithImageUrl(currentArt).WithTimestamp(DateTimeOffset.Now).Build();
    }   

    #endregion
}
