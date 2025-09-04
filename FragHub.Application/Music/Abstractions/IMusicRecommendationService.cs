using FragHub.Domain.Music.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Application.Music.Abstractions
{
    public interface IMusicRecommendationService
    {
        Task<IEnumerable<Track>> GetRecommendations(int limit, Track seed, Track[]? tracksToOmit);
    }
}
