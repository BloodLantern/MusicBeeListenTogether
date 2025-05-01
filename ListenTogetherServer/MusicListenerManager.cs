namespace ListenTogetherServer;

public static class MusicListenerManager
{
    public static List<MusicListener> Listeners { get; } = [];

    private static readonly List<ListeningQueue> ListeningQueues = [];

    public static void AddListener(MusicListener newListener)
    {
        lock (Listeners)
            Listeners.Add(newListener);
    }

    public static bool HasListener(string username)
    {
        lock (Listeners)
            return Listeners.Exists(l => l.Username == username);
    }

    public static bool HasListener(Guid id)
    {
        lock (Listeners)
            return Listeners.Exists(l => l.Id == id);
    }

    public static MusicListener GetListener(Guid id)
    {
        lock (Listeners)
            return Listeners.Find(l => l.Id == id);
    }

    public static void RemoveInactiveListeners()
    {
        lock (Listeners)
            Listeners.RemoveAll(l => l.IsInactive);
    }

    public static void RemoveListener(Guid id)
    {
        lock (Listeners)
            Listeners.RemoveAll(l => l.Id == id);
    }
}
