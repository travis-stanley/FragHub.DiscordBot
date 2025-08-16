using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

public interface IPlayerHistory
{
    IEnumerable<Track> Tracks { get; }
}
