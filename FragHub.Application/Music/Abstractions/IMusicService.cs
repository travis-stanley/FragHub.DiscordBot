using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Application.Music.Commands;
using FragHub.Domain.Music.Entities;

namespace FragHub.Application.Music.Abstractions;

public interface IMusicService
{   
    IEnumerable<T> GetRemotes<T>(string playerId) where T : IMusicRemote;
    IEnumerable<T> AddRemote<T>(string playerId, T remote) where T : IMusicRemote;

    Track[] GetTracks(string playerId);
    Queue<Track> GetQueuedTracks(string playerId);

    IEnumerable<ICommand> GetCommands(string playerId);
    void AddCommand(string playerId, ICommand command);

    Track[] GetRecommendations(string playerId);

    Task PlayAsync(PlayTrackCommand command);
    Task StopAsync(StopTrackCommand command);
    Task SkipAsync(SkipTrackCommand command);
    Task PauseAsync(PauseTrackCommand command);
    Task ResumeAsync(ResumeTrackCommand command);
    Task SetShuffleAsync(ShuffleTracksCommand command);
    Task MoveToTopOfQueueAsync(MoveToTopOfQueueCommand command);
    Task AddRecommendationAsync(AddRecommendationCommand command);    

    Task NotifyPlayerTracked(string id);
    Task NotifyStateChanged(string id, PlayerState state);
    Task NotifyTrackStarted(string id);
    Task NotifyInteractionHandled(string id);
    
    
}
