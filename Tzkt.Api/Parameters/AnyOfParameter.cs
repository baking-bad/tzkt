using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;
using Newtonsoft.Json;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AnyOfBinder))]
    public class AnyOfParameter
    {
        [Required]
        public IEnumerable<string> Fields { get; set; }

        [Required]
        [JsonSchemaType(typeof(string))]  // address (str) is resolved to account id (int)
        public int Value { get; set; }
    }
}