using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    public class BadRequest : BadRequestObjectResult
    {
        public BadRequest(ActionContext context) : base(new
        {
            Code = 400,
            Errors = new Dictionary<string, string>(
                context.ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => 
                    new KeyValuePair<string, string>(x.Key, x.Value.Errors[0].ErrorMessage)))
        }) { }

        public BadRequest(string field, string error) : base(new
        {
            Code = 400,
            Errors = new Dictionary<string, string> { { field, error } }
        }) { }
    }
}
