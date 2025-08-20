using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FragHub.Application;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Env;
using Lavalink4NET.Clients;
using Microsoft.Extensions.Logging;

namespace FragHub.DiscordAdapter.Music.Modules;

public enum OnOffCL
{
    On, Off
}

[RequireContext(ContextType.Guild)]
[Group("music", "Music commands for playing and managing tracks")]
public sealed class MusicModule(ILogger<MusicModule> _logger, CommandDispatcher _commandDispatcher, IVariableService _variableService, IMusicService _musicService) : InteractionModuleBase<SocketInteractionContext>
{
    List<string> _reactedComponents = [];
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
    public async Task Shuffle(OnOffCL shuffle)
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

    /// <summary>
    /// re/Builds the music player embed and sends it to the text channel.
    /// </summary>
    /// <returns></returns>
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

        var msgComponents = await GetPlayerComponents();
        if (msgComponents is null)
        {
            _logger.LogError("Could not build player components.");
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

    /// <summary>
    /// Breakdown the music player controller.
    /// </summary>
    /// <returns></returns>
    private async Task BreakdownPlayer()
    {
        _logger.LogInformation("Breaking down player.");

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

    /// <summary>
    /// Builds out the player embed with the current track information and controls.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Gets the player components for the music player embed.
    /// </summary>
    /// <returns></returns>
    private async Task<MessageComponent?> GetPlayerComponents()
    {
        var msgComponent = new ComponentBuilder();
        msgComponent.AddRow(await GetPlayerControls());
        msgComponent.AddRow(await GetQueueMenu());
        //AddRecommendations(msgComponent);

        return msgComponent.Build();
    }

    /// <summary>
    /// Builds the controls for the player.
    /// </summary>
    /// <returns></returns>
    private async Task<ActionRowBuilder> GetPlayerControls()
    {
        var shuffleState = await _musicService.GetShuffleState(new ShuffleStateCommand());

        var playerActionRow = new ActionRowBuilder();
        playerActionRow.WithButton("Skip", customId: "PlayerSkip", emote: new Emoji("\u23E9"), disabled: _reactedComponents.Contains("PlayerSkip"));

        if (shuffleState)
            playerActionRow.WithButton("Standard", "PlayerShuffle", ButtonStyle.Primary, emote: new Emoji("\u27A1"), disabled: _reactedComponents.Contains("PlayerShuffle"));
        else
            playerActionRow.WithButton("Shuffle", "PlayerShuffle", ButtonStyle.Primary, emote: new Emoji("\uD83D\uDD00"), disabled: _reactedComponents.Contains("PlayerShuffle"));

        playerActionRow.WithButton("Stop", "PlayerStop", ButtonStyle.Danger, emote: new Emoji("\u2716"), disabled: _reactedComponents.Contains("PlayerStop"));
        playerActionRow.WithButton("Next Up", "PlayerQueueBtn", ButtonStyle.Secondary, new Emoji("\u2935"), disabled: true);

        return playerActionRow;
    }

    /// <summary>
    /// Builds the queue menu for the player.
    /// </summary>
    /// <returns></returns>
    private async Task<ActionRowBuilder> GetQueueMenu()
    {
        var voiceState = await IsUserInVoiceAsync(Context).ConfigureAwait(false);
        if (voiceState == null) { return new ActionRowBuilder(); } // no voice state, no queue menu

        var cmd = GetCommand<QueuedTracksCommand>(Context, voiceState);
        var queuedTracks = await _musicService.GetQueuedTracks(cmd);

        var customId = "PlayerQueue";
        var queueMenu = new SelectMenuBuilder().WithCustomId(customId).WithMinValues(1).WithMaxValues(1);

        if (_reactedComponents.Contains(customId)) { queueMenu.IsDisabled = true; }

        if (queuedTracks.Length > 0)
        {
            for (int x = 0; x < queuedTracks.Count(); x++)
            {
                var customTrack = queuedTracks[x];
                var requestedBy = $"<@{customTrack.RequestedUserId}>";
                var trackName = customTrack?.Title ?? "Unknown Track";
                var artistName = customTrack?.Author ?? "Unknown Artist";

                var label = $"{trackName} - {artistName}";
                if (label.Length > 80) { label = label[..80]; }

                if (x == 0)
                    queueMenu.AddOption(label, queuedTracks[x].Identifier, $"Queue position {x + 1} ({requestedBy})", isDefault: true);
                else
                    queueMenu.AddOption(label, queuedTracks[x].Identifier, $"Queue position {x + 1} ({requestedBy})");
            }
        }
        else
        {
            queueMenu.AddOption($"Queue is empty", "Empty", "Play more songs to update the queue", isDefault: true);
        }

        var playerQueueRow = new ActionRowBuilder().WithSelectMenu(queueMenu);
        return playerQueueRow;
    }

    /// <summary>
    /// Builds the recommendation actions for the player.
    /// </summary>
    /// <param name="componentBuilder"></param>
    private void GetRecommendationActions(ComponentBuilder componentBuilder)
    {
        var recActionRow = new ActionRowBuilder();
        recActionRow.WithButton("Lastfm Recommendations", "PlayerRecLabelBtn", ButtonStyle.Success, new Emoji("\U0001F4FB"), disabled: true);
        componentBuilder.AddRow(recActionRow);

        var addedEmoji = new Emoji("\u2705");
        var addEmoji = new Emoji("\u2795");

        var recRecRow = new ActionRowBuilder();
        //for (int x = 0; x < nextRecommended.Take(5).ToList().Count; x++)
        //{
        //    var label = $"{nextRecommended[x].TrackName} - {nextRecommended[x].Artist?.Name}";
        //    var customId = $"PlayerAddRec{x + 1}Btn";
        //    var disabled = disabledCustomIds.Contains(customId);
        //    var emoteToUse = disabledCustomIds.Contains(customId) ? addedEmoji : addEmoji;

        //    if (label.Length > 80) { label = label[..80]; }

        //    recRecRow.WithButton(label, customId, ButtonStyle.Secondary, emoteToUse, disabled: disabled);
        //}
        componentBuilder.AddRow(recRecRow);
    }

    #endregion
}
