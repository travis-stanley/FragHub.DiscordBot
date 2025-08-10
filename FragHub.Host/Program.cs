// Host/Program.cs
using DotNetEnv;
using FragHub.DiscordAdapter.Bot;
using FragHub.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


var builder = Host.CreateApplicationBuilder(args);

Env.Load();
Env.TraversePath().Load();

builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Information));

builder.Services.AddVariableService();
builder.Services.AddCommandServices();

builder.Services.AddLavalinkServices();

builder.Services.AddMusicServices();

builder.Services.AddDiscordServices();

builder.Services.AddHostedService<DiscordBot>();    // main service

builder.Build().Run();
