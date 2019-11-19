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
        [HttpGet("proposals/{hash}")]
        public Task<Proposal> GetProposal([ProtocolHash] string hash)
        {
            return Voting.GetProposal(hash);
        }

        [HttpGet("proposals")]
        public Task<IEnumerable<Proposal>> GetProposals([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Voting.GetProposals(n, p * n);
        }
        #endregion

        #region periods
        [HttpGet("periods")]
        public Task<IEnumerable<VotingPeriod>> GetPeriods([Min(0)] int p = 0, [Range(0, 1000)] int n = 100)
        {
            return Voting.GetPeriods(n, p * n);
        }
        #endregion
    }
}
