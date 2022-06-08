using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SortBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class SortParameter : INormalizable
    {
        /// <summary>
        /// **Ascending** sort mode (optional, i.e. `sort.asc=id` is the same as `sort=id`). \
        /// Specify a field name to sort by.
        /// 
        /// Example: `?sort=balance`.
        /// </summary>
        public string Asc { get; set; }

        /// <summary>
        /// **Descending** sort mode. \
        /// Specify a field name to sort by descending.
        /// 
        /// Example: `?sort.desc=id`.
        /// </summary>
        public string Desc { get; set; }

        public bool Validate(params string[] fields)
        {
            if (Asc != null)
            {
                for (int i = 0; i < fields.Length; i++)
                    if (fields[i] == Asc)
                        return true;

                return false;
            }

            if (Desc != null)
            {
                for (int i = 0; i < fields.Length; i++)
                    if (fields[i] == Desc)
                        return true;

                return false;
            }

            return true;
        }

        public string Normalize(string name)
        {
            return Asc != null ? $"sort.asc={Asc}&" : $"sort.desc={Desc}&";
        }
    }
}
