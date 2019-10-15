using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class WeirdDelegation
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int DelegateId { get; set; }
        public int OriginationId { get; set; }

        #region relations
        [ForeignKey(nameof(OriginationId))]
        public OriginationOperation Origination { get; set; }
        #endregion
    }

    public static class WeirdDelegationModel
    {
        public static void BuildWeirdDelegationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<WeirdDelegation>()
                .HasKey(x => x.Id);
            #endregion

            #region relations
            modelBuilder.Entity<WeirdDelegation>()
                .HasOne(x => x.Origination)
                .WithOne(x => x.WeirdDelegation)
                .HasForeignKey<WeirdDelegation>(x => x.OriginationId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion
        }
    }
}
