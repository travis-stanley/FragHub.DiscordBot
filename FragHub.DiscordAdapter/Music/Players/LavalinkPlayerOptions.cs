using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using FragHub.Application.Music.Abstractions;
using Lavalink4NET.Players.Queued;

namespace FragHub.DiscordAdapter.Music.Players;

public sealed record class LavalinkPlayerOptions(IMusicService MusicService) : QueuedLavalinkPlayerOptions;
