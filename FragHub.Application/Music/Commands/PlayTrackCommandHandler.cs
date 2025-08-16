using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Commands;

public class PlayTrackCommandHandler(IMusicService _musicService) : ICommandHandler<PlayTrackCommand>
{
    public async Task HandleAsync(PlayTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.PlayAsync(command).ConfigureAwait(false);
    }
}
