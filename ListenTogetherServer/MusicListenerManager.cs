using System.Collections.Concurrent;

namespace ListenTogetherServer;

public static class MusicListenerManager
{
    private static readonly ConcurrentBag<MusicListener> Listeners = [];

    private static readonly List<ListeningQueue> ListeningQueues = [];

    public static void AddListener(MusicListener newListener) => Listeners.Add(newListener);

    public static bool HasListener(string username) => Listeners.Exists(l => l.Username == username);

    public static bool HasListener(Guid id) => Listeners.Exists(l => l.Id == id);

    public static MusicListener GetListener(Guid id) => Listeners.Find(l => l.Id == id);

    public static void RemoveInactiveListeners() => Listeners.RemoveAll(l => l.IsInactive);

    public static void RemoveListener(Guid id) => Listeners.RemoveAll(l => l.Id == id);
}
