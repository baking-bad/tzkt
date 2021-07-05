using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Tzkt.Data.Models
{
    public abstract class Account
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public AccountType Type { get; set; }
        public int FirstLevel { get; set; }
        public int LastLevel { get; set; }

        public long Balance { get; set; }
        public int Counter { get; set; }

        public int ContractsCount { get; set; }

        public int DelegationsCount { get; set; }
        public int OriginationsCount { get; set; }
        public int TransactionsCount { get; set; }
        public int RevealsCount { get; set; }

        public int MigrationsCount { get; set; }

        public int? DelegateId { get; set; }
        public int? DelegationLevel { get; set; }
        public bool Staked { get; set; }

        public string Metadata { get; set; }

        #region relations
        [ForeignKey(nameof(DelegateId))]
        public Delegate Delegate { get; set; }

        [ForeignKey(nameof(FirstLevel))]
        public Block FirstBlock { get; set; }
        #endregion

        public override string ToString() => Address;
    }

    public static class AccountModel
    {
        public static void BuildAccountModel(this ModelBuilder modelBuilder)
        {
            #region indexes
            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Id)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Address)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Type);

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Staked);

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.DelegateId);

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Metadata)
                .HasMethod("gin")
                .HasOperators("jsonb_path_ops");
            #endregion

            #region keys
            modelBuilder.Entity<Account>()
                .HasKey(x => x.Id);
            #endregion

            #region props
            modelBuilder.Entity<Account>()
                .HasDiscriminator<AccountType>(nameof(Account.Type))
                .HasValue<User>(AccountType.User)
                .HasValue<Delegate>(AccountType.Delegate)
                .HasValue<Contract>(AccountType.Contract);

            modelBuilder.Entity<Account>()
                .Property(x => x.Type)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Save);

            modelBuilder.Entity<Account>()
                .Property(x => x.Address)
                .IsFixedLength(true)
                .HasMaxLength(36)
                .IsRequired();

            modelBuilder.Entity<Account>()
                .Property(x => x.Metadata)
                .HasColumnType("jsonb");

            // TODO: don't load metadata to the indexer at all
            modelBuilder.Entity<Account>()
                .Property(x => x.Metadata)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            #endregion

            #region relations
            modelBuilder.Entity<Account>()
                .HasOne(x => x.FirstBlock)
                .WithMany(x => x.CreatedAccounts)
                .HasForeignKey(x => x.FirstLevel)
                .HasPrincipalKey(x => x.Level);

            modelBuilder.Entity<Account>()
                .HasOne(x => x.Delegate)
                .WithMany(x => x.DelegatedAccounts)
                .HasForeignKey(x => x.DelegateId);
            #endregion
        }
    }

    public enum AccountType : byte
    {
        User,
        Delegate,
        Contract
    }
}
