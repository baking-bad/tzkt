using System;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    //TODO: add chain id
    public class AppState
    {
        public int Id { get; set; }
        public int KnownHead { get; set; }
        public DateTime LastSync { get; set; }

        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string Protocol { get; set; }
        public string NextProtocol { get; set; }
        public string Hash { get; set; }

        public int GlobalCounter { get; set; }
        public int ManagerCounter { get; set; }
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
                    NextProtocol = "",
                    Hash = "",

                    GlobalCounter = 0,
                    ManagerCounter = 0
                });
        }
    }
}
