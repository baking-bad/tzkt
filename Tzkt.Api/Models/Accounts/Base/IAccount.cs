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

        string Alias { get; }

        string Address { get; }

        long Balance { get; set; }

        int NumContracts { get; }

        int NumDelegations { get; }

        int NumOriginations { get; }

        int NumTransactions { get; }

        int NumReveals { get; }

        int NumSystem { get; }
    }
}
