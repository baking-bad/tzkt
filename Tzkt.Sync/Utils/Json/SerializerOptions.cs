using System.Text.Json;

namespace Tzkt.Sync
{
    public static class SerializerOptions
    {
        public static JsonSerializerOptions Default { get; }

        static SerializerOptions()
        {
            Default = new JsonSerializerOptions { MaxDepth = 128 };
            Default.Converters.Add(new JsonInt32Converter());
            Default.Converters.Add(new JsonInt64Converter());
        }
    }
}
