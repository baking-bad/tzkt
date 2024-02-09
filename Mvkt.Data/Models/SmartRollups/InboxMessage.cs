﻿using Microsoft.EntityFrameworkCore;

namespace Mvkt.Data.Models
{
    public class InboxMessage
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public InboxMessageType Type { get; set; }
        public int? PredecessorLevel { get; set; } // only for LevelInfo
        public long? OperationId { get; set; } // only for Internal and External
        public byte[] Payload { get; set; } // only for External
        public string Protocol { get; set; } // only for Migration
    }

    public enum InboxMessageType
    {
        LevelStart,
        LevelInfo,
        LevelEnd,
        Transfer,
        External,
        Migration
    }

    public static class InboxMessageModel
    {
        public static void BuildInboxMessageModel(this ModelBuilder modelBuilder)
        {
            #region keys
            modelBuilder.Entity<InboxMessage>()
                .HasKey(x => x.Id);
            #endregion

            #region indexes
            modelBuilder.Entity<InboxMessage>()
                .HasIndex(x => x.Level);

            modelBuilder.Entity<InboxMessage>()
                .HasIndex(x => new { x.Type, x.Id });

            modelBuilder.Entity<InboxMessage>()
                .HasIndex(x => x.OperationId);
            #endregion
        }
    }
}
