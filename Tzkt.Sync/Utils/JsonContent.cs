using System.Text;

namespace Tzkt.Sync
{
    class JsonContent(string content) : StringContent(content, Encoding.UTF8, "application/json") { }
}