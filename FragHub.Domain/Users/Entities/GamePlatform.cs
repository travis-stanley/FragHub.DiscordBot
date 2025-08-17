using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Domain.Users.Entities
{
    public enum GamePlatformType
    {
        ElectronicArts
    }

    /// <summary>
    /// Represents a game platform that can be linked to a user.
    /// </summary>
    /// <remarks>
    /// This will be persisted in the database.
    /// </remarks>
    public class GamePlatform
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public GamePlatformType Type { get; set; }
        public string? BaseApiUrl { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }
}
