using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models
{
    public class BalanceSnapshot
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int AddressId { get; set; }
        public int DelegateId { get; set; }
        public long Balance { get; set; }
    }
}
