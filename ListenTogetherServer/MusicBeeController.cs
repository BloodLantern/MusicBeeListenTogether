using System.Diagnostics.CodeAnalysis;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using JetBrains.Annotations;
using MusicBeePlugin;
using Swan;

namespace ListenTogetherServer
{
    /// <summary>
    /// The server REST <c>/musicbee</c> API. This is where all <c>/musicbee</c> requests are handled.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public class MusicBeeController : WebApiController
    {
        [Route(HttpVerbs.Get, ServerApi.RequestConnect)]
        [UsedImplicitly]
        public Guid Connect([QueryField] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw HttpException.BadRequest("Empty username");

            username = username.Trim();
            
            // Check if the listener is already connected
            if (MusicListenerManager.HasListener(username))
                throw HttpException.BadRequest("User is already connected");
            
            MusicListener newListener = new(username);
            MusicListenerManager.AddListener(newListener);
            return newListener.Id;
        }

        [Route(HttpVerbs.Post, ServerApi.RequestDisconnect)]
        [UsedImplicitly]
        public void Disconnect([QueryField] Guid id)
        {
            CheckListenerConnected(id);

            MusicListenerManager.RemoveListener(id);
        }

        [Route(HttpVerbs.Post, ServerApi.RequestListenersUpdateListeningActivity)]
        [UsedImplicitly]
        public void ListenersUpdateListeningActivity([QueryField] Guid id, [JsonData] ListeningState state)
        {
            CheckListenerConnected(id);

            MusicListener listener = MusicListenerManager.GetListener(id);
            listener.ListeningState = state;
            listener.SetActiveNow();
        }

        [Route(HttpVerbs.Get, ServerApi.RequestListenersStates)]
        [UsedImplicitly]
        public string ListenersStates()
        {
            ListenerSharedState[] result = new ListenerSharedState[MusicListenerManager.Listeners.Count];

            for (int i = 0; i < MusicListenerManager.Listeners.Count; i++)
            {
                MusicListener listener = MusicListenerManager.Listeners[i];
                result[i] = new()
                {
                    Username = listener.Username,
                    State = listener.ListeningState,
                    QueueOwner = listener.CurrentQueueOwner?.Username
                };
            }

            return result.ToJson();
        }

        private static void CheckListenerConnected(Guid id)
        {
            if (!MusicListenerManager.HasListener(id))
                throw HttpException.BadRequest("User isn't connected");
        }
    }
}
