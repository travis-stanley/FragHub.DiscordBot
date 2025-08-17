using FragHub.Application.Music.Commands;
using FragHub.Application.Users.Commands;
using FragHub.Domain.Music.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Application.Users.Abstractions
{
    public interface IUserService
    {
        Task LinkPlatformUserAsync(LinkPlatformUserCommand command);
    }
}
