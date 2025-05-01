using System;
using System.Diagnostics.Contracts;

namespace MusicBeePlugin
{
    public struct ListeningState
    {
        public string TrackTitle;

        public string TrackArtists;

        public string TrackAlbum;

        public DateTime Time;

        [Pure]
        public bool IsIdle() => TrackTitle == null;
    }
}
