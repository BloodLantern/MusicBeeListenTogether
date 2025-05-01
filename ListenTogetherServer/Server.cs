using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using Swan.Logging;

namespace ListenTogetherServer
{
    public class Server
    {
        private WebServer server;

        public void SetupServer(string serverUri)
        {
            server = CreateWebServer(serverUri);
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
            webServer.StateChanged += (_, e) => $"WebServer New State - {e.NewState}".Info();
            
            return webServer;
        }
    }
}
