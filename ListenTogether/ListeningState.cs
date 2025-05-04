using System;
using System.Diagnostics.Contracts;

namespace MusicBeePlugin
{
    public struct ListeningState
    {
        public string TrackTitle;

        public string TrackArtists;

        public string TrackAlbum;

        public string FileUrl;

        public int Position;

        public DateTime Time;

        [Pure]
        public bool IsIdle() => TrackTitle == null;
    }
}
