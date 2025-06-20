﻿using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Netezos.Encoding;

namespace Tzkt.Sync.Utils
{
    public static class Bson
    {
        public static JToken Parse(string hex)
        {
            var data = Hex.Parse(hex);
            using var mem = new MemoryStream(data);
            using var reader = new BsonDataReader(mem);
            return JToken.ReadFrom(reader);
        }
    }
}
