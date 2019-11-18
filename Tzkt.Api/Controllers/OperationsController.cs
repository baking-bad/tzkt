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
        #endregion

        #region proposals
        [HttpGet("proposals")]
        public Task<IEnumerable<ProposalOperation>> GetProposals([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetProposals(n, p * n);
        }

        [HttpGet("proposals/{hash}")]
        public Task<ProposalOperation> GetProposal([OpHash] string hash)
        {
            return Operations.GetProposal(hash);
        }
        #endregion

        #region ballots
        [HttpGet("ballots")]
        public Task<IEnumerable<BallotOperation>> GetBallots([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetBallots(n, p * n);
        }

        [HttpGet("ballots/{hash}")]
        public Task<BallotOperation> GetBallot([OpHash] string hash)
        {
            return Operations.GetBallot(hash);
        }
        #endregion

        #region activations
        [HttpGet("activations")]
        public Task<IEnumerable<ActivationOperation>> GetActivations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetActivations(n, p * n);
        }

        [HttpGet("activations/{hash}")]
        public Task<ActivationOperation> GetActivation([OpHash] string hash)
        {
            return Operations.GetActivation(hash);
        }
        #endregion

        #region double baking
        [HttpGet("double_baking")]
        public Task<IEnumerable<DoubleBakingOperation>> GetDoubleBakings([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetDoubleBakings(n, p * n);
        }

        [HttpGet("double_baking/{hash}")]
        public Task<DoubleBakingOperation> GetDoubleBaking([OpHash] string hash)
        {
            return Operations.GetDoubleBaking(hash);
        }
        #endregion

        #region double endorsing
        [HttpGet("double_endorsing")]
        public Task<IEnumerable<DoubleEndorsingOperation>> GetDoubleEndorsings([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetDoubleEndorsings(n, p * n);
        }

        [HttpGet("double_endorsing/{hash}")]
        public Task<DoubleEndorsingOperation> GetDoubleEndorsing([OpHash] string hash)
        {
            return Operations.GetDoubleEndorsing(hash);
        }
        #endregion

        #region nonce revelations
        [HttpGet("nonce_revelations")]
        public Task<IEnumerable<NonceRevelationOperation>> GetNonceRevelations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetNonceRevelations(n, p * n);
        }

        [HttpGet("nonce_revelations/{hash}")]
        public Task<NonceRevelationOperation> GetNonceRevelation([OpHash] string hash)
        {
            return Operations.GetNonceRevelation(hash);
        }
        #endregion

        #region delegations
        [HttpGet("delegations")]
        public Task<IEnumerable<DelegationOperation>> GetDelegations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetDelegations(n, p * n);
        }

        [HttpGet("delegations/{hash}")]
        public Task<DelegationOperation> GetDelegation([OpHash] string hash)
        {
            return Operations.GetDelegation(hash);
        }
        #endregion

        #region originations
        [HttpGet("originations")]
        public Task<IEnumerable<OriginationOperation>> GetOriginations([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetOriginations(n, p * n);
        }

        [HttpGet("originations/{hash}")]
        public Task<OriginationOperation> GetOrigination([OpHash] string hash)
        {
            return Operations.GetOrigination(hash);
        }
        #endregion

        #region reveals
        [HttpGet("reveals")]
        public Task<IEnumerable<RevealOperation>> GetReveals([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Operations.GetReveals(n, p * n);
        }

        [HttpGet("reveals/{hash}")]
        public Task<RevealOperation> GetReveal([OpHash] string hash)
        {
            return Operations.GetReveal(hash);
        }
        #endregion
    }
}
