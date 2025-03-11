using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class SmartRollup : Account
    {
        [Column("CreatorId")]
        public required int CreatorId { get; set; }
        public required PvmKind PvmKind { get; set; }
        public required byte[] ParameterSchema { get; set; }
        public required string GenesisCommitment { get; set; }
        public required string LastCommitment { get; set; }
        public required int InboxLevel { get; set; }

        public int TotalStakers { get; set; }
        public int ActiveStakers { get; set; }
        public int ExecutedCommitments { get; set; }
        public int CementedCommitments { get; set; }
        public int PendingCommitments { get; set; }
        public int RefutedCommitments { get; set; }
        public int OrphanCommitments { get; set; }
    }

    public enum PvmKind
    {
        Arith,
        Wasm
    }

    public static class SmartRollupModel
    { 
        public static void BuildSmartRollupModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<SmartRollup>()
                .HasIndex(x => x.CreatorId);
            #endregion
        }
    }
}
