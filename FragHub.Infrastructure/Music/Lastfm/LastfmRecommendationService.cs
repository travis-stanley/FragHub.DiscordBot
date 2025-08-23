using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Infrastructure.Music.Lastfm
{
    public class LastfmRecommendationService(ILogger<LastfmRecommendationService> _logger, IVariableService _variableService) : IMusicRecommendationService
    {
        public async Task<IEnumerable<Domain.Music.Entities.Track>> GetRecommendations(int limit, Domain.Music.Entities.Track seed)
        {
            return await GetSimilar(seed.Author, seed.Title, limit);
        }



        /// <summary>
        /// Gets similar tracks from the provided artist and track, limiting results to n.
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="track"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        /// <remarks>
        /// The returned data is returned by descending play counts so the results are fairly constant.
        /// TODO: remove tracks that have been previously played in this session
        /// TODO: only include x tracks from the same artist
        /// </remarks>
        private async Task<IEnumerable<Domain.Music.Entities.Track>> GetSimilar(string artist, string? track, int limit)
        {
            _logger.LogInformation($"Getting similar tracks for {track} by {artist}");
            var lastfmKey = _variableService.GetVariable(LastfmConfig.ApiToken) ?? string.Empty;

            if (lastfmKey == null)
            {
                _logger.LogWarning($"Lastfm Api key not set in the configuration. No recommendations will be available.");
                return [];
            }

            artist = Uri.EscapeDataString(artist);
            string requestUrl = $"https://ws.audioscrobbler.com/2.0/?method=track.getsimilar&artist={artist}&api_key={lastfmKey}&format=json";

            if (track is not null) { 
                track = Uri.EscapeDataString(track);
                requestUrl = $"https://ws.audioscrobbler.com/2.0/?method=track.getsimilar&artist={artist}&track={track}&api_key={lastfmKey}&format=json";
            }                                  

            var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var similarTracks = JsonConvert.DeserializeObject<GetSimilarResponse>(jsonResponse);

            var randomizeTracks = similarTracks?.SimilarTracks?.Tracks?.Take(10).OrderBy(o => Guid.NewGuid()).Take(limit);
            if (randomizeTracks == null) { return []; }

            return randomizeTracks.Select(t => new Domain.Music.Entities.Track() { Author = t.Artist?.Name ?? "", Title = t.TrackName });
        }
    }
}
