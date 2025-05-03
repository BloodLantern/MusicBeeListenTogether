namespace ListenTogetherServer;

public class ListeningQueue
{
    public MusicListener Owner;
    
    public List<MusicListener> Listeners;

    public ListeningQueue(MusicListener owner, params MusicListener[] listeners)
    {
        Owner = owner;
        Listeners = new(listeners);
    }
}
