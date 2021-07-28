using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Commitment
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public long Balance { get; set; }

        public int? AccountId { get; set; }
        public int? Level { get; set; }
    }

    public static class CommitmentModel
    {
        public static void BuildCommitmentModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Commitment>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Commitment>()
                .Property(x => x.Address)
                .IsFixedLength(true)
                .HasMaxLength(37)
                .IsRequired();
            #endregion

            #region indexes
            modelBuilder.Entity<Commitment>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Commitment>()
                .HasIndex(x => x.Address)
                .IsUnique();
            #endregion
        }
    }
}
