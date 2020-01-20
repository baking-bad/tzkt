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
    public class VotingController : ControllerBase
    {
        private readonly VotingRepository Voting;
        public VotingController(VotingRepository voting)
        {
            Voting = voting;
        }

        #region proposals
        /// <summary>
        /// Get proposals
        /// </summary>
        /// <remarks>
        /// Returns a list of protocol proposals.
        /// </remarks>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet("proposals")]
        public Task<IEnumerable<Proposal>> GetProposals([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Voting.GetProposals(n, p * n);
        }

        /// <summary>
        /// Get proposal by hash
        /// </summary>
        /// <remarks>
        /// Returns a protocol proposal with the specified hash.
        /// </remarks>
        /// <param name="hash">Proposal hash</param>
        /// <returns></returns>
        [HttpGet("proposals/{hash}")]
        public Task<Proposal> GetProposalByHash([ProtocolHash] string hash)
        {
            return Voting.GetProposal(hash);
        }
        #endregion

        #region periods
        /// <summary>
        /// Get voting periods
        /// </summary>
        /// <remarks>
        /// Returns a list of voting periods.
        /// </remarks>
        /// <param name="p">Page offset (pagination)</param>
        /// <param name="n">Number of items to return</param>
        /// <returns></returns>
        [HttpGet("periods")]
        public Task<IEnumerable<VotingPeriod>> GetPeriods([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Voting.GetPeriods(n, p * n);
        }
        #endregion
    }
}
