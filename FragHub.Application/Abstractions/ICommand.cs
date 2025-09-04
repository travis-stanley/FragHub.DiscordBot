using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Application.Abstractions;

public interface ICommand 
{
    /// <summary>
    /// Origin Guild ID.
    /// </summary>
    ulong GuildId { get; set; }

    /// <summary>
    /// Origin User ID.
    /// </summary>
    ulong? UserId { get; set; }

    /// <summary>
    /// TextChannel ID for the bot command, if applicable.
    /// </summary>
    ulong? TextChannelId { get; set; }

    /// <summary>
    /// Voice Channel ID for the command, if applicable.
    /// </summary>
    ulong? VoiceChannelId { get; set; }

    /// <summary>
    /// Description of the command.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Debug details
    /// </summary>
    List<string> DebugDetails { get; set; }
}

public interface ICommandHandler<TCommand> where TCommand : ICommand
{    
    Task HandleAsync(TCommand command);
}
