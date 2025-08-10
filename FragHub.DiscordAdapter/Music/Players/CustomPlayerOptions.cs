using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Lavalink4NET.Players.Queued;

namespace FragHub.Discord.Music.Players;

public sealed record class CustomPlayerOptions() : QueuedLavalinkPlayerOptions; //ITextChannel? TextChannel -- needed?
