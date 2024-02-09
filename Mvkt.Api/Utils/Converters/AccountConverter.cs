using System.Text.Json;
using System.Text.Json.Serialization;
using Mvkt.Api.Models;

namespace Mvkt.Api
{
    class AccountConverter : JsonConverter<Account>
    {
        public override Account Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Account value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
