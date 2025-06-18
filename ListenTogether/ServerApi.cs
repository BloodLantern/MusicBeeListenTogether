using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Swan.Formatters;

namespace MusicBeePlugin;

/// <summary>
/// The client-side REST API. This is where all the requests are made.
/// </summary>
public class ServerApi
{
#if DEBUG
    public const string ServerEndpoint = "localhost";
#else
    public const string ServerEndpoint = "172.27.211.161";
#endif
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

    public const int AutoRefreshTime = 3000;

    public event Action OnPreConnect;
    public event Action OnPostConnect;
    public event Action OnPreDisconnect;
    public event Action OnPostDisconnect;
    public event Action OnPreUpdatePlayingTrack;
    public event Action OnPostUpdatePlayingTrack;
    public event Action OnPreClearPlayingTrack;
    public event Action OnPostClearPlayingTrack;
    public event Action OnPreUpdateListenerStates;
    public event Action OnPostUpdateListenerStates;
    public event Action OnPreJoinListeningQueue;
    public event Action OnPostJoinListeningQueue;
    public event Action OnPreLeaveListeningQueue;
    public event Action OnPostLeaveListeningQueue;

    private HttpClient Client { get; set; }

    private Plugin Plugin { get; }

    private Guid id;

    private string IdParameter => $"id={id}";

    public bool Connected => id != Guid.Empty;

    public ListenerSharedState[] ListenerSharedStates { get; private set; } = [];

    private DateTime lastListenerSharedStatesUpdate;

    public ListenerSharedState LocalSharedState { get; private set; }

    public readonly string LocalUsername = Environment.UserName;

    private DateTime lastSuccessfulRequest;

    public bool InQueue { get; private set; }

    public ServerApi(Plugin plugin)
    {
        Plugin = plugin;

        /*string localGitUsername = GitCommands.GetLocalConfigUsername();
        if (localGitUsername != null)
            LocalUsername = localGitUsername;*/
    }

    public async Task<bool> Connect()
    {
        OnPreConnect?.Invoke();

        if (Client == null)
        {
            Client = new();
            Client.BaseAddress = ServerUri;
        }

        if (!await MakeGetRequestString(RequestConnect, $"username={LocalUsername}", result => id = Guid.Parse(result)))
            return false;

        OnPostConnect?.Invoke();
        return true;
    }

    public async Task Disconnect()
    {
        OnPreDisconnect?.Invoke();

        await MakePostRequest(RequestDisconnect, IdParameter);

        id = Guid.Empty;
        Client.Dispose();
        Client = null;

        ListenerSharedStates = [];
        LocalSharedState = default;

        OnPostDisconnect?.Invoke();
    }

    public async Task<bool> UpdatePlayingTrack()
    {
        OnPreUpdatePlayingTrack?.Invoke();

        ObjectContent<ListeningState> content = new(Plugin.GetListeningState(), new JsonMediaTypeFormatter());
        if (!await MakePostRequest(RequestListenersUpdateActivity, IdParameter, content))
            return false;

        OnPostUpdatePlayingTrack?.Invoke();

        return true;
    }

    public async Task<bool> ClearPlayingTrack()
    {
        OnPreClearPlayingTrack?.Invoke();

        if (!await MakePostRequest(RequestListenersClearActivity, IdParameter))
            return false;

        OnPostClearPlayingTrack?.Invoke();

        return true;
    }

    public async Task<bool> UpdateListenerStates(bool force = false)
    {
        OnPreUpdateListenerStates?.Invoke();

        // Avoid sending too many requests
        if (!force && (DateTime.Now - lastListenerSharedStatesUpdate).TotalSeconds < AutoRefreshTime * 0.001)
        {
            OnPostUpdateListenerStates?.Invoke();
            return true;
        }

        if (!await MakeGetRequest<ListenerSharedState[]>(RequestListenersStates, null, result => ListenerSharedStates = result))
            return false;

        lastListenerSharedStatesUpdate = DateTime.Now;

        LocalSharedState = ListenerSharedStates.First(s => s.Username == LocalUsername);

        if (!InQueue)
        {
            OnPostUpdateListenerStates?.Invoke();
            return true;
        }

        ListenerSharedState queueOwnerSharedState = ListenerSharedStates.First(s => s.Username == LocalSharedState.QueueOwner);
        ListeningState queueOwnerState = queueOwnerSharedState.State;
        if (!queueOwnerState.IsIdle())
            Plugin.SetListeningState(queueOwnerState);

        OnPostUpdateListenerStates?.Invoke();
        return true;
    }

    public async Task<bool> JoinListeningQueue(string username)
    {
        OnPreJoinListeningQueue?.Invoke();

        InQueue = await MakePostRequest(RequestListenersJoinQueue, $"{IdParameter}&username={username}");

        if (!InQueue)
            return false;

        OnPostJoinListeningQueue?.Invoke();

        return await UpdateListenerStates(true);
    }

    public async Task LeaveListeningQueue()
    {
        OnPreLeaveListeningQueue?.Invoke();

        InQueue = false;
        await MakePostRequest(RequestListenersLeaveQueue, IdParameter);

        if (Plugin.GetListeningState().IsIdle())
            await ClearPlayingTrack();
        else
            await UpdatePlayingTrack();

        OnPostLeaveListeningQueue?.Invoke();

        await UpdateListenerStates(true);
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

            lastSuccessfulRequest = DateTime.Now;
            return true;
        }
        catch (HttpRequestException e)
        {
            onException?.Invoke(e);
            await CheckDisconnected();
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

            lastSuccessfulRequest = DateTime.Now;
            return true;
        }
        catch (HttpRequestException e)
        {
            onException?.Invoke(e);
            if (request != RequestDisconnect)
                await CheckDisconnected();
            return false;
        }
        finally
        {
            onFinally?.Invoke();
        }
    }

    private async Task CheckDisconnected()
    {
        // If the last successful request was more than 10 seconds ago, assume the server can't be reached and auto-disconnect ourselves
        if ((DateTime.Now - lastSuccessfulRequest).TotalSeconds > 10.0)
            await Disconnect();
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
