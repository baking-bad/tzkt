using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class TicketBalance
    {
        public long Id { get; set; }
        public int TicketerId { get; set; }
        public long TicketId { get; set; }
        public int AccountId { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public int TransfersCount { get; set; }
        public BigInteger Balance { get; set; }
    }

    public static class TicketBalanceModel
    {
        public static void BuildTicketBalanceModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<TicketBalance>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.TicketerId);

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.TicketId);

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => new { x.AccountId, x.TicketerId });

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => new { x.AccountId, x.TicketId })
                .IsUnique();

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.FirstLevel);

            modelBuilder.Entity<TicketBalance>()
                .HasIndex(x => x.LastLevel);
            #endregion
        }
    }
}
