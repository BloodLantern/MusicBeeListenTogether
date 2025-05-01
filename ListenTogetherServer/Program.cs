using MusicBeePlugin;

namespace ListenTogetherServer;

internal static class Program
{
    public static void Main(string[] args)
    {
        Server server = new();
        
        Uri serverUri = ServerApi.ServerUri;
        if (args.Length > 0)
            serverUri = ServerApi.MakeServerUri(args[0]);
        server.SetupServer(serverUri.ToString());

        Timer removeInactiveUsersTimer = new(
            _ => MusicListenerManager.RemoveInactiveListeners(),
            null,
            MusicListener.InactiveTime,
            MusicListener.InactiveTime
        );

        _ = Console.ReadLine();

        removeInactiveUsersTimer.Dispose();

        server.StopServer();
    }
}