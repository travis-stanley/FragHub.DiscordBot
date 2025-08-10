using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.DiscordAdapter.Config;

/// <summary>
/// Environmental variable names used to retrieve configuration settings for the Discord bot.
/// Do not set these variables to actual values. Set them in the .env file.
/// </summary>
public class DiscordConfig
{
    public const string BotToken = "DISCORD_TOKEN";
    public const string TextChannelName = "DISCORD_TEXT_CHANNEL_NAME";

    // Used for development purposes to register commands in a specific guild for faster testing
    public const string DevelopmentGuildId = "DISCORD_DEVELOPMENT_GUILD_ID"; 
}
