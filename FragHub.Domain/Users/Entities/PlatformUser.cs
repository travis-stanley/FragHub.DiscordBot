using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Domain.Users.Entities
{
    /// <summary>
    /// Represents a discord user linked to a game platform user.
    /// </summary>
    /// <remarks>
    /// This will be persisted in the database.
    /// </remarks>
    public class PlatformUser
    {
        public int Id { get; set; }
        public ulong DiscordUserId { get; set; }
        public GamePlatformType GamePlatformType { get; set; }
        public string PlatformUserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a new platform user to be added.
    /// </summary>
    public class NewPlatformUser
    {
        public ulong DiscordUserId { get; set; }
        public GamePlatformType GamePlatformType { get; set; }
        public string PlatformUserName { get; set; } = string.Empty;
    }
}
