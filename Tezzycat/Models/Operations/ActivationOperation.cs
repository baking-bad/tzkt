using Tezzycat.Models.Base;

namespace Tezzycat.Models
{
    public class ActivationOperation : BaseOperation
    {
        public string Address { get; set; }
        public long Balance { get; set; }
    }
}
