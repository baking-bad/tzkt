using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    public class BadRequest : BadRequestObjectResult
    {
        public BadRequest(string field, string message) : base(new
        {
            Code = "400",
            Field = field,
            Message = message
        }) { }
    }
}
