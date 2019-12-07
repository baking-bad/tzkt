using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class SystemOperation
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }

        public int AccountId { get; set; }
        public SystemEvent Event { get; set; }
        public long BalanceChange { get; set; }

        #region relations
        [ForeignKey(nameof(Level))]
        public Block Block { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; }
        #endregion
    }

    public enum SystemEvent
    {
        Bootstrap,
        ActivateDelegate,
        AirDrop
    }

    public static class SystemOperationModel
    {
        public static void BuildSystemOperationModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<SystemOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<SystemOperation>()
                .HasIndex(x => x.AccountId);
            #endregion
            
            #region keys
            modelBuilder.Entity<SystemOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region relations
            modelBuilder.Entity<SystemOperation>()
                .HasOne(x => x.Block)
                .WithMany(x => x.SystemOperations)
                .HasForeignKey(x => x.Level)
                .HasPrincipalKey(x => x.Level);
            #endregion
        }
    }
}
