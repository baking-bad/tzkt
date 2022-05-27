using System;
using System.Collections.Generic;

namespace Tzkt.Api.Services.Output
{
    public class OutputCacheKeysProvider
    {
        public static string BuildQuery(params object[] args)
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            return "";
        }
    }
}