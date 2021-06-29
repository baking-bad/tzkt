using System;
using System.Data;
using System.Text.Json;
using Dapper;

namespace Tzkt.Api
{
    public class JsonElementTypeHandler : SqlMapper.TypeHandler<JsonElement?>
    {
        public override JsonElement? Parse(object value)
        {
            if (value is not string) return null;
            using var doc = JsonDocument.Parse((string)value);
            return doc.RootElement.Clone();
        }

        public override void SetValue(IDbDataParameter parameter, JsonElement? value)
        {
            throw new NotImplementedException();
        }
    }
}
