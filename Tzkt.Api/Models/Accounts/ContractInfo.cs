namespace Tzkt.Api.Models
{
    public class ContractInfo(Alias manager, string kind)
    {
        /// <summary>
        /// Kind of the contract (`delegator_contract` or `smart_contract`),
        /// where `delegator_contract` - manager.tz smart contract for delegation purpose only
        /// </summary>
        public required string Kind { get; set; } = kind;

        /// <summary>
        /// Name of the project behind the contract or contract description
        /// </summary>
        public string? Alias { get; set; } = manager.Name;

        /// <summary>
        /// Public key hash of the contract
        /// </summary>
        public required string Address { get; set; } = manager.Address;
    }
}
