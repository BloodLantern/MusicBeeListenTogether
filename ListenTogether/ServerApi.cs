using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Swan.Formatters;

namespace MusicBeePlugin
{
    /// <summary>
    /// The client-side REST API. This is where all the requests are made.
    /// </summary>
    public class ServerApi
    {
        public const string ServerEndpoint = "172.27.66.211"; // Will be 172.27.66.211
        public const uint ServerPort = 9696;
        public static readonly Uri ServerUri = MakeServerUri();
        public static Uri MakeServerUri(string endpoint = ServerEndpoint, uint port = ServerPort) => new($"http://{endpoint}:{port}");

        public const string RequestConnect = "/connect";
        public const string RequestDisconnect = "/disconnect";
        public const string RequestListenersClearActivity = "/listeners/clearActivity";
        public const string RequestListenersUpdateActivity = "/listeners/updateActivity";
        public const string RequestListenersStates = "/listeners/states";
        public const string RequestListenersJoinQueue = "/listeners/joinQueue";
        public const string RequestListenersLeaveQueue = "/listeners/leaveQueue";

        public const int AutoRefreshTime = 5000;

        private HttpClient Client { get; } = new();
        
        private Plugin Plugin { get; }

        private Guid id;

        private string IdParameter => $"id={id}";

        public bool Connected => id != Guid.Empty;
        
        public ListenerSharedState[] ListenerSharedStates;

        private readonly string localUsername = Environment.UserName;

        public bool InQueue { get; private set; }
        
        public ServerApi(Plugin plugin)
        {
            Plugin = plugin;
            
            Client.BaseAddress = ServerUri;
        }

        public async Task<bool> Connect()
            => await MakeGetRequestString(RequestConnect, $"username={localUsername}", result => id = Guid.Parse(result));

        public async Task<bool> Disconnect()
            => await MakePostRequest(RequestDisconnect, IdParameter, null, () => id = Guid.Empty, null, () => Client.Dispose());

        public async Task<bool> UpdatePlayingTrack()
        {
            ObjectContent<ListeningState> content = new(Plugin.GetListeningState(), new JsonMediaTypeFormatter());
            return await MakePostRequest(RequestListenersUpdateActivity, IdParameter, content);
        }

        public async Task<bool> ClearPlayingTrack() => await MakePostRequest(RequestListenersClearActivity, IdParameter);

        public async Task<bool> UpdateListenerStates()
        {
            if (!await MakeGetRequest<ListenerSharedState[]>(RequestListenersStates, null, result => ListenerSharedStates = result))
                return false;

            if (!InQueue)
                return true;

            ListenerSharedState localState = ListenerSharedStates.First(s => s.Username == localUsername);
            ListenerSharedState queueOwnerState = ListenerSharedStates.First(s => s.Username == localState.QueueOwner);
            Plugin.SetListeningState(queueOwnerState.State);

            return true;
        }

        public async Task<bool> JoinListeningQueue(string username)
            => InQueue = await MakePostRequest(RequestListenersJoinQueue, $"{IdParameter}&username={username}");

        public async Task LeaveListeningQueue()
        {
            InQueue = false;
            await MakePostRequest(RequestListenersLeaveQueue, IdParameter);
        }

        private async Task<bool> MakeGetRequest<T>(
            string request,
            string parameters,
            Action<T> onSuccess,
            Action<HttpRequestException> onException = null,
            Action onFinally = null
        )
        {
            return await MakeGetRequestString(request, parameters, s => onSuccess(Json.Deserialize<T>(s)), onException, onFinally);
        }

        private async Task<bool> MakeGetRequestString(
            string request,
            string parameters,
            Action<string> onSuccess,
            Action<HttpRequestException> onException = null,
            Action onFinally = null
        )
        {
            try
            {
                HttpResponseMessage response = await Client.GetAsync(MakeUrl(request, parameters));

                if (!response.IsSuccessStatusCode)
                    return false;

                onSuccess((await response.Content.ReadAsStringAsync()).Trim(' ', '\t', '"'));

                return true;
            }
            catch (HttpRequestException e)
            {
                onException?.Invoke(e);
                return false;
            }
            finally
            {
                onFinally?.Invoke();
            }
        }

        private async Task<bool> MakePostRequest(
            string request,
            string parameters,
            HttpContent content = null,
            Action onSuccess = null,
            Action<HttpRequestException> onException = null,
            Action onFinally = null
        )
        {
            try
            {
                HttpResponseMessage response = await Client.PostAsync(MakeUrl(request, parameters), content);

                if (!response.IsSuccessStatusCode)
                    return false;

                onSuccess?.Invoke();

                return true;
            }
            catch (HttpRequestException e)
            {
                onException?.Invoke(e);
                return false;
            }
            finally
            {
                onFinally?.Invoke();
            }
        }

        [Pure]
        private static string MakeUrl(string request, string parameters)
        {
            string url = ServerUri + "/musicbee" + request;
            if (!string.IsNullOrWhiteSpace(parameters))
                url += '?' + parameters.Trim();
            return url;
        }
    }
}
