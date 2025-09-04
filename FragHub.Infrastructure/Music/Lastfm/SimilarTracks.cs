using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Infrastructure.Music.Lastfm
{
    public class GetSimilarResponse
    {
        [JsonProperty("similartracks")]
        public SimilarTracks? SimilarTracks { get; set; }
    }

    public class SimilarTracks
    {
        [JsonProperty("track")]
        public Track[]? Tracks { get; set; }
    }

    public class Track
    {
        [JsonProperty("name")]
        public string? TrackName { get; set; }

        [JsonProperty("playcount")]
        public int? PlayCount { get; set; }

        [JsonProperty("artist")]
        public Artist? Artist { get; set; }
    }

    public class Artist
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }
}
