using Tzkt.Api.Services;

namespace Tzkt.Api
{
    public class ContractFilter : INormalizable
    {
        /// <summary>
        /// Filter by internal MvKT id.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter id { get; set; }

        /// <summary>
        /// Filter by address.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AddressParameter address { get; set; }

        /// <summary>
        /// Filter by kind (`delegator_contract`, `smart_contract`, or `asset`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public ContractKindParameter kind { get; set; }

        /// <summary>
        /// Filter by tzips (`fa1`, `fa12`, or `fa2`).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public ContractTagsParameter tzips { get; set; }

        /// <summary>
        /// Filter by balance.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int64Parameter balance { get; set; }

        /// <summary>
        /// Filter by creator.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter creator { get; set; }

        /// <summary>
        /// Filter by manager.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter manager { get; set; }

        /// <summary>
        /// Filter by delegate.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public AccountParameter @delegate { get; set; }

        /// <summary>
        /// Filter by level of the block where the contract was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter firstActivity { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the contract was first seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter firstActivityTime { get; set; }

        /// <summary>
        /// Filter by level of the block where the contract was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter lastActivity { get; set; }

        /// <summary>
        /// Filter by timestamp (ISO 8601) of the block where the contract was last seen.  
        /// Click on the parameter to expand more details.
        /// </summary>
        public TimestampParameter lastActivityTime { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of contract parameter and storage types (helpful for searching similar contracts).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter typeHash { get; set; }

        /// <summary>
        /// Filter by 32-bit hash of contract code (helpful for searching same contracts).  
        /// Click on the parameter to expand more details.
        /// </summary>
        public Int32Parameter codeHash { get; set; }

        public string Normalize(string name)
        {
            return ResponseCacheService.BuildKey("",
                ("id", id), ("address", address), ("kind", kind), ("tzips", tzips), ("balance", balance), ("creator", creator), ("manager", manager),
                ("@delegate", @delegate), ("firstActivity", firstActivity), ("firstActivityTime", firstActivityTime), ("lastActivity", lastActivity),
                ("lastActivityTime", lastActivityTime), ("typeHash", typeHash), ("codeHash", codeHash));
        }
    }
}
