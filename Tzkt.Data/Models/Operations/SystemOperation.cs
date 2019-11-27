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

        #region relations
        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; }
        #endregion
    }

    public enum SystemEvent
    {
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
            modelBuilder.Entity<NonceRevelationOperation>()
                .HasKey(x => x.Id);
            #endregion
        }
    }
}
