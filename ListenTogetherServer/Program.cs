using ListenTogetherServer;

Console.WriteLine("Hello, World!");

Server server = new();
server.SetupServer();

Timer removeInactiveUsersTimer = new(
    _ => MusicListenerManager.RemoveInactiveListeners(),
    null,
    MusicListener.InactiveTime,
    MusicListener.InactiveTime
);

_ = Console.ReadLine();

removeInactiveUsersTimer.Dispose();

server.StopServer();
