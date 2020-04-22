using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Protocols.Proto6.Serialization;

namespace Tzkt.Sync.Protocols.Proto6
{
    class Serializer : ISerializer
    {
        #region static
        public static JsonSerializerOptions Options { get; }

        static Serializer()
        {
            Options = new JsonSerializerOptions { MaxDepth = 128 };
            Options.Converters.Add(new JsonInt32Converter());
            Options.Converters.Add(new JsonInt64Converter());
            Options.Converters.Add(new RawBalanceUpdateConverter());
            Options.Converters.Add(new RawOperationContentConverter());
            Options.Converters.Add(new RawInternalOperationResultConverter());
        }
        #endregion

        public async Task<IBlock> DeserializeBlock(Stream stream)
        {
            try
            {
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var rawBlock = Newtonsoft.Json.JsonConvert.DeserializeObject<RawBlock>(json, 
                    new NSRawBalanceUpdateConverter(), 
                    new NSRawInternalOperationResultConverter(), 
                    new NSRawOperationContentConverter());

                //var rawBlock = await JsonSerializer.DeserializeAsync<RawBlock>(stream, Options);

                if (!rawBlock.IsValidFormat())
                    throw new SerializationException($"invalid format");

                return rawBlock;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }

        public async Task<RawConstants> DeserializeConstants(Stream stream)
        {
            try
            {
                var rawConstants = await JsonSerializer.DeserializeAsync<RawConstants>(stream, Options);

                if (!rawConstants.IsValidFormat())
                    throw new SerializationException($"invalid format");

                return rawConstants;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }

        public async Task<List<RawBakingRight>> DeserializeBakingRights(Stream stream)
        {
            try
            {
                var rawRights = await JsonSerializer.DeserializeAsync<List<RawBakingRight>>(stream, Options);

                foreach (var right in rawRights)
                    if (!right.IsValidFormat())
                        throw new SerializationException($"invalid format");

                return rawRights;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }

        public async Task<List<RawEndorsingRight>> DeserializeEndorsingRights(Stream stream)
        {
            try
            {
                var rawRights = await JsonSerializer.DeserializeAsync<List<RawEndorsingRight>>(stream, Options);

                foreach (var right in rawRights)
                    if (!right.IsValidFormat())
                        throw new SerializationException($"invalid format");

                return rawRights;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }
    }
}
