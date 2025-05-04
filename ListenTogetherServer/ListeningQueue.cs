namespace ListenTogetherServer;

public class ListeningQueue
{
    public readonly MusicListener Owner;
    
    public readonly List<MusicListener> Listeners;

    public ListeningQueue(MusicListener owner, params MusicListener[] listeners)
    {
        Owner = owner;
        Listeners = new(listeners);
    }

    public bool HasListener(string username) => Listeners.Exists(l => l.Username == username);

    public void RemoveListener(string username) => Listeners.RemoveAll(l => l.Username == username);
}
