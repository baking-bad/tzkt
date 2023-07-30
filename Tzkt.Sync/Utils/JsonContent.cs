using System.Net.Http.Headers;

namespace Tzkt.Sync
{
    class JsonContent : StringContent
    {
        public JsonContent(string content) : base(content)
        {
            Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }
    }
}