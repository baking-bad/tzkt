using System;
using Newtonsoft.Json.Linq;

namespace Tezzycat.Sync
{
    public static class JObjectExtension
    {
        public static DateTime DateTime(this JObject jObject) => jObject.Value<DateTime>();
        public static string String(this JObject jObject) => jObject.Value<string>();
        public static long Int64(this JObject jObject) => jObject.Value<long>();
        public static int Int32(this JObject jObject) => jObject.Value<int>();
        public static bool Bool(this JObject jObject) => jObject.Value<bool>();
    }
}
