using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Discord.Music.Players;

public class CustomPlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties) : QueuedLavalinkPlayer(properties), IInactivityPlayerListener, IMusicPlayer
{
    public ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<int> PlayAsync(Track track)
    {
        ArgumentNullException.ThrowIfNull(track?.Uri, nameof(track.Uri));

        return await PlayAsync(track.Uri).ConfigureAwait(false);
    }

    public Task SkipAsync()
    {
        throw new NotImplementedException();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }

}
