using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tzkt.Sync.Protocols.Genesis
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
    }
}
