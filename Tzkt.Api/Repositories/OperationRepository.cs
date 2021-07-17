using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class OperationRepository : DbConnection
    {
        readonly AccountsCache Accounts;
        readonly QuotesCache Quotes;

        public OperationRepository(AccountsCache accounts, QuotesCache quotes, IConfiguration config) : base(config)
        {
            Accounts = accounts;
            Quotes = quotes;
        }

        public async Task<IEnumerable<Operation>> Get(string hash, MichelineFormat format, Symbols quote)
        {
            #region test manager operations
            var delegations = GetDelegations(hash, quote);
            var originations = GetOriginations(hash, format, quote);
            var transactions = GetTransactions(hash, format, quote);
            var reveals = GetReveals(hash, quote);

            await Task.WhenAll(delegations, originations, transactions, reveals);

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result)
                .Concat(reveals.Result);

            if (managerOps.Any())
                return managerOps.OrderBy(x => x.Id);
            #endregion

            #region less likely
            var activations = GetActivations(hash, quote);
            var proposals = GetProposals(hash, quote);
            var ballots = GetBallots(hash, quote);

            await Task.WhenAll(activations, proposals, ballots);

            if (activations.Result.Any())
                return activations.Result;

            if (proposals.Result.Any())
                return proposals.Result;

            if (ballots.Result.Any())
                return ballots.Result;
            #endregion

            #region very unlikely
            var endorsements = GetEndorsements(hash, quote);
            var dobleBaking = GetDoubleBakings(hash, quote);
            var doubleEndorsing = GetDoubleEndorsings(hash, quote);
            var nonceRevelation = GetNonceRevelations(hash, quote);

            await Task.WhenAll(endorsements, dobleBaking, doubleEndorsing, nonceRevelation);

            if (endorsements.Result.Any())
                return endorsements.Result;

            if (dobleBaking.Result.Any())
                return dobleBaking.Result;

            if (doubleEndorsing.Result.Any())
                return doubleEndorsing.Result;

            if (nonceRevelation.Result.Any())
                return nonceRevelation.Result;
            #endregion

            return new List<Operation>(0);
        }

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var delegations = GetDelegations(hash, counter, quote);
            var originations = GetOriginations(hash, counter, format, quote);
            var transactions = GetTransactions(hash, counter, format, quote);
            var reveals = GetReveals(hash, counter, quote);

            await Task.WhenAll(delegations, originations, transactions, reveals);

            if (reveals.Result.Any())
                return reveals.Result;

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result);

            if (managerOps.Any())
                return managerOps.OrderBy(x => x.Id);

            return new List<Operation>(0);
        }

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, int nonce, MichelineFormat format, Symbols quote)
        {
            var delegations = GetDelegations(hash, counter, nonce, quote);
            var originations = GetOriginations(hash, counter, nonce, format, quote);
            var transactions = GetTransactions(hash, counter, nonce, format, quote);

            await Task.WhenAll(delegations, originations, transactions);

            if (delegations.Result.Any())
                return delegations.Result;

            if (originations.Result.Any())
                return originations.Result;

            if (transactions.Result.Any())
                return transactions.Result;

            return new List<Operation>(0);
        }
    }
}
