using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public interface IOperation
    {
        int Id { get; }

        string Type { get; }
    }
}
