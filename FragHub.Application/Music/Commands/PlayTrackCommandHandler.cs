using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Commands;

public class PlayTrackCommandHandler(IPlayerService _playerService, ITrackService _trackService) : ICommandHandler<PlayTrackCommand>
{
    public async Task HandleAsync(PlayTrackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        if (!command.VoiceChannelId.HasValue) { throw new ArgumentNullException(nameof(command.VoiceChannelId), "VoiceChannelId must be provided."); }
        if (!command.UserId.HasValue) { throw new ArgumentNullException(nameof(command.UserId), "UserId must be provided."); }

        var track = await _trackService.GetTrackAsync(command.Query).ConfigureAwait(false) ?? throw new InvalidOperationException($"Track not found for query: {command.Query}");       
        var player = await _playerService.GetPlayerAsync(command.GuildId, command.VoiceChannelId.Value, command.UserId.Value).ConfigureAwait(false);
        if (player is null) { throw new InvalidOperationException($"Failed to retrieve player for GuildId: {command.GuildId}, VoiceChannelId: {command.VoiceChannelId.Value}, UserId: {command.UserId.Value}"); }

        await player.PlayAsync(track).ConfigureAwait(false);
    }
}
