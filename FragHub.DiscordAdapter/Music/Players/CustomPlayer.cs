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
using System.Text;
using System.Threading.Tasks;

namespace FragHub.DiscordAdapter.Music.Players;

public class CustomPlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties) : QueuedLavalinkPlayer(properties), IInactivityPlayerListener, IMusicPlayer
{
    /// <summary>
    /// Command history for this player.
    /// </summary>
    public IEnumerable<ICommand> GetCommands() => _commands.AsReadOnly();
    private readonly List<ICommand> _commands = [];


    #region Event Handlers

    public ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        return default;
    }

    protected override async ValueTask NotifyTrackStartedAsync(ITrackQueueItem queueItem, CancellationToken cancellationToken = default)
    {
        await base.NotifyTrackStartedAsync(queueItem, cancellationToken);
    }

    public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        return default;
    }

    #endregion
   

    #region Commands

    public async Task<int> PlayAsync(ICommand command, Track track)
    {
        _commands.Add(command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));
        ArgumentNullException.ThrowIfNull(track?.Uri, nameof(track.Uri));        

        return await PlayAsync(track.Uri).ConfigureAwait(false);
    }

    public async Task SkipAsync(ICommand command)
    {
        _commands.Add(command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        await SkipAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(ICommand command)
    {
        _commands.Add(command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        await StopAsync().ConfigureAwait(false);
    }

    public void ToggleShuffle(ICommand command)
    {
        _commands.Add(command ?? throw new ArgumentNullException(nameof(command), "Command cannot be null."));

        this.Shuffle = !this.Shuffle;
    }

    #endregion

}
