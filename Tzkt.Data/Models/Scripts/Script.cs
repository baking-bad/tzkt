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
