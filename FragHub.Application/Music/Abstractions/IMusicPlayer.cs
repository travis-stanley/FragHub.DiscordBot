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
    Task SkipAsync(ICommand command);
    Task StopAsync(ICommand command);
    void ToggleShuffle(ICommand command);

    IEnumerable<ICommand> GetCommands();
}
