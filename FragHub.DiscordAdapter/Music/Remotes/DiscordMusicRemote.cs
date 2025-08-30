using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FragHub.Application;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Env;
using FragHub.Domain.Music.Entities;
using Lavalink4NET.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace FragHub.DiscordAdapter.Music.Remotes;


public sealed class DiscordMusicRemote(ILogger<IMusicService> _logger, IMusicService _musicService, IPlayerSettings _playerSettings, DiscordSocketClient _discordClient, string guildId, string voiceChannelId, string textChannelName) : IMusicRemote
{    
    private List<string> _reactedComponents = [];
    private List<ulong> _embedMessageIds = [];


    public IPlayerSettings PlayerSettings { get => _playerSettings; set => _playerSettings = value; }

    public string GuildId => guildId;

    SocketTextChannel? _textChannel;

    private SocketTextChannel? GetTextChannelForPlayer()
    {
        if (_textChannel != null)
            return _textChannel;

        // get the bot text channel
        if (string.IsNullOrWhiteSpace(textChannelName))
        {
            _logger.LogError("Bot text channel name is not configured.");
            return null;
        }

        // find the guild by id
        var guild = _discordClient.GetGuild(ulong.Parse(guildId));
        if (guild == null)
        {
            _logger.LogError("Guild not found for id: {GuildId}", guildId);
            return null;
        }

        // find the text channel by name
        var textChannel = guild.TextChannels.Where(w => w.Name.Contains(textChannelName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
        if (textChannel == null)
        {
            _logger.LogError("Text channel containing '{TextChannelName}' not found in guild '{GuildId}'", textChannelName, guildId);
            return null;
        }

        _textChannel = textChannel;
        return textChannel;
    }


    /// <summary>
    /// When a track changes, rebuild the remote with new track information.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task OnTrackStarted()
    {
        await RefreshRemote(false).ConfigureAwait(false);
    }

    public async Task OnInteractionHandled()
    {        
        await RefreshRemoteButtons().ConfigureAwait(false);
    }

    public async Task OnStateChanged(PlayerState state)
    {
        if (state == PlayerState.Inactive)
        {
            await RefreshRemote(true).ConfigureAwait(false);            
        }
    }
    public async Task OnRecommendationHandled(string? interactionId)
    {
        if (interactionId is not null) { _reactedComponents.Add(interactionId); }
        await OnInteractionHandled();
    }

    public Task OnPlayerTracked()
    {
        return Task.Run(() => {  });
    }



    #region Music Remote

    /// <summary>
    /// Breakdown the music player controller.
    /// </summary>
    /// <returns></returns>
    private async Task DeleteRemote()
    {
        _logger.LogInformation("Breaking down player.");

        var textChannel = GetTextChannelForPlayer();
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
        _reactedComponents.Clear();
    }

    /// <summary>
    /// re/Builds the music player embed and sends it to the text channel.
    /// </summary>
    /// <returns></returns>
    private async Task RefreshRemote(bool isInactive)
    {
        await DeleteRemote().ConfigureAwait(false);

        var embed = isInactive ? BuildRemoteEmbedNothingPlaying() : await BuildRemoteEmbed();            
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

        var textChannel = GetTextChannelForPlayer();
        if (textChannel is null)
        {
            _logger.LogError("No text channel found for the music player.");
            return;
        }

        // Send the embed to the text channel
        var msg = await textChannel.SendMessageAsync(embed: embed, components: msgComponents).ConfigureAwait(false);
        _logger.LogInformation("Player embed sent to text channel {TextChannelId} with message ID {MessageId}.", textChannel.Id, msg.Id);

        _embedMessageIds.Add(msg.Id);
    }

    /// <summary>
    /// Refresh the player components.
    /// </summary>
    /// <returns></returns>
    private async Task RefreshRemoteButtons()
    {
        if (_embedMessageIds.Count > 0)
        {
            var lastEmbedMessageId = _embedMessageIds.Last();

            var msgComponents = await GetPlayerComponents();
            if (msgComponents != null)
            {
                var _textChannel = GetTextChannelForPlayer();
                if (_textChannel is not null)
                    await _textChannel.ModifyMessageAsync(lastEmbedMessageId, m => { m.Components = msgComponents; });
            }
        }
    }    

    /// <summary>
    /// Builds out the player embed with the current track information and controls.
    /// </summary>
    /// <returns></returns>
    private async Task<Embed?> BuildRemoteEmbed()
    {
        var tracks = _musicService.GetTracks(guildId).LastOrDefault();
        if (tracks is null || tracks.Uri is null)
        {
            _logger.LogError("No tracks available to rebuild player.");
            return null;
        }
        _logger.LogInformation("Rebuilding player with track: {TrackTitle} by {TrackAuthor}", tracks.Title, tracks.Author);

        var lastTrack = _musicService.GetTracks(guildId).LastOrDefault();
        if (lastTrack is null) { await DeleteRemote(); return null; }

        var title = $":musical_note:  Music Playing  :musical_note:  in <#{lastTrack.VoiceChannelId}>";
        var currentArt = lastTrack.ArtworkUri is not null ? lastTrack.ArtworkUri.ToString() : "";
        var currentTrack = lastTrack.Title;
        var duration = lastTrack.Duration.ToString(@"hh\:mm\:ss");
        var embedUrl = lastTrack.Uri?.ToString();
        var thumbnailUrl = "https://media.tenor.com/vqpt7EB_tooAAAAi/music-clu.gif";

        var requestedBy = $"<@{lastTrack.RequestedUserId}>";
        if (lastTrack.SourceType == SourceType.RecommendedByLastfm) { requestedBy = "Lastfm"; }

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
    /// Build embed for remote when nothing is playing
    /// </summary>
    /// <returns></returns>
    private Embed? BuildRemoteEmbedNothingPlaying()
    {        
        var title = $"Nothing to Play";

        var embedfieldbuilds = new List<EmbedFieldBuilder>
        {
            new()
            {
                Name = "Add",
                Value = $"Use music slash commands to add tracks",
                IsInline = false
            }
        };

        return new EmbedBuilder().WithColor(15835392).WithTitle(title).WithFields(embedfieldbuilds).WithTimestamp(DateTimeOffset.Now).Build();
    }

    /// <summary>
    /// Gets the player components for the music player embed.
    /// </summary>
    /// <returns></returns>
    private async Task<MessageComponent?> GetPlayerComponents()
    {
        var msgComponent = new ComponentBuilder();
        msgComponent.AddRow(GetPlayerControls());

        var queuedItems = AddQueueMenu(msgComponent);

        // if queue is empty, send add recommendation cmd using one of the 5 recommendations
        if (queuedItems is not null && queuedItems.Length == 0)
        {
            var recommendations = AddRecommendationActions(msgComponent);
            if (recommendations != null)
            {
                var next = recommendations.Where(t => t.Title is not null && t.Author is not null).OrderBy(t => Guid.NewGuid()).FirstOrDefault();
                var nextBtnId = Array.IndexOf(recommendations, next) + 1;

                if (next != null)
                {
                    var cmd = new AddRecommendationCommand()
                    {
                        GuildId = ulong.Parse(guildId),
                        UserId = null,
                        VoiceChannelId = ulong.Parse(voiceChannelId),
                        TextChannelId = GetTextChannelForPlayer()?.Id,
                        Query = $"{next.Author} {next.Title}",
                        SourceType = SourceType.RecommendedByLastfm,
                        BtnId = $"PlayerAddRec{nextBtnId}Btn"
                    };
                    await _musicService.AddRecommendationAsync(cmd);
                }
            }
        }        

        return msgComponent.Build();
    }

    /// <summary>
    /// Builds the controls for the player.
    /// </summary>
    /// <returns></returns>
    private ActionRowBuilder GetPlayerControls()
    {        
        var shuffleState = PlayerSettings.IsShuffling;

        var playerActionRow = new ActionRowBuilder();
        playerActionRow.WithButton("Skip", customId: "PlayerSkip", emote: new Emoji("\u23E9"), disabled: _reactedComponents.Contains("PlayerSkip"));

        if (shuffleState)
            playerActionRow.WithButton("Standard", "PlayerShuffleOff", ButtonStyle.Primary, emote: new Emoji("\u27A1"), disabled: _reactedComponents.Contains("PlayerShuffleOff"));
        else
            playerActionRow.WithButton("Shuffle", "PlayerShuffleOn", ButtonStyle.Primary, emote: new Emoji("\uD83D\uDD00"), disabled: _reactedComponents.Contains("PlayerShuffleOn"));

        playerActionRow.WithButton("Stop", "PlayerStop", ButtonStyle.Danger, emote: new Emoji("\u2716"), disabled: _reactedComponents.Contains("PlayerStop"));
        playerActionRow.WithButton("Next Up", "PlayerQueueBtn", ButtonStyle.Secondary, new Emoji("\u2935"), disabled: true);

        return playerActionRow;
    }

    /// <summary>
    /// Builds the queue menu for the player.
    /// </summary>
    /// <returns></returns>
    private Track[]? AddQueueMenu(ComponentBuilder componentBuilder)
    {
        var queuedTracks = _musicService.GetQueuedTracks(guildId).ToArray();

        var customId = "PlayerQueue";
        var queueMenu = new SelectMenuBuilder().WithCustomId(customId).WithMinValues(1).WithMaxValues(1);

        if (_reactedComponents.Contains(customId)) { queueMenu.IsDisabled = true; }

        if (queuedTracks.Length > 0)
        {
            for (int x = 0; x < queuedTracks.Length; x++)
            {
                var customTrack = queuedTracks[x];
                if (customTrack is null) { continue; }

                string requestedBy = "Unknown";
                if (customTrack.RequestedUserId.HasValue) { requestedBy = $"<@{customTrack.RequestedUserId.Value}>"; }
                if (customTrack?.SourceType == SourceType.RecommendedByLastfm) { requestedBy = "Lastfm"; }

                var trackName = customTrack?.Title ?? "Unknown Track";
                var artistName = customTrack?.Author ?? "Unknown Artist";

                var label = $"{trackName} - {artistName}";
                if (label.Length > 80) { label = label[..80]; }
                
                queueMenu.AddOption(label, queuedTracks[x].Identifier, $"Queue position {x + 1} ({requestedBy})", isDefault: x == 0);
            }
        }
        else
        {
            queueMenu.AddOption($"Queue is empty", "Empty", "Play more songs to update the queue", isDefault: true);
        }

        var playerQueueRow = new ActionRowBuilder().WithSelectMenu(queueMenu);
        componentBuilder.AddRow(playerQueueRow);

        return queuedTracks;
    }

    /// <summary>
    /// Builds the recommendation actions for the player.
    /// </summary>
    /// <param name="componentBuilder"></param>
    /// <returns>Recommendataions</returns>
    private Track[]? AddRecommendationActions(ComponentBuilder componentBuilder)
    {
        var recActionRow = new ActionRowBuilder();
        recActionRow.WithButton("Lastfm Recommendations", "PlayerRecLabelBtn", ButtonStyle.Success, new Emoji("\U0001F4FB"), disabled: true);
        componentBuilder.AddRow(recActionRow);

        var addedEmoji = new Emoji("\u2705");
        var addEmoji = new Emoji("\u2795");

        var nextRecommended = _musicService.GetRecommendations(guildId);

        var recRecRow = new ActionRowBuilder();
        for (int x = 0; x < nextRecommended.Take(5).ToList().Count; x++)
        {            
            var customId = $"PlayerAddRec{x + 1}Btn";
            var disabled = _reactedComponents.Contains(customId);
            var emoteToUse = _reactedComponents.Contains(customId) ? addedEmoji : addEmoji;

            var label = $"{nextRecommended[x].Title} - {nextRecommended[x].Author}";
            if (label.Length > 80) { label = label[..80]; }

            recRecRow.WithButton(label, customId, ButtonStyle.Secondary, emoteToUse, disabled: disabled);
        }
        componentBuilder.AddRow(recRecRow);

        return nextRecommended;
    }

    #endregion



    
}
