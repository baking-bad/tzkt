using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(ProtocolBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class ProtocolParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a protocol hash to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?protocol=PsCARTHAGaz...`.
        /// </summary>
        public string Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a protocol hash to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?sender.ne=PsBabyM1eUX...`.
        /// </summary>
        public string Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of protocol hashes to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?sender.in=PsCARTHAGaz,PsBabyM1eUX`.
        /// </summary>
        public List<string> In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of protocol hashes to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?sender.ni=PsCARTHAGaz,PsBabyM1eUX`.
        /// </summary>
        public List<string> Ni { get; set; }

        public string Normalize(string name)
        {
            var sb = new StringBuilder();

            if (Eq != null)
            {
                sb.Append($"{name}.eq={Eq}&");
            }

            if (Ne != null)
            {
                sb.Append($"{name}.ne={Ne}&");
            }
            
            if (In?.Count > 0)
            {
                sb.Append($"{name}.in={string.Join(",", In.OrderBy(x => x))}&");
            }

            if (Ni?.Count > 0)
            {
                sb.Append($"{name}.ni={string.Join(",", Ni.OrderBy(x => x))}&");
            }

            return sb.ToString();
        }
    }
}
