using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FragHub.Application.Abstractions;
using DiscordConfig = FragHub.DiscordAdapter.Config.DiscordConfig;

namespace FragHub.DiscordAdapter.Bot;

public class DiscordBot(
    ILogger<DiscordBot> _logger,
    DiscordSocketClient _discordSocketClient, 
    InteractionHandler _interactionHandler, 
    IVariableService _variableService
    ) : IHostedService
{

    /// <summary>
    /// Starts the Discord bot asynchronously, initializing necessary components and connecting to Discord.
    /// </summary>
    /// <remarks>This method initializes the interaction handler, sets up event handlers for Discord client
    /// events, and logs the bot into Discord using the configured token. Ensure that the required variables, including
    /// the Discord token, are properly configured before calling this method.</remarks>
    /// <param name="cancellationToken">A token that can be used to signal the operation should be canceled.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord Bot...");

        // Initialize the interaction handler to register commands and set up event handlers
        await _interactionHandler.InitializeAsync().ConfigureAwait(false); 

        await _discordSocketClient.LoginAsync(TokenType.Bot, _variableService.GetVariable(DiscordConfig.BotToken)).ConfigureAwait(false);
        await _discordSocketClient.StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the asynchronous operation and releases resources associated with the interaction handler and Discord
    /// client.
    /// </summary>
    /// <remarks>This method disposes of the interaction handler, unsubscribes from client events, and stops
    /// the Discord client. Ensure that the method is called when the application is shutting down or no longer requires
    /// the Discord client.</remarks>
    /// <param name="cancellationToken">A token that can be used to signal the cancellation of the stop operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _interactionHandler.Dispose().ConfigureAwait(false);

        await _discordSocketClient.StopAsync().ConfigureAwait(false);
    }

}
