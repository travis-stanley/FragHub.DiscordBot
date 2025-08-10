using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;

namespace FragHub.Application.Music.Commands;

public class PlayTrackCommand : Command
{
    public string Query { get; set; } = string.Empty;
}
