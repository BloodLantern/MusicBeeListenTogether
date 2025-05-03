namespace ListenTogetherServer;

public static class MusicListenerManager
{
    /// <summary>
    /// The currently connected listeners list.
    /// This can be modified from a Timer thread to remove the inactive users, and therefore needs to be locked before access.
    /// </summary>
    public static readonly List<MusicListener> Listeners = [];

    public static readonly List<ListeningQueue> ListeningQueues = [];

    public static int ListenerCount { get { lock (Listeners) return Listeners.Count; } }

    public static void ForEachListener(Action<MusicListener> action)
    {
        lock (Listeners)
        {
            foreach (MusicListener listener in Listeners)
                action(listener);
        }
    }

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

    public static MusicListener GetListener(int index)
    {
        lock (Listeners)
            return Listeners[index];
    }

    public static MusicListener GetListener(string username)
    {
        lock (Listeners)
            return Listeners.Find(l => l.Username == username);
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
