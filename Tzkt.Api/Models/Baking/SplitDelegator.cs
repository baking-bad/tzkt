namespace Tzkt.Api.Models
{
    public class SplitDelegator
    {
        /// <summary>
        /// Address of the delegator
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Balance of the delegator at the snapshot time
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// Balance of the delegator at the moment
        /// </summary>
        public long CurrentBalance { get; set; }

        /// <summary>
        /// Indicates whether the delegator is emptied (at the moment, not at the snapshot time).
        /// Emptied accounts (users with zero balance) should be re-allocated, so if you make payment to emptied account you will pay (burn) `0.257 tez` allocation fee.
        /// </summary>
        public bool Emptied { get; set; }
    }
}
