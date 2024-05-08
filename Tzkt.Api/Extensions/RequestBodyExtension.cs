using System.Text;

namespace Tzkt.Api.Services.Auth
{
    public static class RequestBodyExtension
    {
        public static async Task<string> ReadAsStringAsync(this Stream body)
        {
            using var reader = new StreamReader(body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}