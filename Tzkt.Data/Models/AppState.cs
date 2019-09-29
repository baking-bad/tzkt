using System;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class AppState
    {
        public int Id { get; set; }
        public bool Synced { get; set; }

        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Protocol { get; set; }
        public string Hash { get; set; }

        public int Counter { get; set; }
    }

    public static class AppStateModel
    {
        public static void BuildAppStateModel(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppState>().HasData(
                new AppState
                {
                    Id = -1,
                    Level = -1,
                    Timestamp = DateTime.MinValue,
                    Protocol = "",
                    Hash = "",
                });
        }
    }
}
