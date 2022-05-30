using System;
using System.Collections.Generic;
using System.Text;

namespace Tzkt.Api.Services.Output
{
    public class OutputCacheKeysProvider
    {
        public static string BuildQuery(params (string, object)[] args)
        {
            var sb = new StringBuilder();
            sb.Append('?');
            foreach (var arg in args)
            {
                var b = arg.Item2 is INormalized;
                sb.Append(b ? ((INormalized)arg.Item2).Normalize() : $"{arg.Item1}={arg.Item2}&");
            }

            return sb.ToString();
        }
    }
}