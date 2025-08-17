using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Application.Users.Abstractions;
using FragHub.Application.Users.Commands;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Users.Commands;

public class UserPlatformCommandHandler(IUserService _userService) : 
    ICommandHandler<LinkPlatformUserCommand>
{

    public async Task HandleAsync(LinkPlatformUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        await _userService.LinkPlatformUserAsync(command).ConfigureAwait(false);
    }

}
