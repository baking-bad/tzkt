using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Tzkt.Data.Models
{
    public class Software
    {
        public int Id { get; set; }
        public int BlocksCount { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }
        public string ShortHash { get; set; }

        #region off-chain
        public DateTime? CommitDate { get; set; }
        public string CommitHash { get; set; }
        public string Version { get; set; }
        public List<string> Tags { get; set; }
        #endregion
    }

    public static class SoftwareModel
    {
        public static void BuildSoftwareModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<Software>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Software>()
                .Property(x => x.ShortHash)
                .IsFixedLength(true)
                .HasMaxLength(8)
                .IsRequired();

            modelBuilder.Entity<Software>()
                .Property(x => x.CommitHash)
                .IsFixedLength(true)
                .HasMaxLength(40);
            #endregion
        }
    }
}
