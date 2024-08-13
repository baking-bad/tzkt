using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class DalAttestationStatus
    {
        public int Id { get; set; }
        public int DalCommitmentStatusId { get; set; }
        public long AttestationId { get; set; }
        public bool Attested { get; set; }

        #region relations
        [ForeignKey(nameof(DalCommitmentStatusId))]
        public DalCommitmentStatus DalCommitmentStatus { get; set; }

        [ForeignKey(nameof(AttestationId))]
        public EndorsementOperation Attestation { get; set; }
        #endregion
    }

    public static class DalAttestationModel
    {
        public static void BuildDalAttestationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<DalAttestationStatus>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<DalAttestationStatus>()
                .HasIndex(x => x.DalCommitmentStatusId);

            modelBuilder.Entity<DalAttestationStatus>()
                .HasIndex(x => x.AttestationId);
            #endregion
        }
    }
}
