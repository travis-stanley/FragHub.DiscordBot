using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

public enum PlayerState
{
    Active,
    Stopped,
    Playing,
    Paused,
    Inactive
}

public interface IPlayerSettings
{
    float Volume { get; set; }

    PlayerState PlayerState { get; set; }
    bool IsShuffling { get; set; }          
}
