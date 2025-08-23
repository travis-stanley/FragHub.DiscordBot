using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

/// <summary>
/// Defines the contract for a music player capable of handling playback commands and managing a queue of tracks.
/// </summary>
public interface IMusicPlayer
{
    string GuildId { get; }

    Task<int> PlayAsync(ICommand command, Track track);    
    Task StopAsync(ICommand command);
    Task SkipAsync(ICommand command);
    Task PauseAsync(ICommand command);
    Task ResumeAsync(ICommand command);    
    void SetShuffle(ICommand command, bool enabled);     
}
