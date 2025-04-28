namespace ListenTogetherServer;

public class MusicListener
{
    public static readonly TimeSpan InactiveTime = new(1, 0, 0);
    
    public Guid Id { get; } = Guid.NewGuid();
    
    public string Username { get; }

    public DateTime LastRequestTime { get; private set; } = DateTime.Now;

    public ListeningState ListeningState;

    public bool IsInactive => DateTime.Now - LastRequestTime > InactiveTime;

    public MusicListener(string username) => Username = username;

    public void SetActiveNow() => LastRequestTime = DateTime.Now;
}
