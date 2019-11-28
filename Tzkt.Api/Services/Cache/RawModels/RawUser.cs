using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Services.Cache
{
    public class RawUser : RawAccount
    {
        public override string Type => "user";

        public bool? Activated { get; set; }
        public string PublicKey { get; set; }
    }
}
