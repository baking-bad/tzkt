using System.Net.Http.Headers;

namespace Tzkt.Sync
{
    class JsonContent : StringContent
    {
        public JsonContent(string content) : base(content)
        {
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
    }
}