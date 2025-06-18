using Tzkt.Api.Models;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Repositories
{
    public partial class AccountRepository
    {
        public async Task<IEnumerable<Activity>> GetActivity(
            HashSet<string> addresses,
            HashSet<string> types,
            ActivityRole roles,
            TimestampParameter? timestamp,
            bool sortAsc,
            long? lastId,
            int limit,
            Symbols quote,
            MichelineFormat format)
        {
            var accounts = new List<RawAccount>(addresses.Count);
            foreach (var address in addresses)
            {
                var account = await Accounts.GetAsync(address);
                if (account != null)
                    accounts.Add(account);
            }

            if (accounts.Count == 0)
                return [];

            var pagination = new Pagination
            {
                sort = sortAsc
                    ? new SortParameter { Asc = "Id" }
                    : new SortParameter { Desc = "Id" },
                offset = lastId != null
                    ? new OffsetParameter { Cr = lastId }
                    : null,
                limit = limit
            };

            var activationOps = types.Contains(ActivityTypes.Activation)
                ? Operations.GetActivationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var autostakingOps = types.Contains(ActivityTypes.Autostaking)
                ? Operations.GetAutostakingOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var bakingOps = types.Contains(ActivityTypes.Baking)
                ? Operations.GetBakingOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var ballotOps = types.Contains(ActivityTypes.Ballot)
                ? Operations.GetBallotOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var dalAttestationRewardOps = types.Contains(ActivityTypes.DalAttestationReward)
                ? Operations.GetDalAttestationRewardOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var dalEntrapmentEvidenceOps = types.Contains(ActivityTypes.DalEntrapmentEvidence)
                ? Operations.GetDalEntrapmentEvidenceOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var dalPublishCommitmentOps = types.Contains(ActivityTypes.DalPublishCommitment)
                ? Operations.GetDalPublishCommitmentOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var delegationOps = types.Contains(ActivityTypes.Delegation)
                ? Operations.GetDelegationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var doubleBakingOps = types.Contains(ActivityTypes.DoubleBaking)
                ? Operations.GetDoubleBakingOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var doubleAttestationOps = types.Contains(ActivityTypes.DoubleAttestation)
                ? Operations.GetDoubleAttestationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var doublePreattestationOps = types.Contains(ActivityTypes.DoublePreattestation)
                ? Operations.GetDoublePreattestationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var drainDelegateOps = types.Contains(ActivityTypes.DrainDelegate)
                ? Operations.GetDrainDelegateOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var attestationOps = types.Contains(ActivityTypes.Attestation)
                ? Operations.GetAttestationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var attestationRewardOps = types.Contains(ActivityTypes.AttestationReward)
                ? Operations.GetAttestationRewardOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var increasePaidStorageOps = types.Contains(ActivityTypes.IncreasePaidStorage)
                ? Operations.GetIncreasePaidStorageOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var migrationOps = types.Contains(ActivityTypes.Migration)
                ? Operations.GetMigrationOpsActivity(accounts, roles, timestamp, pagination, quote, format)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var nonceRevelationOps = types.Contains(ActivityTypes.NonceRevelation)
                ? Operations.GetNonceRevelationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var originationOps = types.Contains(ActivityTypes.Origination)
                ? Operations.GetOriginationOpsActivity(accounts, roles, timestamp, pagination, quote, format)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var preattestationOps = types.Contains(ActivityTypes.Preattestation)
                ? Operations.GetPreattestationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var proposalOps = types.Contains(ActivityTypes.Proposal)
                ? Operations.GetProposalOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var registerConstantOps = types.Contains(ActivityTypes.RegisterConstant)
                ? Operations.GetRegisterConstantOpsActivity(accounts, roles, timestamp, pagination, quote, format)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var revealOps = types.Contains(ActivityTypes.Reveal)
                ? Operations.GetRevealOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var revelationPenaltyOps = types.Contains(ActivityTypes.RevelationPenalty)
                ? Operations.GetRevelationPenaltyOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var setDelegateParametersOps = types.Contains(ActivityTypes.SetDelegateParameters)
                ? Operations.GetSetDelegateParametersOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var setDepositsLimitOps = types.Contains(ActivityTypes.SetDepositsLimit)
                ? Operations.GetSetDepositsLimitOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupAddMessagesOps = types.Contains(ActivityTypes.SmartRollupAddMessages)
                ? Operations.GetSmartRollupAddMessagesOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupCementOps = types.Contains(ActivityTypes.SmartRollupCement)
                ? Operations.GetSmartRollupCementOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupExecuteOps = types.Contains(ActivityTypes.SmartRollupExecute)
                ? Operations.GetSmartRollupExecuteOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupOriginateOps = types.Contains(ActivityTypes.SmartRollupOriginate)
                ? Operations.GetSmartRollupOriginateOpsActivity(accounts, roles, timestamp, pagination, quote, format)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupPublishOps = types.Contains(ActivityTypes.SmartRollupPublish)
                ? Operations.GetSmartRollupPublishOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupRecoverBondOps = types.Contains(ActivityTypes.SmartRollupRecoverBond)
                ? Operations.GetSmartRollupRecoverBondOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var smartRollupRefuteOps = types.Contains(ActivityTypes.SmartRollupRefute)
                ? Operations.GetSmartRollupRefuteOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var stakingOps = types.Contains(ActivityTypes.Staking)
                ? Operations.GetStakingOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var transactionOps = types.Contains(ActivityTypes.Transaction)
                ? Operations.GetTransactionOpsActivity(accounts, roles, timestamp, pagination, quote, format)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var transferTicketOps = types.Contains(ActivityTypes.TransferTicket)
                ? Operations.GetTransferTicketOpsActivity(accounts, roles, timestamp, pagination, quote, format)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var updateSecondaryKeyOps = types.Contains(ActivityTypes.UpdateSecondaryKey)
                ? Operations.GetUpdateSecondaryKeyOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var vdfRevelationOps = types.Contains(ActivityTypes.VdfRevelation)
                ? Operations.GetVdfRevelationOpsActivity(accounts, roles, timestamp, pagination, quote)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var ticketTransfers = types.Contains(ActivityTypes.TicketTransfer)
                ? Tickets.GetTicketTransfersActivity(accounts, roles, timestamp, pagination)
                : Task.FromResult(Enumerable.Empty<Activity>());

            var tokenTransfers = types.Contains(ActivityTypes.TokenTransfer)
                ? Tokens.GetTokenTransfersActivity(accounts, roles, timestamp, pagination)
                : Task.FromResult(Enumerable.Empty<Activity>());

            await Task.WhenAll(
                activationOps,
                autostakingOps,
                bakingOps,
                ballotOps,
                dalAttestationRewardOps,
                dalEntrapmentEvidenceOps,
                dalPublishCommitmentOps,
                delegationOps,
                doubleBakingOps,
                doubleAttestationOps,
                doublePreattestationOps,
                drainDelegateOps,
                attestationOps,
                attestationRewardOps,
                increasePaidStorageOps,
                migrationOps,
                nonceRevelationOps,
                originationOps,
                preattestationOps,
                proposalOps,
                registerConstantOps,
                revealOps,
                revelationPenaltyOps,
                setDelegateParametersOps,
                setDepositsLimitOps,
                smartRollupAddMessagesOps,
                smartRollupCementOps,
                smartRollupExecuteOps,
                smartRollupOriginateOps,
                smartRollupPublishOps,
                smartRollupRecoverBondOps,
                smartRollupRefuteOps,
                stakingOps,
                transactionOps,
                transferTicketOps,
                updateSecondaryKeyOps,
                vdfRevelationOps,
                ticketTransfers,
                tokenTransfers);

            var result = activationOps.Result
                .Concat(autostakingOps.Result)
                .Concat(bakingOps.Result)
                .Concat(ballotOps.Result)
                .Concat(dalAttestationRewardOps.Result)
                .Concat(dalEntrapmentEvidenceOps.Result)
                .Concat(dalPublishCommitmentOps.Result)
                .Concat(delegationOps.Result)
                .Concat(doubleBakingOps.Result)
                .Concat(doubleAttestationOps.Result)
                .Concat(doublePreattestationOps.Result)
                .Concat(drainDelegateOps.Result)
                .Concat(attestationOps.Result)
                .Concat(attestationRewardOps.Result)
                .Concat(increasePaidStorageOps.Result)
                .Concat(migrationOps.Result)
                .Concat(nonceRevelationOps.Result)
                .Concat(originationOps.Result)
                .Concat(preattestationOps.Result)
                .Concat(proposalOps.Result)
                .Concat(registerConstantOps.Result)
                .Concat(revealOps.Result)
                .Concat(revelationPenaltyOps.Result)
                .Concat(setDelegateParametersOps.Result)
                .Concat(setDepositsLimitOps.Result)
                .Concat(smartRollupAddMessagesOps.Result)
                .Concat(smartRollupCementOps.Result)
                .Concat(smartRollupExecuteOps.Result)
                .Concat(smartRollupOriginateOps.Result)
                .Concat(smartRollupPublishOps.Result)
                .Concat(smartRollupRecoverBondOps.Result)
                .Concat(smartRollupRefuteOps.Result)
                .Concat(stakingOps.Result)
                .Concat(transactionOps.Result)
                .Concat(transferTicketOps.Result)
                .Concat(updateSecondaryKeyOps.Result)
                .Concat(vdfRevelationOps.Result)
                .Concat(ticketTransfers.Result)
                .Concat(tokenTransfers.Result);

            return sortAsc == false
                ? result.OrderByDescending(x => x.Id).Take(limit)
                : result.OrderBy(x => x.Id).Take(limit);
        }
    }
}
