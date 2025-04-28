using System.Diagnostics.CodeAnalysis;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
// ReSharper disable UnusedMember.Global

namespace ListenTogetherServer
{
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public class MusicBeeController : WebApiController
    {
        [Route(HttpVerbs.Get, "/connect")]
        public Guid Connect([QueryField] string username)
        {
            // Check if the listener is already connected
            if (MusicListenerManager.HasListener(username))
                throw HttpException.BadRequest("User is already connected");
            
            MusicListener newListener = new(username);
            MusicListenerManager.AddListener(newListener);
            return newListener.Id;
        }

        [Route(HttpVerbs.Post, "/disconnect")]
        public void Disconnect([QueryField] Guid id)
        {
            CheckListenerConnected(id);

            MusicListenerManager.RemoveListener(id);
        }

        [Route(HttpVerbs.Post, "/listeners/updatePlayingTrack")]
        public void ListenersUpdatePlayingTrack(
            [QueryField] Guid id,
            [QueryField] string trackTitle,
            [QueryField] string trackArtist,
            [QueryField] string trackAlbum
        )
        {
            CheckListenerConnected(id);

            MusicListener listener = MusicListenerManager.GetListener(id);
            listener.ListeningState = new()
            {
                TrackTitle = trackTitle,
                TrackArtist = trackArtist,
                TrackAlbum = trackAlbum
            };
            listener.SetActiveNow();
        }

        private static void CheckListenerConnected(Guid id)
        {
            if (!MusicListenerManager.HasListener(id))
                throw HttpException.BadRequest("User isn't connected");
        }
    }
}
