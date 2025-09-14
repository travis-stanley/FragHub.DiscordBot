using Discord;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Abstractions;
using FragHub.Domain.Music.Entities;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FragHub.DiscordAdapter.Music.Players;


public class LavalinkPlayerSettings : IPlayerSettings
{
    public float Volume { get; set; }
    public Application.Music.Abstractions.PlayerState PlayerState { get; set; }
    public bool IsShuffling { get; set; }
}
