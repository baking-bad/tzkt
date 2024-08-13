using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Tzkt.Api.Models;
using Tzkt.Api.Repositories;

namespace Tzkt.Api.Controllers
{
    [ApiController]
    [Route("v1/dal")]
    public class DalController : ControllerBase
    {
        private readonly DalRepository Dal;

        public DalController(DalRepository dal)
        {
            Dal = dal;
        }

        #region commiments
        /// <summary>
        /// Get DAL commitments count
        /// </summary>
        /// <remarks>
        /// Returns total number of DAL commitments published.
        /// </remarks>
        /// <param name="hash">Filters by DAL commitment hash</param>
        /// <param name="level">Filters by level</param>
        /// <param name="slotIndex">Filters by slot-index</param>
        /// <param name="publisher">Filters by DAL commitment publisher</param>
        /// <returns></returns>
        [HttpGet("commitments/count")]
        public async Task<int> GetDalCommitmentsCount(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher)
        {
            return await Dal.GetCommitmentsCount(hash, level, slotIndex, publisher);
        }

        /// <summary>
        /// Get DAL commitments information
        /// </summary>
        /// <remarks>
        /// Returns information of the DAL commitments published.
        /// </remarks>
        /// <param name="hash">Filters by DAL commitment hash</param>
        /// <param name="level">Filters by level</param>
        /// <param name="slotIndex">Filters by slot-index</param>
        /// <param name="publisher">Filters by DAL commitment publisher</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts DAL commitments by specified field. Supported fields: `level` (default), `slotIndex`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("commitments")]
        public async Task<ActionResult<IEnumerable<DalCommitment>>> GetDalCommitments(
            DalCommitmentHashParameter hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("level", "slotIndex"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Dal.GetCommitments(hash, level, slotIndex, publisher, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Dal.GetCommitments(hash, level, slotIndex, publisher, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Dal.GetCommitments(hash, level, slotIndex, publisher, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Dal.GetCommitments(hash, level, slotIndex, publisher, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Dal.GetCommitments(hash, level, slotIndex, publisher, sort, offset, limit, select.Fields)
                    });
                }
            }
        }

        /// <summary>
        /// Get DAL commitments information by hash
        /// </summary>
        /// <remarks>
        /// Returns information of the DAL commitments published with the specified hash.
        /// </remarks>
        /// <param name="hash">DAL commitment hash</param>
        /// <param name="level">Filters by level</param>
        /// <param name="slotIndex">Filters by slot-index</param>
        /// <param name="publisher">Filters by DAL commitment publisher</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts DAL commitments by specified field. Supported fields: `level` (default), `slotIndex`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("commitments/{hash}")]
        public async Task<ActionResult<IEnumerable<DalCommitment>>> GetDalCommitmentsByHash(
            [Required][DalCommitmentHash] string hash,
            Int32Parameter level,
            Int32Parameter slotIndex,
            AccountParameter publisher,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
            =>
            await GetDalCommitments(new DalCommitmentHashParameter { Eq = hash }, level, slotIndex, publisher, select, sort, offset, limit);
        #endregion

        #region attestations
        /// <summary>
        /// Get DAL attestations count
        /// </summary>
        /// <remarks>
        /// Returns total number of DAL attestations.
        /// </remarks>
        /// <param name="commitment">Filters by DAL commitment hash</param>
        /// <param name="publishLevel">Filters by level at which the commitment has been published</param>
        /// <param name="slotIndex">Filters by slot-index</param>
        /// <param name="attester">Filters by attester</param>
        /// <param name="shardsCount">Filters by number of shards</param>
        /// <param name="attested">Filters by if the slot has been attested</param>
        /// <returns></returns>
        [HttpGet("attestations/count")]
        public async Task<int> GetDalAttestationsCount(
            DalCommitmentHashParameter commitment,
            Int32Parameter publishLevel,
            Int32Parameter slotIndex,
            Int32Parameter shardsCount,
            AccountParameter attester,
            BoolParameter attested)
        {
            return await Dal.GetAttestationsCount(commitment, publishLevel, slotIndex, attester, shardsCount, attested);
        }

        /// <summary>
        /// Get DAL attestations information
        /// </summary>
        /// <remarks>
        /// Returns information of the DAL attestations.
        /// </remarks>
        /// <param name="commitment">Filters by DAL commitment hash</param>
        /// <param name="publishLevel">Filters by level at which the commitment has been published</param>
        /// <param name="slotIndex">Filters by slot-index</param>
        /// <param name="attester">Filters by attester</param>
        /// <param name="shardsCount">Filters by number of shards</param>
        /// <param name="attested">Filters if the shards are attested</param>
        /// <param name="select">Specify comma-separated list of fields to include into response or leave it undefined to return full object. If you select single field, response will be an array of values in both `.fields` and `.values` modes.</param>
        /// <param name="sort">Sorts DAL commitments by specified field. Supported fields: `publishLevel` (default), `slotIndex`, `shardsCount`.</param>
        /// <param name="offset">Specifies which or how many items should be skipped</param>
        /// <param name="limit">Maximum number of items to return</param>
        /// <returns></returns>
        [HttpGet("attestations")]
        public async Task<ActionResult<IEnumerable<DalAttestationStatus>>> GetDalAttestations(
            DalCommitmentHashParameter commitment,
            Int32Parameter publishLevel,
            Int32Parameter slotIndex,
            AccountParameter attester,
            Int32Parameter shardsCount,
            BoolParameter attested,
            SelectParameter select,
            SortParameter sort,
            OffsetParameter offset,
            [Range(0, 10000)] int limit = 100)
        {
            #region validate
            if (sort != null && !sort.Validate("publishLevel", "slotIndex", "shardsCount"))
                return new BadRequest($"{nameof(sort)}", "Sorting by the specified field is not allowed.");
            #endregion

            if (select == null)
                return Ok(await Dal.GetAttestations(commitment, publishLevel, slotIndex, attester, shardsCount, attested, sort, offset, limit));

            if (select.Values != null)
            {
                if (select.Values.Length == 1)
                    return Ok(await Dal.GetAttestations(commitment, publishLevel, slotIndex, attester, shardsCount, attested, sort, offset, limit, select.Values[0]));
                else
                    return Ok(await Dal.GetAttestations(commitment, publishLevel, slotIndex, attester, shardsCount, attested, sort, offset, limit, select.Values));
            }
            else
            {
                if (select.Fields.Length == 1)
                    return Ok(await Dal.GetAttestations(commitment, publishLevel, slotIndex, attester, shardsCount, attested, sort, offset, limit, select.Fields[0]));
                else
                {
                    return Ok(new SelectionResponse
                    {
                        Cols = select.Fields,
                        Rows = await Dal.GetAttestations(commitment, publishLevel, slotIndex, attester, shardsCount, attested, sort, offset, limit, select.Fields)
                    });
                }
            }
        }
        #endregion
    }
}
