using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Domain.Music.Entities;

public enum SourceType
{
    UserProvided = 0, // Track provided by the user
    RecommendedByLastfm = 1, // Track recommended by the Last.fm service
}

public class Track
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? Identifier { get; set; } // Unique identifier for the track, e.g., YouTube video ID, Spotify URI, etc.

    public Uri? Uri { get; set; }

    public string? Title { get; set; }    

    public string Author { get; init; } = null!;

    public TimeSpan Duration { get; init; }

    public Uri? ArtworkUri { get; init; }

    public string? SourceName { get; init; }
    

    public ulong GuildId { get; init; }
    public ulong VoiceChannelId { get; init; }
    public ulong? RequestedUserId { get; init; }    // may be null if the track was not requested by a user (e.g., recommended track)
    public SourceType SourceType { get; init; }

    public bool WasSkipped { get; set; }
    public bool WasPlayed { get; set; }

    public DateTime DateRequested { get; init; } = DateTime.UtcNow;
}