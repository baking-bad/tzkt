using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tzkt.Api
{
    public enum OffsetMode
    {
        Page = 0,
        Items = 1,
        Id = 2
    }
}
