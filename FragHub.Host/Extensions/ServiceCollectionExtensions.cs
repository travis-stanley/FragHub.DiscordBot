using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FragHub.Application;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Discord.Bot;
using FragHub.Discord.Music.Players;
using FragHub.Discord.Music.Tracks;
using FragHub.DiscordAdapter.Config;
using FragHub.Infrastructure.Env;
using DiscordConfig = FragHub.DiscordAdapter.Config.DiscordConfig;
using Microsoft.Extensions.Logging;

namespace FragHub.Host.Extensions;


/// <summary>
/// Provides extension methods for registering services in an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>This static class contains methods to simplify the registration of various services, including
/// generic handlers, variable services, audio-related services, Discord-related services, and command-related services.
/// These methods are designed to streamline dependency injection setup for applications using the .NET dependency
/// injection framework.</remarks>
public static class ServiceCollectionExtensions
{

    /// <summary>
    /// Registers implementations of a specified open generic interface from the provided assembly into the service
    /// collection.
    /// </summary>
    /// <remarks>This method scans the specified assembly for concrete types that implement the given open
    /// generic interface. It registers each implementation with the specified service lifetime (transient or scoped).
    /// If an unsupported lifetime is provided, an <see cref="ArgumentException"/> is thrown.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the handlers will be added.</param>
    /// <param name="assembly">The assembly to scan for types implementing the specified open generic interface.</param>
    /// <param name="openGenericInterface">The open generic interface type to register implementations for.</param>
    /// <param name="lifetime">The <see cref="ServiceLifetime"/> to use when registering the handlers.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="lifetime"/> is not <see cref="ServiceLifetime.Transient"/> or <see
    /// cref="ServiceLifetime.Scoped"/>.</exception>
    public static IServiceCollection AddGenericHandlers(this IServiceCollection services, Assembly assembly, Type openGenericInterface, ServiceLifetime lifetime)
    {
        var handlers = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t =>
                t.GetInterfaces()
                 .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                 .Select(i => new { Interface = i, Implementation = t }))
            .ToList();

        if (lifetime == ServiceLifetime.Transient)
            handlers.ForEach(handler => { services.AddTransient(handler.Interface, handler.Implementation); });
        else if (lifetime == ServiceLifetime.Scoped)
            handlers.ForEach(handler => { services.AddScoped(handler.Interface, handler.Implementation); });
        else
            throw new ArgumentException($"Unsupported lifetime: {lifetime}");

        return services;
    }

    /// <summary>
    /// Adds the <see cref="IVariableService"/> implementation to the specified <see cref="IServiceCollection"/>.   
    /// </summary>
    /// <remarks>This method registers <see cref="IVariableService"/> as a singleton service in the dependency
    /// injection container. The implementation of <see cref="IVariableService"/> is initialized with a <see
    /// cref="RequiredVariables"/> instance.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the service will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddVariableService(this IServiceCollection services)
    {
        object[] vars = [new LavalinkConfig(), new DiscordConfig()];
        services.AddSingleton<IVariableService>(sp => new VariableService(sp.GetRequiredService<ILogger<IVariableService>>(), vars));

        return services;
    }

    /// <summary>
    /// Adds and configures services required for bot audio functionality, including Lavalink and inactivity tracking.
    /// </summary>
    /// <remarks>This method configures Lavalink for audio streaming and inactivity tracking for managing
    /// audio-related timeouts. It retrieves required configuration values from an <see cref="IVariableService"/>
    /// implementation.  Lavalink settings such as host, port, passphrase, and label are configured based on environment
    /// variables. Inactivity tracking is set up with predefined timeout behaviors and tracking modes.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the audio services will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddLavalinkServices(this IServiceCollection services)
    {
        var variableService = services.BuildServiceProvider().GetRequiredService<IVariableService>();

        services.AddLavalink();
        services.AddInactivityTracking();

        services.ConfigureLavalink(config =>
        {
            config.BaseAddress = new Uri($"http://{variableService.GetVariable(LavalinkConfig.Host)}:{variableService.GetVariable(LavalinkConfig.Port)}/");
            config.Passphrase = variableService.GetVariable(LavalinkConfig.Password)!;
            config.Label = variableService.GetVariable(LavalinkConfig.Label)!;
            config.HttpClientName = variableService.GetVariable(LavalinkConfig.Label)!;
        });

        services.ConfigureInactivityTracking(options =>
        {
            options.TimeoutBehavior = InactivityTrackingTimeoutBehavior.Highest;
            options.TrackingMode = InactivityTrackingMode.All;
        });

        services.Configure<IdleInactivityTrackerOptions>(config =>
        {
            config.Timeout = TimeSpan.FromMinutes(5);
            config.InitialTimeout = TimeSpan.FromMinutes(5);
        });

        services.Configure<UsersInactivityTrackerOptions>(config =>
        {
            config.Timeout = TimeSpan.FromMinutes(1);
        });

        return services;
    }

    /// <summary>
    /// Adds music-related services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>This method registers the following services as singletons: <list type="bullet">
    /// <item><description><see cref="IPlayerService"/> implemented by <see cref="PlayerService"/>.</description></item>
    /// <item><description><see cref="ISearchService"/> implemented by <see cref="SearchService"/>.</description></item>
    /// </list> Use this method to configure music-related functionality in your application's dependency injection
    /// container.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the services will be added.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance with the music services registered.</returns>
    public static IServiceCollection AddMusicServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<ITrackService, TrackService>();

        return services;
    }

    /// <summary>
    /// Adds Discord-related services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>This method registers the <see cref="DiscordSocketClient"/> and <see
    /// cref="InteractionService"/> as singleton services. The <see cref="DiscordSocketClient"/> is configured with all
    /// gateway intents enabled. The <see cref="InteractionService"/> is initialized with asynchronous run mode,
    /// informational log level, and compiled lambda usage.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the Discord services will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddDiscordServices(this IServiceCollection services)
    {
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All }));
        services.AddSingleton<InteractionServiceConfig>(_ => new()
        {
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Info,
            UseCompiledLambda = true
        });

        services.AddSingleton<InteractionService>();
        services.AddSingleton<InteractionHandler>();

        return services;
    }

    /// <summary>
    /// Registers command-related services in the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>This method adds generic command handlers and a singleton instance of <see
    /// cref="CommandDispatcher"/> to the service collection. Command handlers are registered with a transient
    /// lifetime.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the command services will be added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddCommandServices(this IServiceCollection services)
    {
        services.AddGenericHandlers(typeof(ICommand).Assembly, typeof(ICommandHandler<>), ServiceLifetime.Transient);
        services.AddSingleton<CommandDispatcher>();

        return services;
    }

    /// <summary>
    /// Adds a hosted Discord bot service to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>This method registers the <see cref="DiscordBot"/> as a hosted service, enabling it to run  
    /// as part of the application's background services. The bot will be managed by the application's   hosting
    /// infrastructure.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the hosted Discord bot service is added.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddHostedDiscordBot(this IServiceCollection services)
    {
        services.AddHostedService<DiscordBot>();
        
        return services;
    }

}
