using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;

namespace FragHub.Application.Music.Commands
{

    /// <summary>
    /// Command base class for music-related commands.
    /// </summary>
    public class Command : ICommand
    {
        public ulong GuildId { get; set; }
        public ulong? UserId { get; set; }
        public ulong? TextChannelId { get; set; }
        public ulong? VoiceChannelId { get; set; }
    }
}
