using System;
using Newtonsoft.Json.Linq;

namespace Tzkt.Sync
{
    public static class JTokenExtension
    {
        public static DateTime DateTime(this JToken jToken) => jToken.Value<DateTime>();
        public static string String(this JToken jToken) => jToken.Value<string>();
        public static long Int64(this JToken jToken) => jToken.Value<long>();
        public static int Int32(this JToken jToken) => jToken.Value<int>();
        public static bool Bool(this JToken jToken) => jToken.Value<bool>();

        public static Data.Models.Base.OperationStatus OperationStatus(this JToken jToken)
            => jToken.Value<string>() switch
            {
                "applied" => Data.Models.Base.OperationStatus.Applied,
                _ => throw new NotImplementedException()
            };

        public static void RequireValue(this JToken jToken, string property)
        {
            if (string.IsNullOrEmpty(jToken[property]?.String()))
                throw new Exception($"Property {property} is missed");
        }

        public static void RequireObject(this JToken jToken, string property)
        {
            if (jToken[property] as JObject == null)
                throw new Exception($"Property {property} is missed");
        }

        public static void RequireArray(this JToken jToken, string property)
        {
            if (jToken[property] as JArray == null)
                throw new Exception($"Property {property} is missed");
        }
    }
}
