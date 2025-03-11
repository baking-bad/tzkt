namespace Tzkt.Data.Models.Base
{
    public class InternalOperation : ManagerOperation
    {
        public int? InitiatorId { get; set; }
        public int? Nonce { get; set; }
    }
}
