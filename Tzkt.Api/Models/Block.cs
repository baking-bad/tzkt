using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tzkt.Api.Models
{
    public class Block
    {
        public int Level { get; set; }
        
        public string Hash { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public int Proto { get; set; }
        
        public int Priority { get; set; }
        
        public int Validations { get; set; }

        public long Reward { get; set; }

        public long Fees { get; set; }

        public bool NonceRevealed { get; set; }

        public Alias Baker { get; set; }

        public IEnumerable<EndorsementOperation> Endorsements { get; set; }

        public IEnumerable<ProposalOperation> Proposals { get; set; }
        public IEnumerable<BallotOperation> Ballots { get; set; }

        public IEnumerable<ActivationOperation> Activations { get; set; }
        public IEnumerable<DoubleBakingOperation> DoubleBaking { get; set; }
        public IEnumerable<DoubleEndorsingOperation> DoubleEndorsing { get; set; }
        public IEnumerable<NonceRevelationOperation> NonceRevelations { get; set; }

        public IEnumerable<DelegationOperation> Delegations { get; set; }
        public IEnumerable<OriginationOperation> Originations { get; set; }
        public IEnumerable<TransactionOperation> Transactions { get; set; }
        public IEnumerable<RevealOperation> Reveals { get; set; }
    }
}
