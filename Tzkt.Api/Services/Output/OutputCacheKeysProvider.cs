using System;
using System.Collections.Generic;
using System.Text;

namespace Tzkt.Api.Services.Output
{
    public class OutputCacheKeysProvider
    {
        public static string BuildQuery(string requestPath, params (string, object)[] args)
        {
            var sb = new StringBuilder(requestPath);
            sb.Append('?');
            foreach (var (name, value) in args)
            {
                sb.Append(value is INormalized normalizable ? normalizable.Normalize("") : $"{name}={value}&");
            }

            return sb.ToString();
        }
    }
}