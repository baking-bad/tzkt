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
    [Route("v1/voting")]
    public class VotingController : ControllerBase
    {
        private readonly VotingRepository Voting;
        public VotingController(VotingRepository voting)
        {
            Voting = voting;
        }

        #region proposals
        /// <summary>
        /// Get proposals count
        /// </summary>
        /// <remarks>
        /// Returns the total number of protocol proposals.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("proposals/count")]
        public Task<int> GetDoubleBakingCount()
        {
            return Voting.GetProposalsCount();
        }

        /// <summary>
        /// Get proposals
        /// </summary>
        /// <remarks>
        /// Returns a list of protocol proposals.
        /// </remarks>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts proposals by specified field. Supported fields: `id`, `period`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<Proposal>>> GetProposals(
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "period"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Voting.GetProposals(sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Voting.GetProposals(sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Voting.GetProposals(sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Voting.GetProposals(sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Voting.GetProposals(sort, offset, limit, select.Fields)
                    });
                }
            }
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
