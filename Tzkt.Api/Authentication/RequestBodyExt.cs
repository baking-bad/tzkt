using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Tzkt.Api.Authentication
{
    public static class RequestBodyExt
    {
        public static async Task<string> GetStringAsync(this Stream body)
        {
            using var reader = new StreamReader(body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}