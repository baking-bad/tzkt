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
        #endregion
    }
}
