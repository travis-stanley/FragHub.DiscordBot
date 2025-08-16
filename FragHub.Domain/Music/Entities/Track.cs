using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Domain.Music.Entities;

public class Track
{
    public Guid Id { get; } = Guid.NewGuid();

    public Uri? Uri { get; set; }

    public string? Title { get; set; }    

    public string Author { get; init; } = null!;

    public TimeSpan Duration { get; init; }

    public Uri? ArtworkUri { get; init; }

    public string? SourceName { get; init; }
}