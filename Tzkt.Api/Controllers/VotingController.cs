using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/voting")]
    public class VotingController : ControllerBase
    {
        private readonly VotingRepository Voting;
        private readonly StateCache State;

        public VotingController(VotingRepository voting, StateCache state)
        {
            Voting = voting;
            State = state;
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
        public Task<int> GetProposalsCount()
        {
            return Task.FromResult(State.Current.ProposalsCount);
        }

        /// <summary>
        /// Get proposals
        /// </summary>
        /// <remarks>
        /// Returns a list of protocol proposals.
        /// </remarks>
        /// <param name="hash">Filters proposals by hash</param>
        /// <param name="epoch">Filters proposals by voting epoch</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts proposals by specified field. Supported fields: `id` (default), `upvotes`, `votingPower`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("proposals")]
        public async Task<ActionResult<IEnumerable<Proposal>>> GetProposals(
            ProtocolParameter hash,
            Int32Parameter epoch,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "upvotes", "votingPower"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Voting.GetProposals(hash, epoch, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Voting.GetProposals(hash, epoch, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Voting.GetProposals(hash, epoch, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Voting.GetProposals(hash, epoch, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Voting.GetProposals(hash, epoch, sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get proposal by hash
        /// </summary>
        /// <remarks>
        /// Returns the most recent protocol proposal with the specified hash.
        /// </remarks>
        /// <param name="hash">Proposal hash</param>
        /// <returns></returns>
        [HttpGet("proposals/{hash}")]
        public Task<Proposal> GetProposalByHash([Required][ProtocolHash] string hash)
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
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts voting periods by specified field. Supported fields: `id` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("periods")]
        public async Task<ActionResult<IEnumerable<VotingPeriod>>> GetPeriods(
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Voting.GetPeriods(sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Voting.GetPeriods(sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Voting.GetPeriods(sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Voting.GetPeriods(sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Voting.GetPeriods(sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get voting period by index
        /// </summary>
        /// <remarks>
        /// Returns a voting period at the specified index.
        /// </remarks>
        /// <param name="index">Voting period index starting from zero</param>
        /// <returns></returns>
        [HttpGet("periods/{index:int}")]
        public Task<VotingPeriod> GetPeriod([Min(0)] int index)
        {
            return Voting.GetPeriod(index);
        }

        /// <summary>
        /// Get current voting period
        /// </summary>
        /// <remarks>
        /// Returns current voting period.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("periods/current")]
        public Task<VotingPeriod> GetCurrentPeriod()
        {
            return Voting.GetPeriod(State.Current.VotingPeriod);
        }

        /// <summary>
        /// Get period voters
        /// </summary>
        /// <remarks>
        /// Returns voters from the voting period at the specified index.
        /// </remarks>
        /// <param name="index">Voting period index starting from zero</param>
        /// <param name="status">Filters voters by status (`none`, `upvoted`, `voted_yay`, `voted_nay`, `voted_pass`)</param>
        /// <param name="sort">Sorts voters by specified field. Supported fields: `id` (default), `votingPower`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("periods/{index:int}/voters")]
        public async Task<ActionResult<IEnumerable<VoterSnapshot>>> GetPeriodVoters(
            [Min(0)] int index,
            VoterStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "votingPower"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            return Ok(await Voting.GetVoters(index, status, sort, offset, limit));
        }

        /// <summary>
        /// Get period voter
        /// </summary>
        /// <remarks>
        /// Returns a voter with the specified address from the voting period at the specified index.
        /// </remarks>
        /// <param name="index">Voting period index starting from zero</param>
        /// <param name="address">Voter address</param>
        /// <returns></returns>
        [HttpGet("periods/{index:int}/voters/{address}")]
        public Task<VoterSnapshot> GetPeriodVoter([Min(0)] int index, [Required][TzAddress] string address)
        {
            return Voting.GetVoter(index, address);
        }

        /// <summary>
        /// Get current period voters
        /// </summary>
        /// <remarks>
        /// Returns voters from the current period.
        /// </remarks>
        /// <param name="status">Filters voters by status (`none`, `upvoted`, `voted_yay`, `voted_nay`, `voted_pass`)</param>
        /// <param name="sort">Sorts voters by specified field. Supported fields: `id` (default), `votingPower`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("periods/current/voters")]
        public async Task<ActionResult<IEnumerable<VoterSnapshot>>> GetPeriodVoters(
            VoterStatusParameter status,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id", "votingPower"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            return Ok(await Voting.GetVoters(State.Current.VotingPeriod, status, sort, offset, limit));
        }

        /// <summary>
        /// Get current period voter
        /// </summary>
        /// <remarks>
        /// Returns a voter with the specified address from the current period.
        /// </remarks>
        /// <param name="address">Voter address</param>
        /// <returns></returns>
        [HttpGet("periods/current/voters/{address}")]
        public Task<VoterSnapshot> GetPeriodVoter([Required][TzAddress] string address)
        {
            return Voting.GetVoter(State.Current.VotingPeriod, address);
        }
        #endregion

        #region epochs
        /// <summary>
        /// Get voting epochs
        /// </summary>
        /// <remarks>
        /// Returns a list of voting epochs.
        /// </remarks>
        /// <param name="sort">Sorts voting epochs by specified field. Supported fields: `id` (default).</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("epochs")]
        public async Task<ActionResult<IEnumerable<VotingEpoch>>> GetEpochs(
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("id"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            return Ok(await Voting.GetEpochs(sort, offset, limit));
        }

        /// <summary>
        /// Get voting epoch by index
        /// </summary>
        /// <remarks>
        /// Returns a voting epoch at the specified index.
        /// </remarks>
        /// <param name="index">Voting epoch index starting from zero</param>
        /// <returns></returns>
        [HttpGet("epochs/{index:int}")]
        public Task<VotingEpoch> GetEpoch([Min(0)] int index)
        {
            return Voting.GetEpoch(index);
        }

        /// <summary>
        /// Get current voting epoch
        /// </summary>
        /// <remarks>
        /// Returns the current voting epoch
        /// </remarks>
        /// <returns></returns>
        [HttpGet("epochs/current")]
        public Task<VotingEpoch> GetCurrentEpoch()
        {
            return Voting.GetEpoch(State.Current.VotingEpoch);
        }

        /// <summary>
        /// Get latest voting
        /// </summary>
        /// <remarks>
        /// Returns the latest epoch with at least one proposal
        /// </remarks>
        /// <returns></returns>
        [HttpGet("epochs/latest_voting")]
        public Task<VotingEpoch> GetLatestVoting()
        {
            return Voting.GetLatestVoting();
        }
        #endregion
    }
}
