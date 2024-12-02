using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DalAttestation
    {
        public int Id { get; set; }
        public long DalPublishCommitmentOpsId { get; set; }
        public long AttestationId { get; set; }
        public bool Attested { get; set; }
        public int ShardsCount { get; set; }

        #region relations
        [ForeignKey(nameof(DalPublishCommitmentOpsId))]
        public DalPublishCommitmentOperation DalPublishCommitmentOp { get; set; }

        [ForeignKey(nameof(AttestationId))]
        public EndorsementOperation Attestation { get; set; }
        #endregion
    }

    public static class DalAttestationModel
    {
        public static void BuildDalAttestationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DalAttestation>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<DalAttestation>()
                .HasIndex(x => x.DalPublishCommitmentOpsId);

            modelBuilder.Entity<DalAttestation>()
                .HasIndex(x => x.AttestationId);
            #endregion
        }
    }
}
