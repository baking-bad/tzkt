using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;
using Tzkt.Data;

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

        static async Task<bool?> GetStatus(IDbConnection db, string table, string hash)
        {
            return await db.QueryFirstOrDefaultAsync<bool?>($@"
                SELECT ""Status"" = 1
                FROM   ""{table}""
                WHERE  ""OpHash"" = @hash::character(51)
                LIMIT  1",
            new { hash });
        }

        public async Task<bool?> GetStatus(string hash)
        {
            using var db = GetConnection();
            return await GetStatus(db, nameof(TzktContext.TransactionOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.OriginationOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.DelegationOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.RevealOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.RegisterConstantOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.SetDepositsLimitOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.IncreasePaidStorageOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TransferTicketOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupCommitOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupDispatchTicketsOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupFinalizeCommitmentOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupOriginationOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupRejectionOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupRemoveCommitmentOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupReturnBondOps), hash)
                ?? await GetStatus(db, nameof(TzktContext.TxRollupSubmitBatchOps), hash);
        }

        public async Task<IEnumerable<Operation>> Get(string hash, MichelineFormat format, Symbols quote)
        {
            #region test manager operations
            var delegations = GetDelegations(hash, quote);
            var originations = GetOriginations(hash, format, quote);
            var transactions = GetTransactions(hash, format, quote);
            var reveals = GetReveals(hash, quote);
            var registerConstants = GetRegisterConstants(hash, format, quote);
            var setDepositsLimits = GetSetDepositsLimits(hash, quote);
            var increasePaidStorageOps = GetIncreasePaidStorageOps(hash, quote);
            var transferTicketOps = GetTransferTicketOps(hash, format, quote);
            var txRollupCommitOps = GetTxRollupCommitOps(hash, quote);
            var txRollupDispatchTicketsOps = GetTxRollupDispatchTicketsOps(hash, quote);
            var txRollupFinalizeCommitmentOps = GetTxRollupFinalizeCommitmentOps(hash, quote);
            var txRollupOriginationOps = GetTxRollupOriginationOps(hash, quote);
            var txRollupRejectionOps = GetTxRollupRejectionOps(hash, quote);
            var txRollupRemoveCommitmentOps = GetTxRollupRemoveCommitmentOps(hash, quote);
            var txRollupReturnBondOps = GetTxRollupReturnBondOps(hash, quote);
            var txRollupSubmitBatchOps = GetTxRollupSubmitBatchOps(hash, quote);

            await Task.WhenAll(
                delegations,
                originations,
                transactions,
                reveals,
                registerConstants,
                setDepositsLimits,
                increasePaidStorageOps,
                transferTicketOps,
                txRollupCommitOps,
                txRollupDispatchTicketsOps,
                txRollupFinalizeCommitmentOps,
                txRollupOriginationOps,
                txRollupRejectionOps,
                txRollupRemoveCommitmentOps,
                txRollupReturnBondOps,
                txRollupSubmitBatchOps);

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result)
                .Concat(reveals.Result)
                .Concat(registerConstants.Result)
                .Concat(setDepositsLimits.Result)
                .Concat(increasePaidStorageOps.Result)
                .Concat(transferTicketOps.Result)
                .Concat(txRollupCommitOps.Result)
                .Concat(txRollupDispatchTicketsOps.Result)
                .Concat(txRollupFinalizeCommitmentOps.Result)
                .Concat(txRollupOriginationOps.Result)
                .Concat(txRollupRejectionOps.Result)
                .Concat(txRollupRemoveCommitmentOps.Result)
                .Concat(txRollupReturnBondOps.Result)
                .Concat(txRollupSubmitBatchOps.Result);

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
            var preendorsements = GetPreendorsements(hash, quote);
            var doubleBaking = GetDoubleBakings(hash, quote);
            var doubleEndorsing = GetDoubleEndorsings(hash, quote);
            var doublePreendorsing = GetDoublePreendorsings(hash, quote);
            var nonceRevelation = GetNonceRevelations(hash, quote);
            var vdfRevelation = GetVdfRevelations(hash, quote);

            await Task.WhenAll(endorsements, preendorsements, doubleBaking, doubleEndorsing, doublePreendorsing, nonceRevelation);

            if (endorsements.Result.Any())
                return endorsements.Result;

            if (preendorsements.Result.Any())
                return preendorsements.Result;

            if (doubleBaking.Result.Any())
                return doubleBaking.Result;

            if (doubleEndorsing.Result.Any())
                return doubleEndorsing.Result;

            if (doublePreendorsing.Result.Any())
                return doublePreendorsing.Result;

            if (nonceRevelation.Result.Any())
                return nonceRevelation.Result;

            if (vdfRevelation.Result.Any())
                return vdfRevelation.Result;
            #endregion

            return new List<Operation>(0);
        }

        public async Task<IEnumerable<Operation>> Get(string hash, int counter, MichelineFormat format, Symbols quote)
        {
            var delegations = GetDelegations(hash, counter, quote);
            var originations = GetOriginations(hash, counter, format, quote);
            var transactions = GetTransactions(hash, counter, format, quote);
            var reveals = GetReveals(hash, counter, quote);
            var registerConstants = GetRegisterConstants(hash, counter, format, quote);
            var setDepositsLimits = GetSetDepositsLimits(hash, counter, quote);
            var increasePaidStorageOps = GetIncreasePaidStorageOps(hash, quote);
            var transferTicketOps = GetTransferTicketOps(hash, counter, format, quote);
            var txRollupCommitOps = GetTxRollupCommitOps(hash, counter, quote);
            var txRollupDispatchTicketsOps = GetTxRollupDispatchTicketsOps(hash, counter, quote);
            var txRollupFinalizeCommitmentOps = GetTxRollupFinalizeCommitmentOps(hash, counter, quote);
            var txRollupOriginationOps = GetTxRollupOriginationOps(hash, counter, quote);
            var txRollupRejectionOps = GetTxRollupRejectionOps(hash, counter, quote);
            var txRollupRemoveCommitmentOps = GetTxRollupRemoveCommitmentOps(hash, counter, quote);
            var txRollupReturnBondOps = GetTxRollupReturnBondOps(hash, counter, quote);
            var txRollupSubmitBatchOps = GetTxRollupSubmitBatchOps(hash, counter, quote);

            await Task.WhenAll(
                delegations,
                originations,
                transactions,
                reveals,
                registerConstants,
                setDepositsLimits,
                increasePaidStorageOps,
                transferTicketOps,
                txRollupCommitOps,
                txRollupDispatchTicketsOps,
                txRollupFinalizeCommitmentOps,
                txRollupOriginationOps,
                txRollupRejectionOps,
                txRollupRemoveCommitmentOps,
                txRollupReturnBondOps,
                txRollupSubmitBatchOps);

            if (txRollupSubmitBatchOps.Result.Any())
                return txRollupSubmitBatchOps.Result;

            if (txRollupReturnBondOps.Result.Any())
                return txRollupReturnBondOps.Result;

            if (txRollupRemoveCommitmentOps.Result.Any())
                return txRollupRemoveCommitmentOps.Result;

            if (txRollupRejectionOps.Result.Any())
                return txRollupRejectionOps.Result;

            if (txRollupOriginationOps.Result.Any())
                return txRollupOriginationOps.Result;

            if (txRollupFinalizeCommitmentOps.Result.Any())
                return txRollupFinalizeCommitmentOps.Result;

            if (txRollupDispatchTicketsOps.Result.Any())
                return txRollupDispatchTicketsOps.Result;

            if (txRollupCommitOps.Result.Any())
                return txRollupCommitOps.Result;

            if (increasePaidStorageOps.Result.Any())
                return increasePaidStorageOps.Result;

            if (setDepositsLimits.Result.Any())
                return setDepositsLimits.Result;

            if (registerConstants.Result.Any())
                return registerConstants.Result;

            if (reveals.Result.Any())
                return reveals.Result;

            var managerOps = ((IEnumerable<Operation>)delegations.Result)
                .Concat(originations.Result)
                .Concat(transactions.Result)
                .Concat(transferTicketOps.Result);

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
