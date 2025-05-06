using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(SelectionBinder))]
    [JsonSchemaExtensionData("x-tzkt-extension", "query-parameter")]
    public class SelectionParameter : INormalizable
    {
        /// <summary>
        /// **Fields** selection mode (optional, i.e. `select.fields=balance` is the same as `select=balance`). \
        /// Specify a comma-separated list of fields to include into response.
        /// 
        /// Example:
        /// `?select=address,balance as b,metadata.name as meta_name` will result in
        /// `[ { "address": "asd", "b": 10, "meta_name": "qwe" } ]`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<SelectionField>? Fields { get; set; }

        /// <summary>
        /// **Values** selection mode. \
        /// Specify a comma-separated list of fields to include their values into response.
        /// 
        /// Example:
        /// `?select.values=address,balance,metadata.name`  will result in
        /// `[ [ "asd", 10, "qwe" ] ]`.
        /// </summary>
        [JsonSchemaType(typeof(List<string>))]
        public List<SelectionField>? Values { get; set; }

        public string Normalize(string name)
        {
            return $"{name}={string.Join(",", (Fields ?? Values)!.Select(x => $"{x.Full} as {x.Alias}"))}";
        }
    }

    public class SelectionField
    {
        public required string Alias { get; init; }
        public required string Field { get; init; }
        public required string Full { get; init; }
        public List<string>? Path { get; init; }

        public string? PathString => Path == null ? null : string.Join(",", Path);

        public string? Column { get; set; }

        public SelectionField? SubField() => Path == null ? null : new()
        {
            Field = Path[0],
            Path = Path.Count > 1 ? Path.Skip(1).ToList() : null,
            Alias = Alias,
            Full = Full
        };

        public static bool TryParse(string value, [NotNullWhen(true)] out SelectionField? field)
        {
            var ss = value.Split(" as ");
            if (ss.Length == 1 || ss.Length == 2 && Regexes.Field().IsMatch(ss[1]))
            {
                if (Regexes.Field().IsMatch(ss[0]))
                {
                    field = new()
                    {
                        Field = ss[0],
                        Alias = ss[^1],
                        Full = ss[0]
                    };
                    return true;
                }
                else if (Regexes.FieldPath().IsMatch(ss[0]))
                {
                    var sss = ss[0].Split('.');
                    field = new()
                    {
                        Field = sss[0],
                        Path = sss.Skip(1).ToList(),
                        Alias = ss[^1],
                        Full = ss[0]
                    };
                    return true;
                }
            }
            field = null;
            return false;
        }
    }
}
