﻿namespace Mvkt.Api.Models
{
    public class SplitDelegator
    {
        /// <summary>
        /// Address of the delegator
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Amount delegated to the baker at the snapshot time (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long DelegatedBalance { get; set; }

        /// <summary>
        /// Amount staked to the baker at the snapshot time (micro tez).
        /// </summary>
        public long StakedBalance { get; set; }

        /// <summary>
        /// Amount delegated to the baker at the moment (micro tez).
        /// This amount doesn't include staked amount.
        /// </summary>
        public long CurrentDelegatedBalance { get; set; }

        /// <summary>
        /// Amount staked to the baker at the moment (micro tez).
        /// </summary>
        public long CurrentStakedBalance { get; set; }

        /// <summary>
        /// Indicates whether the delegator is emptied (at the moment, not at the snapshot time).
        /// Emptied accounts (users with zero balance) should be re-allocated, so if you make payment to the emptied account you will pay allocation fee.
        /// </summary>
        public bool Emptied { get; set; }

        #region deprecated
        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long Balance => DelegatedBalance + StakedBalance;

        /// <summary>
        /// [DEPRECATED]
        /// </summary>
        public long CurrentBalance => CurrentDelegatedBalance + CurrentStakedBalance;
        #endregion
    }
}
