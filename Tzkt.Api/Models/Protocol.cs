using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Protocol
    {
        public int Code { get; set; }

        public string Hash { get; set; }

        public int  FirstLevel { get; set; }

        public int LastLevel { get; set; }

        public ProtocolConstants Constants { get; set; }
    }
}
