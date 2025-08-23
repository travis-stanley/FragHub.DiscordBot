using Discord;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FragHub.DiscordAdapter.Music.Players;

/// <summary>
/// Lavalink music player implementation that supports inactivity tracking and music player commands.
/// Players may be destroyed after periods of inactivity to conserve resources.
/// Do not store state in this class as players may be recreated.
/// </summary>
/// <param name="properties"></param>
public sealed class LavalinkPlayer(IPlayerProperties<LavalinkPlayer, LavalinkPlayerOptions> properties) : QueuedLavalinkPlayer(properties), IInactivityPlayerListener, IMusicPlayer
{
    string Id => this.GuildId.ToString();
    string IMusicPlayer.GuildId => this.GuildId.ToString();
    private readonly IMusicService? _musicService = properties.Options?.Value?.MusicService;

    #region Event Handlers

    public ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        _musicService?.NotifyPlayerTracked(Id);
        return default;
    }
    public ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        _musicService?.NotifyStateChanged(Id, Application.Music.Abstractions.PlayerState.Active);
        return default;
    }
    public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        _musicService?.NotifyStateChanged(Id, Application.Music.Abstractions.PlayerState.Inactive);
    }

    protected override async ValueTask NotifyTrackStartedAsync(ITrackQueueItem queueItem, CancellationToken cancellationToken = default)
    {
        await base.NotifyTrackStartedAsync(queueItem, cancellationToken);
        _musicService?.NotifyTrackStarted(Id);
    }

    #endregion
   

    #region Commands

    public async Task<int> PlayAsync(ICommand command, Track track)
    {
        ArgumentNullException.ThrowIfNull(track, nameof(track));
        ArgumentNullException.ThrowIfNull(track?.Uri, nameof(track.Uri));

        command.Description = $"Play track: {(track is null ? "Unknwon" : track.Title)}";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));               

        return await PlayAsync(track.Uri, enqueue: true).ConfigureAwait(false);
    }

    public async Task StopAsync(ICommand command)
    {
        command.Description = $"Stop playing";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        await StopAsync().ConfigureAwait(false);
    }

    public async Task SkipAsync(ICommand command)
    {
        var track = _musicService?.GetTracks(Id).FirstOrDefault();

        command.Description = $"Skip track: {(track is null ? "Unknown" : track.Title)}";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        await SkipAsync().ConfigureAwait(false);
    }

    public async Task PauseAsync(ICommand command)
    {
        command.Description = $"Pause playing";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));
        
        await PauseAsync().ConfigureAwait(false);
    }

    public async Task ResumeAsync(ICommand command)
    {
        command.Description = $"Resume playing";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        await ResumeAsync().ConfigureAwait(false);
    }

    public void SetShuffle(ICommand command, bool enabled)
    {
        command.Description = $"Toggle shuffle {(enabled ? "On" : "Off")}";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        this.Shuffle = enabled;
    }

    public async Task MoveToTopOfQueue(ICommand command, Track queuedTrack)
    {
        command.Description = $"Move {queuedTrack.Title} to queue top";
        _musicService?.AddCommand(Id, command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        var queueItem = this.Queue.Where(q => q.Identifier.Contains(queuedTrack.Identifier ?? "Unknown", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();        

        if (queueItem != null)
        {            
            await this.Queue.RemoveAsync(queueItem);
            await this.Queue.InsertAsync(0, queueItem);
            command.DebugDetails.Add($"Player Queued Items: {string.Join("|", this.Queue.Select(q => q.Track?.Title))}");
        }
        else { command.DebugDetails.Add($"Queued item from the music player not found"); }
    }
    #endregion

}
