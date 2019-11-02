using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DelegateChange
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int DelegateId { get; set; }
        public DelegateChangeType Type { get; set; }

        #region relations
        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }
        #endregion
    }

    public enum DelegateChangeType
    {
        Activated,
        Deactivated,
        Reactivated
    }

    public static class DelegateChangeModel
    {
        public static void BuildDelegateChangeModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<DelegateChange>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<DelegateChange>()
                .HasIndex(x => x.DelegateId);
            #endregion

            #region keys
            modelBuilder.Entity<DelegateChange>()
                .HasKey(x => x.Id);
            #endregion

            #region relations
            modelBuilder.Entity<DelegateChange>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.DelegateChanges)
                .HasForeignKey(x => x.DelegateId);
            #endregion
        }
    }
}
