using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Commands;

public class MusicCommandHandler(IMusicService _musicService) : 
    ICommandHandler<PlayTrackCommand>,
    ICommandHandler<StopTrackCommand>,
    ICommandHandler<SkipTrackCommand>,
    ICommandHandler<PauseTrackCommand>,
    ICommandHandler<ResumeTrackCommand>,
    ICommandHandler<ShuffleTracksCommand>,
    ICommandHandler<MoveToTopOfQueueCommand>,
    ICommandHandler<AddRecommendationCommand>
{

    public async Task HandleAsync(PlayTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.PlayAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(StopTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.StopAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(SkipTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.SkipAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(PauseTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.PauseAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(ResumeTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.ResumeAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(ShuffleTracksCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.SetShuffleAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(MoveToTopOfQueueCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.MoveToTopOfQueueAsync(command).ConfigureAwait(false);
    }

    public async Task HandleAsync(AddRecommendationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _musicService.AddRecommendationAsync(command).ConfigureAwait(false);
    }
}
