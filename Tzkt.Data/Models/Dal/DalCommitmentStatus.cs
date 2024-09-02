using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DalCommitmentStatus
    {
        public int Id { get; set; }
        public long PublishmentId { get; set; }
        public int ShardsAttested { get; set; }
        public bool Attested { get; set; }
        
        #region relations
        [ForeignKey(nameof(PublishmentId))]
        public DalPublishCommitmentOperation Publishment { get; set; }
        #endregion
    }

    public static class DalCommitmentStatusModel
    {
        public static void BuildDalCommitmentStatusModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DalCommitmentStatus>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<DalCommitmentStatus>()
                .HasIndex(x => x.PublishmentId);
            #endregion

            #region relations
            modelBuilder.Entity<DalCommitmentStatus>()
                .HasOne(x => x.Publishment)
                .WithOne(x => x.DalCommitmentStatus)
                .HasForeignKey<DalCommitmentStatus>(x => x.PublishmentId)
                .HasPrincipalKey<DalPublishCommitmentOperation>(x => x.Id);
            #endregion
        }
    }
}
