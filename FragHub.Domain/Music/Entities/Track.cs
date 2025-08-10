using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Domain.Music.Entities;

public class Track
{
    public string? Title { get; set; }
    public Uri? Uri { get; set; }
}