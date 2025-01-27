using System;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace MusicBeePlugin
{
    public class MusicBeeController : WebApiController
    {
        [Route(HttpVerbs.Get, "/hello")]
        public string Hello([QueryField] string username)
        {
            return $"Hello {username}";
        }

        [Route(HttpVerbs.Post, "/helloworld")]
        public void HelloWorld([QueryField] string username)
        {
            Console.WriteLine($"Hello {username}");
        }

        [Route(HttpVerbs.Get, "/currenttrack")]
        public string CurrentTrack()
        {
            return Plugin.GetCurrentTrack();
        }
    }
}
