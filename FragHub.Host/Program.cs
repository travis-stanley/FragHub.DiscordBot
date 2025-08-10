// Host/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FragHub.Host.Extensions;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Information));

builder.Services.AddVariableService();
builder.Services.AddCommandServices();

builder.Services.AddLavalinkServices();

builder.Services.AddMusicServices();

builder.Services.AddDiscordServices();
builder.Services.AddHostedDiscordBot();

builder.Build().Run();
