using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

public interface IMusicPlayer
{
    Task<int> PlayAsync(ICommand command, Track track);    
    Task StopAsync(ICommand command);
    Task SkipAsync(ICommand command);
    Task PauseAsync(ICommand command);
    Task ResumeAsync(ICommand command);    
    void SetShuffle(ICommand command, bool enabled);

    IEnumerable<ICommand> GetCommands();
}
