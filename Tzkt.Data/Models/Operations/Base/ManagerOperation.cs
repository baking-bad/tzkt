﻿namespace Tzkt.Data.Models.Base
{
    public class ManagerOperation : BaseOperation
    {
        public required int SenderId { get; set; }
        public int Counter { get; set; }

        public long BakerFee { get; set; }
        public long? StorageFee { get; set; }
        public long? AllocationFee { get; set; }

        public int GasLimit { get; set; }
        public int GasUsed { get; set; }

        public int StorageLimit { get; set; }
        public int StorageUsed { get; set; }

        public OperationStatus Status { get; set; }
        public string? Errors { get; set; }
    }

    public enum OperationStatus : byte
    {
        None,
        Applied,
        Backtracked,
        Skipped,
        Failed
    }
}
