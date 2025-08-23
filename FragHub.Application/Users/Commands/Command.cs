using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;

namespace FragHub.Application.Users.Commands
{

    /// <summary>
    /// Command base class for user-related commands.
    /// </summary>
    public class Command : ICommand
    {
        public ulong GuildId { get; set; }
        public ulong? UserId { get; set; }
        public ulong? TextChannelId { get; set; }
        public ulong? VoiceChannelId { get; set; }

        public string Description { get; set; } = string.Empty;
        public List<string> DebugDetails { get; set; } = [];
    }
}
