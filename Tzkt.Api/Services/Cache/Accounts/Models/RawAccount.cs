using Tzkt.Api.Models;

namespace Tzkt.Api.Services.Cache
{
    public class RawAccount
    {
        public int Id { get; set; }
        public string Address { get; set; }
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

        public AccountMetadata Metadata { get; set; }

        public string Alias => Metadata?.Alias;

        public Alias Info => new()
        {
            Name = Metadata?.Alias,
            Address = Address
        };
    }
}
