using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Tzkt.Sync.Utils
{
    public static class Bson
    {
        public static T Parse<T>(string hex)
        {
            var data = Hex.Parse(hex);
            using var mem = new MemoryStream(data);
            using var reader = new BsonDataReader(mem);
            return new JsonSerializer().Deserialize<T>(reader);
        }
    }
}
