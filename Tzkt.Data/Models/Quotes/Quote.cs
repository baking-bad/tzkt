using Microsoft.EntityFrameworkCore;
using System;

namespace Tzkt.Data.Models
{
    public class Quote : IQuote
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }

        public double Btc { get; set; }
        public double Eur { get; set; }
        public double Usd { get; set; }
        public double Cny { get; set; }
        public double Jpy { get; set; }
        public double Krw { get; set; }
    }

    public static class QuoteModel
    {
        public static void BuildQuoteModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Quote>()
                .HasIndex(x => x.Level)
                .IsUnique();
            #endregion

            #region keys
            modelBuilder.Entity<Quote>()
                .HasKey(x => x.Id);
            #endregion
        }
    }
}
