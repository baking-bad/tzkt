using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [JsonSchemaType(typeof(string))]
    [ModelBinder(BinderType = typeof(AnyOfBinder))]
    public class AnyOfParameter
    {
        public IEnumerable<string> Fields { get; set; }
        public int Value { get; set; }
    }
}