using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Commands;

/// <summary>
/// Play a track based on a query string.
/// </summary>
public class PlayTrackCommand : Command 
{ 
    public string Query { get; set; } = string.Empty; 
    public SourceType SourceType { get; set; }
}

/// <summary>
/// Stop the currently playing track.
/// </summary>
public class StopTrackCommand : Command { }

/// <summary>
/// Skip the currently playing track.
/// </summary>
public class SkipTrackCommand : Command { }

/// <summary>
/// Pause the currently playing track.
/// </summary>
public class PauseTrackCommand : Command { }

/// <summary>
/// Resume the currently paused track.
/// </summary>
public class ResumeTrackCommand : Command { }

/// <summary>
/// Shuffle the tracks in the current playlist.
/// </summary>
public class ShuffleTracksCommand : Command { public bool Enabled { get; set; } }

/// <summary>
/// Get the current shuffle state of the playlist.
/// </summary>
public class ShuffleStateCommand : Command { }

/// <summary>
/// Get the queued tracks in the current playlist.
/// </summary>
public class QueuedTracksCommand : Command { }