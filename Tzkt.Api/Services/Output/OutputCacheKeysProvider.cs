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
                //TODO Check for null
                if (value == null)
                    continue;
                
                sb.Append(value is INormalized normalizable ? normalizable.Normalize(name) : $"{name}={value}&");
            }

            return sb.ToString();
        }
    }
}