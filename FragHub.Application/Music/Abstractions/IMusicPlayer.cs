using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

public interface IMusicPlayer
{
    Task<int> PlayAsync(Track track);
    Task SkipAsync();
    Task StopAsync();
}
