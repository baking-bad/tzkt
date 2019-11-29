using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class RelatedContract
    {
        public string Kind { get; set; }

        public string Alias { get; set; }

        public string Address { get; set; }

        public long Balance { get; set; }

        public DelegateInfo Delegate { get; set; }
    }
}
