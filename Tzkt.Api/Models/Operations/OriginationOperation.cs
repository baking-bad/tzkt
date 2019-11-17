using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class OriginationOperation
    {
        public int Level { get; set; }

        public DateTime Timestamp { get; set; }

        public string Hash { get; set; }

        public Alias Sender { get; set; }

        public int Counter { get; set; }

        public int GasLimit { get; set; }

        public int GasUsed { get; set; }

        public int StorageLimit { get; set; }

        public int StorageUsed { get; set; }

        public long BakerFee { get; set; }

        public long StorageFee { get; set; }

        public long AllocationFee { get; set; }

        public long ContractBalance { get; set; }

        public Alias ContractDelegate { get; set; }

        public bool Internal { get; set; }

        public string Status { get; set; }

        public Alias OriginatedContract { get; set; }
    }
}
