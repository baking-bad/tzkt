using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OperationsController : ControllerBase
    {
        private readonly OperationRepository Operations;
        public OperationsController(OperationRepository operations)
        {
            Operations = operations;
        }

        #region operations
        [HttpGet("{hash}")]
        public Task<IEnumerable<Operation>> Get([OpHash] string hash)
        {
            return Operations.Get(hash);
        }

        [HttpGet("{hash}/{counter}")]
        public Task<IEnumerable<Operation>> Get([OpHash] string hash, [Min(0)] int counter)
        {
            return Operations.Get(hash, counter);
        }

        [HttpGet("{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<Operation>> Get([OpHash] string hash, [Min(0)] int counter, [Min(0)] int nonce)
        {
            return Operations.Get(hash, counter, nonce);
        }
        #endregion

        #region endorsements
        [HttpGet("endorsements")]
        public Task<IEnumerable<EndorsementOperation>> GetEndorsements([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetEndorsements(n, p * n);
        }

        [HttpGet("endorsements/{hash}")]
        public Task<IEnumerable<EndorsementOperation>> GetEndorsement([OpHash] string hash)
        {
            return Operations.GetEndorsements(hash);
        }

        [HttpGet("endorsements/count")]
        public Task<int> GetEndorsementsCount()
        {
            return Operations.GetEndorsementsCount();
        }
        #endregion

        #region proposals
        [HttpGet("proposals")]
        public Task<IEnumerable<ProposalOperation>> GetProposals([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetProposals(n, p * n);
        }

        [HttpGet("proposals/{hash}")]
        public Task<IEnumerable<ProposalOperation>> GetProposal([OpHash] string hash)
        {
            return Operations.GetProposals(hash);
        }

        [HttpGet("proposals/count")]
        public Task<int> GetProposalsCount()
        {
            return Operations.GetProposalsCount();
        }
        #endregion

        #region ballots
        [HttpGet("ballots")]
        public Task<IEnumerable<BallotOperation>> GetBallots([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetBallots(n, p * n);
        }

        [HttpGet("ballots/{hash}")]
        public Task<IEnumerable<BallotOperation>> GetBallot([OpHash] string hash)
        {
            return Operations.GetBallots(hash);
        }

        [HttpGet("ballots/count")]
        public Task<int> GetBallotsCount()
        {
            return Operations.GetBallotsCount();
        }
        #endregion

        #region activations
        [HttpGet("activations")]
        public Task<IEnumerable<ActivationOperation>> GetActivations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetActivations(n, p * n);
        }

        [HttpGet("activations/{hash}")]
        public Task<IEnumerable<ActivationOperation>> GetActivation([OpHash] string hash)
        {
            return Operations.GetActivations(hash);
        }

        [HttpGet("activations/count")]
        public Task<int> GetActivationsCount()
        {
            return Operations.GetActivationsCount();
        }
        #endregion

        #region double baking
        [HttpGet("double_baking")]
        public Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetDoubleBakings(n, p * n);
        }

        [HttpGet("double_baking/{hash}")]
        public Task<IEnumerable<DoubleBakingOperation>> GetDoubleBaking([OpHash] string hash)
        {
            return Operations.GetDoubleBakings(hash);
        }

        [HttpGet("double_baking/count")]
        public Task<int> GetDoubleBakingsCount()
        {
            return Operations.GetDoubleBakingsCount();
        }
        #endregion

        #region double endorsing
        [HttpGet("double_endorsing")]
        public Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetDoubleEndorsings(n, p * n);
        }

        [HttpGet("double_endorsing/{hash}")]
        public Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsing([OpHash] string hash)
        {
            return Operations.GetDoubleEndorsings(hash);
        }

        [HttpGet("double_endorsing/count")]
        public Task<int> GetDoubleEndorsingsCount()
        {
            return Operations.GetDoubleEndorsingsCount();
        }
        #endregion

        #region nonce revelations
        [HttpGet("nonce_revelations")]
        public Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetNonceRevelations(n, p * n);
        }

        [HttpGet("nonce_revelations/{hash}")]
        public Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelation([OpHash] string hash)
        {
            return Operations.GetNonceRevelations(hash);
        }

        [HttpGet("nonce_revelations/count")]
        public Task<int> GetNonceRevelationsCount()
        {
            return Operations.GetNonceRevelationsCount();
        }
        #endregion

        #region revelation penalties
        [HttpGet("revelation_penalties")]
        public Task<IEnumerable<RevelationPenaltyOperation>> GetRevelationPenalties([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetRevelationPenalties(n, p * n);
        }

        [HttpGet("revelation_penalties/count")]
        public Task<int> GetRevelationPenaltiesCount()
        {
            return Operations.GetRevelationPenaltiesCount();
        }
        #endregion

        #region delegations
        [HttpGet("delegations")]
        public Task<IEnumerable<DelegationOperation>> GetDelegations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetDelegations(n, p * n);
        }

        [HttpGet("delegations/{hash}")]
        public Task<IEnumerable<DelegationOperation>> GetDelegation([OpHash] string hash)
        {
            return Operations.GetDelegations(hash);
        }

        [HttpGet("delegations/count")]
        public Task<int> GetDelegationsCount()
        {
            return Operations.GetDelegationsCount();
        }
        #endregion

        #region originations
        [HttpGet("originations")]
        public Task<IEnumerable<OriginationOperation>> GetOriginations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetOriginations(n, p * n);
        }

        [HttpGet("originations/{hash}")]
        public Task<IEnumerable<OriginationOperation>> GetOrigination([OpHash] string hash)
        {
            return Operations.GetOriginations(hash);
        }

        [HttpGet("originations/count")]
        public Task<int> GetOriginationsCount()
        {
            return Operations.GetOriginationsCount();
        }
        #endregion

        #region transactions
        [HttpGet("transactions")]
        public Task<IEnumerable<TransactionOperation>> GetTransactions([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetTransactions(n, p * n);
        }

        [HttpGet("transactions/{hash}")]
        public Task<IEnumerable<TransactionOperation>> GetTransaction([OpHash] string hash)
        {
            return Operations.GetTransactions(hash);
        }

        [HttpGet("transactions/{hash}/{counter}")]
        public Task<IEnumerable<TransactionOperation>> GetTransaction([OpHash] string hash, [Min(0)] int counter)
        {
            return Operations.GetTransactions(hash, counter);
        }

        [HttpGet("transactions/{hash}/{counter}/{nonce}")]
        public Task<IEnumerable<TransactionOperation>> GetTransaction([OpHash] string hash, [Min(0)] int counter, [Min(0)] int nonce)
        {
            return Operations.GetTransactions(hash, counter, nonce);
        }

        [HttpGet("transactions/count")]
        public Task<int> GetTransactionsCount()
        {
            return Operations.GetTransactionsCount();
        }
        #endregion

        #region reveals
        [HttpGet("reveals")]
        public Task<IEnumerable<RevealOperation>> GetReveals([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetReveals(n, p * n);
        }

        [HttpGet("reveals/{hash}")]
        public Task<IEnumerable<RevealOperation>> GetReveal([OpHash] string hash)
        {
            return Operations.GetReveals(hash);
        }

        [HttpGet("reveals/count")]
        public Task<int> GetRevealsCount()
        {
            return Operations.GetRevealsCount();
        }
        #endregion

        #region system
        [HttpGet("system")]
        public Task<IEnumerable<SystemOperation>> GetSystemOps([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetSystemOps(n, p * n);
        }

        [HttpGet("system/count")]
        public Task<int> GetSystemOpsCount()
        {
            return Operations.GetSystemOpsCount();
        }
        #endregion

        #region baking
        [HttpGet("baking")]
        public Task<IEnumerable<BakingOperation>> GetBaking([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetBakings(n, p * n);
        }

        [HttpGet("baking/count")]
        public Task<int> GetBakingCount()
        {
            return Operations.GetBakingsCount();
        }
        #endregion
    }
}
