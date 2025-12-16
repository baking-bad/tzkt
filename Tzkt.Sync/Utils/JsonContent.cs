using System.Net.Http.Headers;

namespace Tzkt.Sync
{
    class JsonContent(string content) : StringContent(content, new MediaTypeHeaderValue("application/json")) { }
}