using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.DiscordAdapter.Config;

/// <summary>
/// Environmental variable names used to retrieve configuration settings for the lavalink server.
/// Do not set these variables to actual values. Set them in the .env file.
/// </summary>
public class LavalinkConfig
{
    public const string Host = "LAVALINK_HOST";
    public const string Port = "LAVALINK_PORT";
    public const string Password = "LAVALINK_PW";
    public const string Label = "LAVALINK_LABEL";
}
