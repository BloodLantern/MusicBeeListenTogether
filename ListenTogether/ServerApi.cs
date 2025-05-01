using System;
using System.Diagnostics.Contracts;
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
        public const string ServerEndpoint = "localhost"; // 172.27.66.211
        public const uint ServerPort = 9696;
        public static readonly Uri ServerUri = MakeServerUri();
        public static Uri MakeServerUri(string endpoint = ServerEndpoint, uint port = ServerPort) => new($"http://{endpoint}:{port}/");

        public const string RequestConnect = "/connect";
        public const string RequestDisconnect = "/disconnect";
        public const string RequestListenersUpdateListeningActivity = "/listeners/updateListeningActivity";
        public const string RequestListenersStates = "/listeners/states";

        private HttpClient Client { get; } = new();
        
        private Plugin Plugin { get; }

        private Guid id;

        private string IdParameter => $"id={id}";

        public bool Connected => id != Guid.Empty;
        
        public ListenerSharedState[] ListenerSharedStates;
        
        public ServerApi(Plugin plugin)
        {
            Plugin = plugin;
            
            Client.BaseAddress = ServerUri;
        }

        public async Task<bool> Connect()
            => await MakeGetRequest<string>(RequestConnect, $"username={Environment.UserName}", result => id = Guid.Parse(result));

        public async Task<bool> Disconnect()
            => await MakePostRequest(RequestDisconnect, IdParameter, null, () => id = Guid.Empty, null, () => Client.Dispose());

        public async Task<bool> UpdatePlayingTrack()
        {
            ObjectContent<ListeningState> content = new(Plugin.GetListeningState(), new JsonMediaTypeFormatter());
            return await MakePostRequest(RequestListenersUpdateListeningActivity, IdParameter, content);
        }

        public async Task<bool> UpdateListenerStates()
            => await MakeGetRequest<string>(RequestConnect, null, result => ListenerSharedStates = Json.Deserialize<ListenerSharedState[]>(result));

        private async Task<bool> MakeGetRequest<T>(
            string request,
            string parameters,
            Action<T> onSuccess,
            Action<HttpRequestException> onException = null,
            Action onFinally = null
        )
        {
            try
            {
                HttpResponseMessage response = await Client.GetAsync(MakeUrl(request, parameters));

                if (!response.IsSuccessStatusCode)
                    return false;

                onSuccess?.Invoke(await response.Content.ReadAsAsync<T>());

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
            string url = request;
            if (!string.IsNullOrWhiteSpace(parameters))
                url += '?' + parameters.Trim();
            return url;
        }
    }
}
