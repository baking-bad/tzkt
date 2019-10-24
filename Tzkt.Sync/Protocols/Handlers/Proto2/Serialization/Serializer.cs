using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Tzkt.Sync.Protocols.Proto2.Serialization;

namespace Tzkt.Sync.Protocols.Proto2
{
    class Serializer : ISerializer
    {
        #region static
        public static JsonSerializerOptions Options { get; }

        static Serializer()
        {
            Options = new JsonSerializerOptions();
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
                var rawBlock = await JsonSerializer.DeserializeAsync<RawBlock>(stream, Options);

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

        public async Task<RawDelegate> DeserializeDelegate(Stream stream)
        {
            try
            {
                var rawContracts = await JsonSerializer.DeserializeAsync<RawDelegate>(stream, Options);

                if (!rawContracts.IsValidFormat())
                    throw new SerializationException($"invalid format");

                return rawContracts;
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"[{ex.Path}] {ex.Message}");
            }
        }
    }
}
