﻿using System.Text;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(MigrationKindBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    [JsonSchemaExtensionData("x-tzkt-query-parameter", "bootstrap,activate_delegate,airdrop,proposal_invoice,code_change,origination,subsidy,remove_bigmap_key")]
    public class MigrationKindParameter : INormalizable
    {
        /// <summary>
        /// **Equal** filter mode (optional, i.e. `param.eq=123` is the same as `param=123`). \
        /// Specify a migration kind to get items where the specified field is equal to the specified value.
        /// 
        /// Example: `?kind=bootstrap`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Eq { get; set; }

        /// <summary>
        /// **Not equal** filter mode. \
        /// Specify a migration kind to get items where the specified field is not equal to the specified value.
        /// 
        /// Example: `?type.ne=proposal_invoice`.
        /// </summary>
        [JsonSchemaType(typeof(string))]
        public int? Ne { get; set; }

        /// <summary>
        /// **In list** (any of) filter mode. \
        /// Specify a comma-separated list of migration kinds to get items where the specified field is equal to one of the specified values.
        /// 
        /// Example: `?sender.in=bootstrap,proposal_invoice`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int>? In { get; set; }

        /// <summary>
        /// **Not in list** (none of) filter mode. \
        /// Specify a comma-separated list of migration kinds to get items where the specified field is not equal to all the specified values.
        /// 
        /// Example: `?sender.ni=airdrop,bootstrap`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<int>? Ni { get; set; }

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
