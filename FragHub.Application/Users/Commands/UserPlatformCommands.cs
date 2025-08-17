using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Domain.Users.Entities;

namespace FragHub.Application.Users.Commands;


/// <summary>
/// Link a Discord user to a game platform user.
/// </summary>
public class LinkPlatformUserCommand : Command 
{
    public GamePlatformType GamePlatformType { get; set; }
    public string PlatformUserName { get; set; } = string.Empty;
}

