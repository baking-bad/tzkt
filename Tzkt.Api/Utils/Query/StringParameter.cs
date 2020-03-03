using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api
{
    public class StringParameter
    {
        public string Value { get; private set; }
        public QueryMode Mode { get; private set; }
        public string Error { get; private set; }

        public bool Invalid => Error != null;

        public static Task<StringParameter> Parse(string value)
        {
            if (value == null)
                return Task.FromResult(new StringParameter());

            if (value == "null")
                return Task.FromResult(new StringParameter { Mode = QueryMode.Null });

            var template = false;
            if (value[0] == '*')
            {
                template = true;
            }
            else
            {
                for (int i = 1; i < value.Length; i++)
                {
                    if (value[i] == '*' && value[i - 1] != '\\')
                    {
                        template = true;
                        break;
                    }
                }
            }

            if (template)
            {
                value = value.Replace("%", "\\%").Replace("\\*", "ъуъ").Replace("*", "%").Replace("ъуъ", "*");
                return Task.FromResult(new StringParameter { Mode = QueryMode.Like, Value = value });
            }

            return Task.FromResult(new StringParameter { Mode = QueryMode.Exact, Value = value });
        }
    }
}
