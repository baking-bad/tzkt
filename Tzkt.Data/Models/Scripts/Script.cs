using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;

namespace Tzkt.Data.Models
{
    public class Script
    {
        public int Id { get; set; }
        public int ContractId { get; set; }

        public byte[] ParameterSchema { get; set; }
        public byte[] StorageSchema { get; set; }
        public byte[] CodeSchema { get; set; }

        #region schema
        [NotMapped]
        ContractScript _Schema = null;

        [NotMapped]
        public ContractScript Schema
        {
            get
            {
                _Schema ??= new ContractScript(Micheline.FromBytes(ParameterSchema), Micheline.FromBytes(StorageSchema));
                return _Schema;
            }
        }
        #endregion

        #region manager.tz
        public static readonly byte[] ManagerTzBytes = new byte[]
        {
            99, 144, 0, 160, 100, 161, 94, 128, 108, 144, 95, 128, 109, 66, 100, 111, 129, 108, 71, 100, 101, 102, 97,
            117, 108, 116, 144, 1, 128, 93, 144, 2, 98, 97, 99, 128, 33, 128, 22, 144, 31, 97, 128, 23, 160, 46, 107,
            160, 67, 128, 106, 1, 0, 128, 19, 98, 98, 128, 25, 128, 37, 160, 44, 96, 97, 98, 128, 79, 128, 39, 98,
            144, 31, 97, 128, 33, 128, 76, 128, 30, 128, 84, 128, 72, 98, 98, 128, 25, 128, 37, 160, 44, 96, 97,
            98, 128, 79, 128, 39, 128, 79, 128, 38, 128, 66, 99, 128, 32, 144, 61, 128, 109, 128, 66
        };

        public static ContractScript ManagerTz { get; } = new ContractScript(Micheline.FromBytes(ManagerTzBytes));
        #endregion
    }

    public static class ScriptModel
    {
        public static void BuildScriptModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Script>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Script>()
                .HasIndex(x => x.ContractId)
                .IsUnique();
            #endregion

            #region keys
            modelBuilder.Entity<Script>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Script>()
                .HasAlternateKey(x => x.ContractId);
            #endregion
        }
    }
}
