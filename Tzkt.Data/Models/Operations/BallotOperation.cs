using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models.Base;

namespace Tzkt.Data.Models
{
    public class BallotOperation : BaseOperation
    {
        public int Epoch { get; set; }
        public int Period { get; set; }
        public int ProposalId { get; set; }
        public int SenderId { get; set; }
        public long VotingPower { get; set; }

        public Vote Vote { get; set; }
    }

    public static class BallotOperationModel
    {
        public static void BuildBallotOperationModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<BallotOperation>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<BallotOperation>()
                .Property(x => x.OpHash)
                .IsFixedLength(true)
                .HasMaxLength(51)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.Epoch);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.Period);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.OpHash);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.SenderId);

            modelBuilder.Entity<BallotOperation>()
                .HasIndex(x => x.ProposalId);
            #endregion
        }
    }

    public enum Vote
    {
        Yay,
        Nay,
        Pass
    }
}
