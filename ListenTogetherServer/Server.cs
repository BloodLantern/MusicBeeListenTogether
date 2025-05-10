using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using MusicBeePlugin;
using Swan.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ListenTogetherServer;

public class Server : BackgroundService
{
    private WebServer server;

    public ILogger<Server> Logger { get; }

    public Server(ILogger<Server> logger)
    {
        Logger = logger;
    }

    public void SetupServer()
    {
        server = CreateWebServer(ServerApi.MakeServerUri().ToString());
        server.RunAsync();
    }

    public void StopServer()
    {
        server.Dispose();
    }

    // Create and configure our web server.
    private WebServer CreateWebServer(string url)
    {
        WebServer webServer = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
            .WithLocalSessionManager()
            .WithWebApi("/musicbee", m => m.WithController<MusicBeeController>())
            .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

        // Listen for state changes.
        webServer.StateChanged += (_, e) =>
        {
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("WebServer New State - {WebServerState}", e.NewState);
        };
            
        return webServer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SetupServer();
        
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
        
        StopServer();
    }
}