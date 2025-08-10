using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FragHub.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using IResult = Discord.Interactions.IResult;
using DiscordConfig = FragHub.DiscordAdapter.Config.DiscordConfig;

namespace FragHub.DiscordAdapter.Bot;


/// <summary>
/// Handles the initialization and processing of Discord interactions, including command registration and execution.
/// </summary>
/// <remarks>This class is responsible for managing the lifecycle of Discord interactions by registering commands,
/// handling interaction events, and logging relevant information. It integrates with the Discord.NET library and uses
/// the <see cref="InteractionService"/> to process commands.
/// See <see href="https://github.com/discord-net/Discord.Net/blob/dev/samples/InteractionFramework/InteractionHandler.cs">InteractionHandler</see> for more details.
/// </remarks>
/// <param name="_logger"></param>
/// <param name="_client"></param>
/// <param name="_handler"></param>
/// <param name="_services"></param>
public class InteractionHandler(ILogger<InteractionHandler> _logger, DiscordSocketClient _client, InteractionService _handler, IServiceProvider _services, IVariableService _variableService)
{

    /// <summary>
    /// Initializes the interaction handler and prepares it to process interactions.
    /// </summary>
    /// <remarks>This method sets up event handlers for client readiness, interaction creation, and command
    /// execution. It also registers public modules that inherit from <see cref="InteractionModuleBase{T}"/> with the
    /// interaction service. Call this method before using the interaction handler to ensure proper
    /// initialization.</remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        ValidateServices();

        // Process when the client is ready, so we can register our commands.
        _client.Ready += ReadyAsync;
        _handler.Log += Log;

        // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services).ConfigureAwait(false);

        // Process the InteractionCreated payloads to execute Interactions commands
        _client.InteractionCreated += HandleInteraction;

        // Also process the result of the command execution.
        _handler.InteractionExecuted += HandleInteractionExecuted;

        _logger.LogInformation("InteractionHandler initialized and ready to process interactions.");
    }

    /// <summary>
    /// Validates the required services for the application.
    /// </summary>
    /// <remarks>This method ensures that all necessary services are properly initialized before use. If any
    /// of the required services are null, an exception is thrown.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if any of the following services are null: <list type="bullet"> <item><description><paramref
    /// name="_logger"/>: The logger instance.</description></item> <item><description><paramref name="_client"/>: The
    /// DiscordSocketClient instance.</description></item> <item><description><paramref name="_handler"/>: The
    /// InteractionService instance.</description></item> <item><description><paramref name="_services"/>: The service
    /// provider instance.</description></item> </list></exception>
    private void ValidateServices()
    {        
        if (_logger == null) 
            throw new ArgumentNullException(nameof(_logger), "Logger cannot be null.");
        if (_client == null)
            throw new ArgumentNullException(nameof(_client), "DiscordSocketClient cannot be null.");
        if (_handler == null)
            throw new ArgumentNullException(nameof(_handler), "InteractionService cannot be null.");
        if (_services == null)
            throw new ArgumentNullException(nameof(_services), "Service provider cannot be null.");
    }

    /// <summary>
    /// Prepares the application by registering commands globally.
    /// </summary>
    /// <remarks>This method is typically called when the application is ready to handle commands.  It ensures
    /// that all commands are registered globally and logs the process for monitoring purposes.</remarks>
    /// <returns></returns>
    private async Task ReadyAsync()
    {
        _logger.LogInformation("Ready event triggered. Registering commands...");

        var devGuildId = _variableService.GetVariable(DiscordConfig.DevelopmentGuildId);
        if (!string.IsNullOrWhiteSpace(devGuildId))
        {
            _logger.LogInformation("Registering commands to guild with ID: {GuildId}", devGuildId);
            await _handler.RegisterCommandsToGuildAsync(ulong.Parse(devGuildId)).ConfigureAwait(false);
        }

        await _handler.RegisterCommandsGloballyAsync().ConfigureAwait(false);   // takes long to propagate        

        _logger.LogInformation("Commands registered globally.");
    }

    /// <summary>
    /// Logs the specified interaction message using the application's logging infrastructure.
    /// </summary>
    /// <param name="logMessage">The message to be logged, containing details about the interaction.</param>
    /// <returns>A completed task representing the asynchronous operation.</returns>
    private Task Log(LogMessage logMessage)
    {
        _logger.LogInformation("Interaction created: {Message}", [logMessage.ToString()]);
        return Task.CompletedTask;  
    }

    /// <summary>
    /// Handles incoming interactions by executing the associated command.
    /// </summary>
    /// <param name="interaction"></param>
    /// <returns></returns>
    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the incoming command.
            var result = await _handler.ExecuteCommandAsync(context, _services).ConfigureAwait(false);

            // Due to async nature of InteractionFramework, the result here may always be success.
            // That's why we also need to handle the InteractionExecuted event.
            if (!result.IsSuccess)
                _logger.LogError("Command execution failed: from {User} - {Error}", interaction.User, result.ErrorReason);
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
            // response, or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    /// <summary>
    /// Logs the result of an interaction command execution.
    /// </summary>
    /// <param name="commandInfo"></param>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private Task HandleInteractionExecuted(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
            _logger.LogError("Command execution failed: {Command} - {Error}", commandInfo.Name, result.ErrorReason);
        if (result.IsSuccess)
            _logger.LogInformation("Command executed successfully: {Command} by {User}", commandInfo.Name, context.User.Username);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleanups resources and unsubscribes from events to prevent memory leaks.
    /// </summary>
    /// <returns></returns>
    public async Task Dispose()
    {
        // Unsubscribe from events to prevent memory leaks
        _client.Ready -= ReadyAsync;
        _handler.Log -= Log;
        _client.InteractionCreated -= HandleInteraction;
        _handler.InteractionExecuted -= HandleInteractionExecuted;

        await _handler.RemoveModuleAsync<InteractionModuleBase>().ConfigureAwait(false);

        _logger.LogInformation("InteractionHandler disposed.");
    }

}
