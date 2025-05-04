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

            MusicListener listener = MusicListenerManager.GetListener(id);
            if (listener.CurrentQueueOwner == null)
            {
                ListeningQueue queue = MusicListenerManager.GetListeningQueue(listener.Username, true);
                MusicListenerManager.RemoveQueue(queue);
            }
            else
            {
                ListeningQueue queue = MusicListenerManager.GetListeningQueue(listener.Username, false);
                queue?.Listeners.Remove(listener);
            }

            MusicListenerManager.RemoveListener(id);
        }

        [Route(HttpVerbs.Post, ServerApi.RequestListenersUpdateActivity)]
        [UsedImplicitly]
        public void ListenersUpdateListeningActivity([QueryField] Guid id, [JsonData] ListeningState state)
        {
            CheckListenerConnected(id);

            MusicListener listener = MusicListenerManager.GetListener(id);
            listener.ListeningState = state;
            listener.SetActiveNow();
        }

        [Route(HttpVerbs.Post, ServerApi.RequestListenersClearActivity)]
        [UsedImplicitly]
        public void ListenersClearListeningActivity([QueryField] Guid id)
        {
            CheckListenerConnected(id);

            MusicListener listener = MusicListenerManager.GetListener(id);
            listener.ListeningState = default;
            listener.SetActiveNow();
        }

        [Route(HttpVerbs.Get, ServerApi.RequestListenersStates)]
        [UsedImplicitly]
        public string ListenersStates()
        {
            ListenerSharedState[] result = new ListenerSharedState[MusicListenerManager.ListenerCount];

            for (int i = 0; i < MusicListenerManager.ListenerCount; i++)
            {
                MusicListener listener = MusicListenerManager.GetListener(i);
                result[i] = new()
                {
                    Username = listener.Username,
                    State = listener.ListeningState,
                    QueueOwner = listener.CurrentQueueOwner?.Username
                };
            }

            return result.ToJson();
        }

        [Route(HttpVerbs.Post, ServerApi.RequestListenersJoinQueue)]
        [UsedImplicitly]
        public void ListenersJoinQueue([QueryField] Guid id, [QueryField] string username)
        {
            CheckListenerConnected(id);

            MusicListener listener = MusicListenerManager.GetListener(id);
            MusicListener newQueueOwner = MusicListenerManager.GetListener(username);
            listener.CurrentQueueOwner = newQueueOwner;

            ListeningQueue newQueue = new(newQueueOwner, listener);
            MusicListenerManager.ListeningQueues.Add(newQueue);
        }

        [Route(HttpVerbs.Post, ServerApi.RequestListenersLeaveQueue)]
        [UsedImplicitly]
        public void ListenersLeaveQueue([QueryField] Guid id)
        {
            CheckListenerConnected(id);

            MusicListener listener = MusicListenerManager.GetListener(id);
            
            if (listener.CurrentQueueOwner == null)
                throw HttpException.BadRequest("User is not in a queue");

            listener.CurrentQueueOwner = null;

            ListeningQueue queue = MusicListenerManager.GetListeningQueue(listener.Username, false);
            queue.Listeners.Remove(listener);
        }

        private static void CheckListenerConnected(Guid id)
        {
            if (!MusicListenerManager.HasListener(id))
                throw HttpException.BadRequest("User isn't connected");
        }
    }
}
