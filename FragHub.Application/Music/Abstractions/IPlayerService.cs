using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

public interface IPlayerService
{
    Task<IMusicPlayer?> GetPlayerAsync(ulong guildId, ulong voiceChannelId, ulong userId, bool connectToVoiceChannel = true);
}
