using Tezzycat.Data.Models.Base;

namespace Tezzycat.Data.Models
{
    public class ActivationOperation : BaseOperation
    {
        public string Address { get; set; }
        public long Balance { get; set; }
    }
}
