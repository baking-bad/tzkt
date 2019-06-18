using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class ActivationOperation : BaseOperation
    {
        public string Address { get; set; }
        public long Balance { get; set; }
    }
}
