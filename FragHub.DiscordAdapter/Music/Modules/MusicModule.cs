using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using FragHub.Application;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Env;
using FragHub.Application.Music.Abstractions;

namespace FragHub.DiscordAdapter.Music.Modules;

[RequireContext(ContextType.Guild)]
public sealed class MusicModule(ILogger<MusicModule> _logger, CommandDispatcher _commandDispatcher, IVariableService _variableService, IPlayerService _playerService) : InteractionModuleBase<SocketInteractionContext>
{

    [SlashCommand("play", description: "Play or add a song to the queue")]  // , runMode: RunMode.Async
    public async Task Play(string song_artist_url_etc)
    {        
        // follow up calls are tied to first, thus follow ephemeral of first
        await DeferAsync(ephemeral: true);

        _logger.LogInformation("Received play command: from {User} - {Input}", Context.User.Username, song_artist_url_etc);

        // validate input
        if (string.IsNullOrWhiteSpace(song_artist_url_etc))
        {
            await FollowupAsync("Provide a song, artist, or URL to play.").ConfigureAwait(false);
            return;
        }

        // check if user is in a voice channel
        if (Context.User is not IVoiceState voiceState || voiceState.VoiceChannel is null)
        {
            await FollowupAsync("You must be in a voice channel to play music.").ConfigureAwait(false);
            return;
        }

        // get the bot text channel from the environment variables
        var botTextChannelName = _variableService.GetVariable(DiscordAdapter.Config.DiscordConfig.TextChannelName);
        if (string.IsNullOrWhiteSpace(botTextChannelName))
        {
            await FollowupAsync("No text channel configured for the music player.").ConfigureAwait(false);
            return;
        }

        // find the text channel by name
        var textChannel = Context.Guild.TextChannels.Where(w => w.Name.Contains(botTextChannelName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault();
        if (textChannel == null) {
            await FollowupAsync("No text channel found for the music player.").ConfigureAwait(false);
            return;
        }
        
        var playCommand = new PlayTrackCommand
        {
            GuildId = Context.Guild.Id,
            UserId = Context.User.Id,
            TextChannelId = Context.Channel.Id,
            VoiceChannelId = voiceState.VoiceChannel.Id,
            Query = song_artist_url_etc
        };
        await _commandDispatcher.DispatchAsync(playCommand).ConfigureAwait(false);

        var track = _playerService.Tracks.LastOrDefault();
        if (track is null || track.Uri is null)
        {
            await FollowupAsync($"No track found for query: {song_artist_url_etc}").ConfigureAwait(false);
            return;
        }

        await FollowupAsync($"Adding {track.Title} by {track.Author} to the play list").ConfigureAwait(false);
    }




    private async Task RebuildPlayer()
    {
        var tracks = _playerService.Tracks;
    }

}
