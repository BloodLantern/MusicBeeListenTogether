using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace MusicBeePlugin
{
    public class MusicBeeController : WebApiController
    {
        // hello?username=Elvis
        [Route(HttpVerbs.Get, "/hello")]
        public string Hello([QueryField] string username)
        {
            return $"Hello {username}";
        }
    }
}
