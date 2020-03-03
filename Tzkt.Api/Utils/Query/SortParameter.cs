using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api
{
    public class SortParameter
    {
        public bool Desc { get; private set; }
        public string Value { get; private set; }
        public QueryMode Mode { get; private set; }
        public string Error { get; private set; }

        public bool Invalid => Error != null;

        public static Task<SortParameter> Parse(string value, params string[] columns)
        {
            if (value == null)
                return Task.FromResult(new SortParameter());

            var desc = value.EndsWith(".desc");
            if (desc) value = value[..^5];

            if (!columns.Contains(value))
                return Task.FromResult(new SortParameter { Error = "Unsupported field for sorting by" });

            return Task.FromResult(new SortParameter { Mode = QueryMode.Exact, Value = value, Desc = desc });
        }
    }
}
