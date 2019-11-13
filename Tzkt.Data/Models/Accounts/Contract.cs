using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Contract : Account
    {
        public ContractKind Kind { get; set; }
        public bool? Spendable { get; set; }

        public int? ManagerId { get; set; }
        public int? WeirdDelegateId { get; set; }

        #region relations
        [ForeignKey(nameof(ManagerId))]
        public Account Manager { get; set; }

        [ForeignKey(nameof(WeirdDelegateId))]
        public User WeirdDelegate { get; set; }
        #endregion
    }

    public enum ContractKind : byte
    {
        DelegatorContract,
        SmartContract
    }

    public static class ContractModel
    { 
        public static void BuildContractModel(this ModelBuilder modelBuilder)
        {

        }
    }
}
