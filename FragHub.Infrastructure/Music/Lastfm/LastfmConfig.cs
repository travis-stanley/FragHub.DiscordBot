using FragHub.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Infrastructure.Music.Lastfm
{
    public class LastfmConfig : IEnvConfig
    {
        public const string ApiToken = "LASTFM_TOKEN";
    }
}
