namespace Mvkt.Api.Models
{
    public class SrStaker : Alias
    {
        /// <summary>
        /// Internal MvKT id.  
        /// **[sortable]**
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bond status (`active`, `returned`, or `lost`).
        /// </summary>
        public string BondStatus { get; set; }

        /// <summary>
        /// Level of the block where the staker published his first commitment.  
        /// **[sortable]**
        /// </summary>
        public int BondLevel { get; set; }

        /// <summary>
        /// Timestamp of the block where the staker published his first commitment.
        /// </summary>
        public DateTime BondTime { get; set; }
    }
}
