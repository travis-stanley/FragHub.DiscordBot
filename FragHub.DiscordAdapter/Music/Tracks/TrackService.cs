using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Discord.Music.Tracks;

public class TrackService(IAudioService _audioService) : ITrackService
{
    
    public async Task<Track?> GetTrackAsync(string query, CancellationToken cancellationToken = default)
    {
        var track = await _audioService.Tracks.LoadTrackAsync(query, TrackSearchMode.YouTubeMusic, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new Track() { Uri = track?.Uri, Title = track?.Title };
    }
}
