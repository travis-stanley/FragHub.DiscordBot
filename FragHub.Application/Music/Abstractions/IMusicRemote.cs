using FragHub.Application.Abstractions;
using FragHub.Domain.Music.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Application.Music.Abstractions
{

    /// <summary>
    /// Defines the contract for a remote control interface for music playback.
    /// Intermidiary between music player and UI implementations.
    /// </summary>
    public interface IMusicRemote
    {
        string GuildId { get; }

        IPlayerSettings PlayerSettings { get; set; }

        /// <summary>
        /// The music player notifies the remote that a track has started playing.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// In the discord adapter, this would typically trigger an update to the music embed message to reflect the new track information.
        /// </remarks>
        Task OnTrackStarted();

        /// <summary>
        /// After an interaction is handled the remote may need to update its state.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// In the discord adapter, this would typically trigger an update to the music embed components to reflect the new state after a user interaction.
        /// </remarks>
        Task OnInteractionHandled();

        /// <summary>
        /// The music player notifies the remote it is being tracked for inactivity.
        /// </summary>
        /// <returns></returns>
        Task OnPlayerTracked();

        /// <summary>
        /// Update the remote about a change in the player's state (active/inactive).
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        Task OnStateChanged(PlayerState state);
    }
}
