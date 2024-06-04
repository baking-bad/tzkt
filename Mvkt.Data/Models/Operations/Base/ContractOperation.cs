namespace Mvkt.Data.Models.Base
{
    public class ContractOperation : InternalOperation
    {
        public int? StorageId { get; set; }
        public int? BigMapUpdates { get; set; }
        public int? TokenTransfers { get; set; }

        public int? SubIds { get; set; }
    }
}
