// Host/Program.cs
using DotNetEnv;
using FragHub.DiscordAdapter.Bot;
using FragHub.Domain.Users.Entities;
using FragHub.Host;
using FragHub.Host.Extensions;
using FragHub.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var useInMemoryDb = false;

var builder = Host.CreateApplicationBuilder(args);

// Load environment variables from .env file(s) -- see .env
Env.Load();
Env.TraversePath().Load();

builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Information));

builder.Services.AddVariableService();
builder.Services.AddCommandServices();

builder.Services.AddLavalinkServices();

builder.Services.AddMusicServices();
builder.Services.AddUserServices();

builder.Services.AddDiscordServices();

builder.Services.AddDataRepositories();

if (useInMemoryDb)
    builder.Services.AddInMemoryServerContext();
else
    builder.Services.AddSqlServerContext();

builder.Services.AddHostedService<DiscordBot>();    // main service

var app = builder.Build();

if (useInMemoryDb) { InMemoryDatabase.Seed(app); } // seed in-memory database with default data

app.Run();
