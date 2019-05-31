using System;
using Newtonsoft.Json.Linq;

namespace Tezzycat.Sync
{
    public static class JTokenExtension
    {
        public static DateTime DateTime(this JToken jToken) => jToken.Value<DateTime>();
        public static string String(this JToken jToken) => jToken.Value<string>();
        public static long Int64(this JToken jToken) => jToken.Value<long>();
        public static int Int32(this JToken jToken) => jToken.Value<int>();
        public static bool Bool(this JToken jToken) => jToken.Value<bool>();
    }
}
