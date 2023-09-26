using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Netezos.Contracts;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto18
{
    class StakingCommit : ProtocolCommit
    {
        #region static
        public static readonly HashSet<string> Entrypoints = new() { "stake", "unstake", "finalize_unstake", "set_delegate_parameters" };
        static readonly Schema DelegateParametersSchema = Schema.Create(Micheline.FromJson("""
            {
                "prim": "pair",
                "args": [
                    {
                        "prim": "int"
                    },
                    {
                        "prim": "int"
                    },
                    {
                        "prim": "unit"
                    }
                ]
            }
            """) as MichelinePrim);
        #endregion

        public StakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(Block block, JsonElement op, JsonElement content)
        {
            #region init
            var sender = await Cache.Accounts.GetAsync(content.RequiredString("source")) as User;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            var result = content.Required("metadata").Required("operation_result");
            var operation = new StakingOperation
            {
                Id = Cache.AppState.NextOperationId(),
                OpHash = op.RequiredString("hash"),
                Block = block,
                Level = block.Level,
                Timestamp = block.Timestamp,
                BakerFee = content.RequiredInt64("fee"),
                Counter = content.RequiredInt32("counter"),
                GasLimit = content.RequiredInt32("gas_limit"),
                StorageLimit = content.RequiredInt32("storage_limit"),
                Sender = sender,
                SenderId = sender.Id,
                Status = result.RequiredString("status") switch
                {
                    "applied" => OperationStatus.Applied,
                    "backtracked" => OperationStatus.Backtracked,
                    "failed" => OperationStatus.Failed,
                    "skipped" => OperationStatus.Skipped,
                    _ => throw new NotImplementedException()
                },
                Errors = result.TryGetProperty("errors", out var errors)
                    ? OperationErrors.Parse(content, errors)
                    : null,
                GasUsed = (int)(((result.OptionalInt64("consumed_milligas") ?? 0) + 999) / 1000)
            };

            switch (content.Required("parameters").RequiredString("entrypoint"))
            {
                case "stake":
                    operation.Kind = StakingOperationKind.Stake;
                    operation.Amount = content.RequiredInt64("amount");
                    operation.BakerId = senderDelegate?.Id;
                    operation.Pseudotokens = null;
                    break;
                case "unstake":
                    var unstakedAmount = 0L;
                    if (operation.Status == OperationStatus.Applied)
                    {
                        if (result.TryGetProperty("balance_updates", out var unstakeUpdates))
                        {
                            var unstakeUpdate = unstakeUpdates
                                .EnumerateArray()
                                .FirstOrDefault(x => x.RequiredString("category") == "unstaked_deposits");

                            if (unstakeUpdate.ValueKind != JsonValueKind.Undefined)
                                unstakedAmount = unstakeUpdate.RequiredInt64("change");
                        }
                    }
                    else
                    {
                        var amount = content.Required("parameters").Required("value").RequiredBigInteger("int");
                        unstakedAmount = amount >= long.MaxValue ? long.MaxValue : (long)amount;
                    }
                    operation.Kind = StakingOperationKind.Unstake;
                    operation.Amount = unstakedAmount;
                    operation.BakerId = senderDelegate?.Id;
                    operation.Pseudotokens = null;
                    operation.PrevStakedBalance = null;
                    break;
                case "finalize_unstake":
                    var finalizedAmount = 0L;
                    var firstCycle = (int?)null;
                    var lastCycle = (int?)null;
                    if (result.TryGetProperty("balance_updates", out var updates))
                    {
                        var freezerUpdates = updates.EnumerateArray().Where(x => x.RequiredString("kind") == "freezer");
                        if (freezerUpdates.Any())
                        {
                            finalizedAmount = freezerUpdates.Sum(x => -x.RequiredInt64("change"));
                            firstCycle = freezerUpdates.Min(x => x.RequiredInt32("cycle"));
                            lastCycle = freezerUpdates.Max(x => x.RequiredInt32("cycle"));
                        }
                    }
                    operation.Kind = StakingOperationKind.FinalizeUnstake;
                    operation.Amount = finalizedAmount;
                    operation.FirstCycleUnstaked = firstCycle;
                    operation.LastCycleUnstaked = lastCycle;
                    break;
                case "set_delegate_parameters":
                    var param = DelegateParametersSchema.Optimize(Micheline.FromJson(content.Required("parameters").Required("value")));
                    var limit = ((param as MichelinePrim).Args[0] as MichelineInt).Value;
                    var edge = (((param as MichelinePrim).Args[1] as MichelinePrim).Args[0] as MichelineInt).Value;
                    operation.Kind = StakingOperationKind.SetDelegateParameter;
                    operation.LimitOfStakingOverBaking = limit >= 999_999_999_999 ? 999_999_999_999 : (long)limit;
                    operation.EdgeOfBakingOverStaking = (long)edge;
                    operation.ActivationCycle = block.Cycle + block.Protocol.PreservedCycles + 1;
                    break;
                default:
                    throw new NotImplementedException();
            }
            #endregion

            #region apply operation
            Db.TryAttach(sender);
            sender.Balance -= operation.BakerFee;
            sender.Counter = operation.Counter;
            sender.StakingOpsCount++;

            if (senderDelegate != null)
            {
                Db.TryAttach(senderDelegate);
                senderDelegate.StakingBalance -= operation.BakerFee;
                if (senderDelegate != sender)
                {
                    senderDelegate.DelegatedBalance -= operation.BakerFee;
                    senderDelegate.StakingOpsCount++;
                }
            }

            block.Proposer.Balance += operation.BakerFee;
            block.Proposer.StakingBalance += operation.BakerFee;

            block.Operations |= Operations.Staking;
            block.Fees += operation.BakerFee;

            Cache.AppState.Get().StakingOpsCount++;
            #endregion

            #region apply result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.Kind == StakingOperationKind.Stake)
                {
                    if (sender != senderDelegate)
                    {
                        operation.Pseudotokens = senderDelegate.StakingPseudotokens == 0
                            ? operation.Amount.Value
                            : (long)((BigInteger)senderDelegate.StakingPseudotokens * operation.Amount.Value / senderDelegate.ExternalStakedBalance);

                        sender.StakingPseudotokens += operation.Pseudotokens.Value;
                        senderDelegate.StakingPseudotokens += operation.Pseudotokens.Value;
                        senderDelegate.ExternalStakedBalance += operation.Amount.Value;
                        if (sender.StakingPseudotokens == operation.Pseudotokens.Value)
                            senderDelegate.StakersCount++;
                    }
                    sender.StakedBalance += operation.Amount.Value;
                    senderDelegate.TotalStakedBalance += operation.Amount.Value;

                    Cache.Statistics.Current.TotalFrozen += operation.Amount.Value;
                }
                else if (operation.Kind == StakingOperationKind.Unstake)
                {
                    if (sender != senderDelegate)
                    {
                        var newStakedBalance = (long)((BigInteger)senderDelegate.ExternalStakedBalance * sender.StakingPseudotokens / senderDelegate.StakingPseudotokens);
                        operation.PrevStakedBalance = sender.StakedBalance;

                        var rewards = newStakedBalance - sender.StakedBalance;
                        sender.StakedBalance = newStakedBalance;
                        sender.Balance += rewards;
                        senderDelegate.DelegatedBalance += rewards;

                        var pseudotokens = (long)((BigInteger)senderDelegate.StakingPseudotokens * operation.Amount.Value / senderDelegate.ExternalStakedBalance);
                        operation.Pseudotokens = pseudotokens;
                        
                        sender.StakingPseudotokens -= pseudotokens;
                        senderDelegate.StakingPseudotokens -= pseudotokens;
                        senderDelegate.ExternalStakedBalance -= operation.Amount.Value;
                        senderDelegate.ExternalUnstakedBalance += operation.Amount.Value;
                        if (sender.StakingPseudotokens == 0)
                            senderDelegate.StakersCount--;
                    }
                    sender.UnstakedBalance += operation.Amount.Value;
                    sender.StakedBalance -= operation.Amount.Value;
                    senderDelegate.TotalStakedBalance -= operation.Amount.Value;

                    Cache.Statistics.Current.TotalFrozen -= operation.Amount.Value;
                }
                else if (operation.Kind == StakingOperationKind.FinalizeUnstake)
                {
                    if (operation.Amount > 0)
                    {
                        var startCycleProto = await Cache.Protocols.FindByCycleAsync(operation.FirstCycleUnstaked.Value);
                        var startLevel = startCycleProto.GetCycleStart(operation.FirstCycleUnstaked.Value);

                        var endCycleProto = await Cache.Protocols.FindByCycleAsync(operation.LastCycleUnstaked.Value);
                        var endLevel = block.Protocol.GetCycleEnd(operation.LastCycleUnstaked.Value);

                        var stakingOps = await Db.StakingOps
                            .AsNoTracking()
                            .Where(x => 
                                x.SenderId == sender.Id &&
                                x.Kind == StakingOperationKind.Unstake &&
                                x.Status == OperationStatus.Applied &&
                                x.Level >= startLevel &&
                                x.Level <= endLevel)
                            .OrderBy(x => x.Level)
                            .ThenBy(x => x.Id)
                            .ToListAsync();

                        var delegationOps = await Db.DelegationOps
                            .AsNoTracking()
                            .Where(x =>
                                x.SenderId == sender.Id &&
                                x.UnstakedBalance != null &&
                                x.Status == OperationStatus.Applied &&
                                x.Level >= startLevel &&
                                x.Level <= endLevel)
                            .OrderBy(x => x.Level)
                            .ThenBy(x => x.Id)
                            .ToListAsync();

                        var unstakeOps = stakingOps.Select(x => (x.BakerId, x.Amount.Value))
                            .Concat(delegationOps.Select(x => (x.PrevDelegateId, x.UnstakedBalance.Value + x.UnstakedRewards.Value)));

                        if (unstakeOps.Sum(x => x.Item2) != operation.Amount)
                            throw new Exception("Wrong finalized amount");

                        foreach (var (prevBakerId, unstakedAmount) in unstakeOps)
                        {
                            sender.UnstakedBalance -= unstakedAmount;

                            var prevBaker = Cache.Accounts.GetDelegate(prevBakerId);
                            if (prevBaker != sender)
                            {
                                Db.TryAttach(prevBaker);
                                prevBaker.ExternalUnstakedBalance -= unstakedAmount;
                                prevBaker.StakingBalance -= unstakedAmount;
                                prevBaker.DelegatedBalance -= unstakedAmount;

                                if (senderDelegate != null)
                                {
                                    senderDelegate.StakingBalance += unstakedAmount;
                                    if (senderDelegate != sender)
                                        senderDelegate.DelegatedBalance += unstakedAmount;
                                }
                            }
                        }
                    }
                }
                else if (operation.Kind == StakingOperationKind.SetDelegateParameter)
                {
                    Cache.AppState.Get().PendingStakingParameters++;
                }
            }
            #endregion

            Proto.Manager.Set(operation.Sender);
            Db.StakingOps.Add(operation);
        }

        public async Task Revert(Block block, StakingOperation operation)
        {
            var sender = await Cache.Accounts.GetAsync(operation.SenderId) as User;
            var senderDelegate = sender as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(sender.DelegateId);

            Db.TryAttach(sender);
            Db.TryAttach(senderDelegate);

            #region revert result
            if (operation.Status == OperationStatus.Applied)
            {
                if (operation.Kind == StakingOperationKind.Stake)
                {
                    sender.StakedBalance -= operation.Amount.Value;
                    senderDelegate.TotalStakedBalance -= operation.Amount.Value;
                    if (sender != senderDelegate)
                    {
                        sender.StakingPseudotokens -= operation.Pseudotokens.Value;
                        senderDelegate.StakingPseudotokens -= operation.Pseudotokens.Value;
                        senderDelegate.ExternalStakedBalance -= operation.Amount.Value;
                        if (sender.StakingPseudotokens == 0)
                            senderDelegate.StakersCount--;
                    }
                }
                else if (operation.Kind == StakingOperationKind.Unstake)
                {
                    sender.UnstakedBalance -= operation.Amount.Value;
                    sender.StakedBalance += operation.Amount.Value;
                    senderDelegate.TotalStakedBalance += operation.Amount.Value;
                    if (sender != senderDelegate)
                    {
                        var rewards = sender.StakedBalance - operation.PrevStakedBalance.Value;
                        sender.StakedBalance = operation.PrevStakedBalance.Value;
                        sender.Balance -= rewards;
                        senderDelegate.DelegatedBalance -= rewards;

                        sender.StakingPseudotokens += operation.Pseudotokens.Value;
                        senderDelegate.StakingPseudotokens += operation.Pseudotokens.Value;
                        senderDelegate.ExternalStakedBalance += operation.Amount.Value;
                        senderDelegate.ExternalUnstakedBalance -= operation.Amount.Value;
                        if (sender.StakingPseudotokens == operation.Pseudotokens.Value)
                            senderDelegate.StakersCount++;
                    }
                }
                else if (operation.Kind == StakingOperationKind.FinalizeUnstake)
                {
                    if (operation.Amount > 0)
                    {
                        var startCycleProto = await Cache.Protocols.FindByCycleAsync(operation.FirstCycleUnstaked.Value);
                        var startLevel = startCycleProto.GetCycleStart(operation.FirstCycleUnstaked.Value);

                        var endCycleProto = await Cache.Protocols.FindByCycleAsync(operation.LastCycleUnstaked.Value);
                        var endLevel = block.Protocol.GetCycleEnd(operation.LastCycleUnstaked.Value);

                        var stakingOps = await Db.StakingOps
                            .AsNoTracking()
                            .Where(x =>
                                x.SenderId == sender.Id &&
                                x.Kind == StakingOperationKind.Unstake &&
                                x.Status == OperationStatus.Applied &&
                                x.Level >= startLevel &&
                                x.Level <= endLevel)
                            .OrderBy(x => x.Level)
                            .ThenBy(x => x.Id)
                            .ToListAsync();

                        var delegationOps = await Db.DelegationOps
                            .AsNoTracking()
                            .Where(x =>
                                x.SenderId == sender.Id &&
                                x.UnstakedBalance != null &&
                                x.Status == OperationStatus.Applied &&
                                x.Level >= startLevel &&
                                x.Level <= endLevel)
                            .OrderBy(x => x.Level)
                            .ThenBy(x => x.Id)
                            .ToListAsync();

                        var unstakeOps = stakingOps.Select(x => (x.BakerId, x.Amount.Value))
                            .Concat(delegationOps.Select(x => (x.PrevDelegateId, x.UnstakedBalance.Value + x.UnstakedRewards.Value)));

                        foreach (var (prevBakerId, unstakedAmount) in unstakeOps)
                        {
                            sender.UnstakedBalance += unstakedAmount;

                            var prevBaker = Cache.Accounts.GetDelegate(prevBakerId);
                            if (prevBaker != sender)
                            {
                                Db.TryAttach(prevBaker);
                                prevBaker.ExternalUnstakedBalance += unstakedAmount;
                                prevBaker.StakingBalance += unstakedAmount;
                                prevBaker.DelegatedBalance += unstakedAmount;

                                if (senderDelegate != null)
                                {
                                    senderDelegate.StakingBalance -= unstakedAmount;
                                    if (senderDelegate != sender)
                                        senderDelegate.DelegatedBalance -= unstakedAmount;
                                }
                            }
                        }
                    }
                }
                else if (operation.Kind == StakingOperationKind.SetDelegateParameter)
                {
                    Cache.AppState.Get().PendingStakingParameters--;
                }
            }
            #endregion

            #region revert operation
            sender.Balance += operation.BakerFee;
            sender.Counter = operation.Counter - 1;
            sender.StakingOpsCount--;

            if (senderDelegate != null)
            {
                senderDelegate.StakingBalance += operation.BakerFee;
                if (senderDelegate != sender)
                {
                    senderDelegate.DelegatedBalance += operation.BakerFee;
                    senderDelegate.StakingOpsCount--;
                }
            }

            block.Proposer.Balance -= operation.BakerFee;
            block.Proposer.StakingBalance -= operation.BakerFee;

            Cache.AppState.Get().StakingOpsCount--;
            #endregion

            Db.StakingOps.Remove(operation);
            Cache.AppState.ReleaseManagerCounter();
            Cache.AppState.ReleaseOperationId();
        }

        public async Task ActivateStakingParameters(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin) || Cache.AppState.Get().PendingStakingParameters == 0)
                return;

            var ops = await Db.StakingOps
                .AsNoTracking()
                .Where(x =>
                    x.Kind == StakingOperationKind.SetDelegateParameter &&
                    x.Status == OperationStatus.Applied &&
                    x.ActivationCycle == block.Cycle)
                .ToListAsync();

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.SenderId);
                Db.TryAttach(baker);
                baker.EdgeOfBakingOverStaking = op.EdgeOfBakingOverStaking;
                baker.LimitOfStakingOverBaking = op.LimitOfStakingOverBaking;
                Cache.AppState.Get().PendingStakingParameters--;
            }
        }

        public async Task DeactivateStakingParameters(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleBegin))
                return;

            var ops = await Db.StakingOps
                .AsNoTracking()
                .Where(x =>
                    x.Kind == StakingOperationKind.SetDelegateParameter &&
                    x.Status == OperationStatus.Applied &&
                    x.ActivationCycle == block.Cycle)
                .ToListAsync();

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.SenderId);

                var prevOp = await Db.StakingOps
                    .AsNoTracking()
                    .Where(x =>
                        x.SenderId == baker.Id &&
                        x.Kind == StakingOperationKind.SetDelegateParameter &&
                        x.Status == OperationStatus.Applied &&
                        x.ActivationCycle < op.ActivationCycle)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                Db.TryAttach(baker);
                baker.EdgeOfBakingOverStaking = prevOp?.EdgeOfBakingOverStaking;
                baker.LimitOfStakingOverBaking = prevOp?.LimitOfStakingOverBaking;
                Cache.AppState.Get().PendingStakingParameters++;
            }
        }
    }
}
