using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Tzkt.Data.Models;

namespace Tzkt.Api.Models
{
    public interface IAccount
    {
        string Type { get; }

        string Address { get; }
    }
}
