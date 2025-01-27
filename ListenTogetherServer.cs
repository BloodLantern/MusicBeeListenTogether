using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using Swan.Logging;

namespace MusicBeePlugin
{
    public class ListenTogetherServer
    {
        private WebServer server;

        public void SetupServer()
        {
            string url = "http://localhost:9696/";

            // Our web server is disposable.
            server = CreateWebServer(url);

            // Once we've registered our modules and configured them, we call the RunAsync() method.
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
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithWebApi("/musicbee", m => m.WithController<MusicBeeController>())
                //.WithModule(new WebSocketChatModule("/chat"))
                //.WithModule(new WebSocketTerminalModule("/terminal"))
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            webServer.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return webServer;
        }
    }
}
