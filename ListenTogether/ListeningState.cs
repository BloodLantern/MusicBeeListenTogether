using System;
using System.Diagnostics.Contracts;

namespace MusicBeePlugin
{
    public struct ListeningState
    {
        public string TrackTitle;

        public string TrackArtists;

        public string TrackAlbum;

        public int Position;

        public DateTime Time;

        [Pure]
        public bool IsIdle() => TrackTitle == null;

        [Pure]
        public bool IsDifferentTrackFrom(ListeningState otherState) => TrackTitle != otherState.TrackTitle ||
                                                                       TrackArtists != otherState.TrackArtists ||
                                                                       TrackAlbum != otherState.TrackAlbum;
    }
}
