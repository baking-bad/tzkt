using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public async Task<ActionResult<IEnumerable<EndorsementOperation>>> GetEndorsements(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetEndorsements(n, p * n));
        }

        [HttpGet("endorsements/{hash}")]
        public async Task<ActionResult<EndorsementOperation>> GetEndorsement(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetEndorsement(hash));
        }
        #endregion

        #region proposals
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<ProposalOperation>>> GetProposals(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetProposals(n, p * n));
        }

        [HttpGet("proposals/{hash}")]
        public async Task<ActionResult<ProposalOperation>> GetProposal(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetProposal(hash));
        }
        #endregion

        #region ballots
        [HttpGet("ballots")]
        public async Task<ActionResult<IEnumerable<BallotOperation>>> GetBallots(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetBallots(n, p * n));
        }

        [HttpGet("ballots/{hash}")]
        public async Task<ActionResult<BallotOperation>> GetBallot(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetBallot(hash));
        }
        #endregion

        #region activations
        [HttpGet("activations")]
        public async Task<ActionResult<IEnumerable<ActivationOperation>>> GetActivations(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetActivations(n, p * n));
        }

        [HttpGet("activations/{hash}")]
        public async Task<ActionResult<ActivationOperation>> GetActivation(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetActivation(hash));
        }
        #endregion

        #region double baking
        [HttpGet("double_baking")]
        public async Task<ActionResult<IEnumerable<DoubleBakingOperation>>> GetDoubleBakings(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetDoubleBakings(n, p * n));
        }

        [HttpGet("double_baking/{hash}")]
        public async Task<ActionResult<DoubleBakingOperation>> GetDoubleBaking(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetDoubleBaking(hash));
        }
        #endregion

        #region double endorsing
        [HttpGet("double_endorsing")]
        public async Task<ActionResult<IEnumerable<DoubleEndorsingOperation>>> GetDoubleEndorsings(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetDoubleEndorsings(n, p * n));
        }

        [HttpGet("double_endorsing/{hash}")]
        public async Task<ActionResult<DoubleEndorsingOperation>> GetDoubleEndorsing(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetDoubleEndorsing(hash));
        }
        #endregion

        #region nonce revelations
        [HttpGet("nonce_revelations")]
        public async Task<ActionResult<IEnumerable<NonceRevelationOperation>>> GetNonceRevelations(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetNonceRevelations(n, p * n));
        }

        [HttpGet("nonce_revelations/{hash}")]
        public async Task<ActionResult<NonceRevelationOperation>> GetNonceRevelation(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetNonceRevelation(hash));
        }
        #endregion

        #region delegations
        [HttpGet("delegations")]
        public async Task<ActionResult<IEnumerable<DelegationOperation>>> GetDelegations(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetDelegations(n, p * n));
        }

        [HttpGet("delegations/{hash}")]
        public async Task<ActionResult<DelegationOperation>> GetDelegation(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetDelegation(hash));
        }
        #endregion

        #region originations
        [HttpGet("originations")]
        public async Task<ActionResult<IEnumerable<OriginationOperation>>> GetOriginations(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetOriginations(n, p * n));
        }

        [HttpGet("originations/{hash}")]
        public async Task<ActionResult<OriginationOperation>> GetOrigination(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetOrigination(hash));
        }
        #endregion

        #region reveals
        [HttpGet("reveals")]
        public async Task<ActionResult<IEnumerable<RevealOperation>>> GetReveals(int p = 0, int n = 100)
        {
            if (n > 1000)
                return new BadRequest(nameof(n), "Maximum value is 1000");

            if (n < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            if (p < 0)
                return new BadRequest(nameof(n), "Minimum value is 0");

            return Ok(await Operations.GetReveals(n, p * n));
        }

        [HttpGet("reveals/{hash}")]
        public async Task<ActionResult<RevealOperation>> GetReveal(string hash)
        {
            if (!Regex.IsMatch(hash, "^o[0-9A-z]{50}$"))
                return new BadRequest(nameof(hash), "Invalid operation hash");

            return Ok(await Operations.GetReveal(hash));
        }
        #endregion
    }
}
