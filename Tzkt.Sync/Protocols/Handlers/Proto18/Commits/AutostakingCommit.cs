using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto18
{
    class AutostakingCommit : ProtocolCommit
    {
        public AutostakingCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual void Apply(Block block, JsonElement rawBlock)
        {
            if (!block.Events.HasFlag(BlockEvents.CycleEnd))
                return;

            var ops = new List<AutostakingOperation>();

            #region parse updates
            var balanceUpdates = rawBlock
                .Required("metadata")
                .RequiredArray("balance_updates")
                .EnumerateArray()
                .Where(x => x.RequiredString("origin") == "block")
                .ToList();

            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                switch (balanceUpdates[i].RequiredString("kind"))
                {
                    case "accumulator":
                    case "minted":
                        i++;
                        break;
                    case "contract":
                        if (i == balanceUpdates.Count - 1 ||
                            balanceUpdates[i + 1].RequiredString("kind") != "freezer" ||
                            balanceUpdates[i + 1].RequiredString("category") != "deposits" ||
                            balanceUpdates[i].RequiredInt64("change") != -balanceUpdates[i + 1].RequiredInt64("change") ||
                            balanceUpdates[i].RequiredString("contract") != balanceUpdates[i + 1].Required("staker").RequiredString("baker"))
                            throw new Exception("Unexpected contract->deposits balance update behavior");
                        
                        ops.Add(new AutostakingOperation
                        {
                            Id = Cache.AppState.NextOperationId(),
                            Level = block.Level,
                            BakerId = Cache.Accounts.GetDelegate(balanceUpdates[i].RequiredString("contract")).Id,
                            Action = AutostakingAction.Stake,
                            Cycle = block.Cycle + 1,
                            Amount = balanceUpdates[i + 1].RequiredInt64("change")
                        });

                        i++;
                        break;
                    case "freezer" when balanceUpdates[i].RequiredString("category") == "deposits":
                        if (i == balanceUpdates.Count - 1 ||
                            balanceUpdates[i + 1].RequiredString("kind") != "freezer" ||
                            balanceUpdates[i + 1].RequiredString("category") != "unstaked_deposits" ||
                            balanceUpdates[i].RequiredInt64("change") != -balanceUpdates[i + 1].RequiredInt64("change") ||
                            balanceUpdates[i].Required("staker").RequiredString("baker") != balanceUpdates[i + 1].Required("staker").RequiredString("contract") ||
                            balanceUpdates[i].Required("staker").RequiredString("baker") != balanceUpdates[i + 1].Required("staker").RequiredString("delegate"))
                            throw new Exception("Unexpected deposits->unstaked_deposits balance updates behavior");

                        ops.Add(new AutostakingOperation
                        {
                            Id = Cache.AppState.NextOperationId(),
                            Level = block.Level,
                            BakerId = Cache.Accounts.GetDelegate(balanceUpdates[i].Required("staker").RequiredString("baker")).Id,
                            Action = AutostakingAction.Unstake,
                            Cycle = balanceUpdates[i + 1].RequiredInt32("cycle"),
                            Amount = balanceUpdates[i + 1].RequiredInt64("change")
                        });

                        i++;
                        break;
                    case "freezer" when balanceUpdates[i].RequiredString("category") == "unstaked_deposits":
                        if (i < balanceUpdates.Count - 1 &&
                            balanceUpdates[i + 1].RequiredString("kind") == "contract" &&
                            balanceUpdates[i].RequiredInt64("change") == -balanceUpdates[i + 1].RequiredInt64("change") &&
                            balanceUpdates[i].Required("staker").RequiredString("contract") == balanceUpdates[i + 1].RequiredString("contract") &&
                            balanceUpdates[i].Required("staker").RequiredString("delegate") == balanceUpdates[i + 1].RequiredString("contract"))
                        {
                            ops.Add(new AutostakingOperation
                            {
                                Id = Cache.AppState.NextOperationId(),
                                Level = block.Level,
                                BakerId = Cache.Accounts.GetDelegate(balanceUpdates[i].Required("staker").RequiredString("delegate")).Id,
                                Action = AutostakingAction.Finalize,
                                Cycle = balanceUpdates[i].RequiredInt32("cycle"),
                                Amount = balanceUpdates[i + 1].RequiredInt64("change")
                            });
                        }
                        else if (i < balanceUpdates.Count - 1 &&
                            balanceUpdates[i + 1].RequiredString("kind") == "freezer" &&
                            balanceUpdates[i + 1].RequiredString("category") == "deposits" &&
                            balanceUpdates[i].RequiredInt64("change") == -balanceUpdates[i + 1].RequiredInt64("change") &&
                            balanceUpdates[i].Required("staker").RequiredString("contract") == balanceUpdates[i + 1].Required("staker").RequiredString("baker") &&
                            balanceUpdates[i].Required("staker").RequiredString("delegate") == balanceUpdates[i + 1].Required("staker").RequiredString("baker"))
                        {
                            ops.Add(new AutostakingOperation
                            {
                                Id = Cache.AppState.NextOperationId(),
                                Level = block.Level,
                                BakerId = Cache.Accounts.GetDelegate(balanceUpdates[i].Required("staker").RequiredString("delegate")).Id,
                                Action = AutostakingAction.Restake,
                                Cycle = balanceUpdates[i].RequiredInt32("cycle"),
                                Amount = balanceUpdates[i + 1].RequiredInt64("change")
                            });
                        }
                        else
                        {
                            throw new Exception("Unexpected unstaked_deposits->* balance updates behavior");
                        }
                        i++;
                        break;
                    default:
                        throw new Exception("Unexpected block balance update kind");

                }
            }
            #endregion

            Db.TryAttach(block);

            var state = Cache.AppState.Get();
            Db.TryAttach(state);

            var stats = Cache.Statistics.Current;
            Db.TryAttach(stats);

            foreach (var op in ops)
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);
                baker.AutostakingOpsCount++;

                switch (op.Action)
                {
                    case AutostakingAction.Stake:
                        baker.StakedBalance += op.Amount;
                        baker.TotalStakedBalance += op.Amount;
                        stats.TotalFrozen += op.Amount;
                        break;
                    case AutostakingAction.Unstake:
                        baker.UnstakedBalance += op.Amount;
                        if (baker.UnstakedBalance > 0)
                        {
                            if (baker.UnstakedBakerId == null)
                                baker.UnstakedBakerId = baker.Id;
                            else if (baker.UnstakedBakerId != baker.Id)
                                throw new Exception("Multiple unstaked bakers are not expected");
                        }
                        baker.StakedBalance -= op.Amount;
                        baker.TotalStakedBalance -= op.Amount;
                        stats.TotalFrozen -= op.Amount;
                        break;
                    case AutostakingAction.Finalize:
                        baker.UnstakedBalance -= op.Amount;
                        if (baker.UnstakedBalance == 0)
                            baker.UnstakedBakerId = null;
                        break;
                    case AutostakingAction.Restake:
                        baker.UnstakedBalance -= op.Amount;
                        if (baker.UnstakedBalance == 0)
                            baker.UnstakedBakerId = null;
                        baker.StakedBalance += op.Amount;
                        baker.TotalStakedBalance += op.Amount;
                        stats.TotalFrozen += op.Amount;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid AutostakingAction: {op.Action}");
                }

                block.Operations |= Operations.Autostaking;
                state.AutostakingOpsCount++;

                Db.AutostakingOps.Add(op);
            }
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Operations.HasFlag(Operations.Autostaking))
                return;

            foreach (var op in await Db.AutostakingOps.Where(x => x.Level == block.Level).ToListAsync())
            {
                var baker = Cache.Accounts.GetDelegate(op.BakerId);
                Db.TryAttach(baker);
                baker.AutostakingOpsCount++;

                switch (op.Action)
                {
                    case AutostakingAction.Stake:
                        baker.StakedBalance -= op.Amount;
                        baker.TotalStakedBalance -= op.Amount;
                        break;
                    case AutostakingAction.Unstake:
                        baker.UnstakedBalance -= op.Amount;
                        if (baker.UnstakedBalance == 0)
                            baker.UnstakedBakerId = null;
                        baker.StakedBalance += op.Amount;
                        baker.TotalStakedBalance += op.Amount;
                        break;
                    case AutostakingAction.Finalize:
                        baker.UnstakedBalance += op.Amount;
                        if (baker.UnstakedBalance == op.Amount)
                            baker.UnstakedBakerId = baker.Id;
                        break;
                    case AutostakingAction.Restake:
                        baker.UnstakedBalance += op.Amount;
                        if (baker.UnstakedBalance == op.Amount)
                            baker.UnstakedBakerId = baker.Id;
                        baker.StakedBalance -= op.Amount;
                        baker.TotalStakedBalance -= op.Amount;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid AutostakingAction: {op.Action}");
                }

                Cache.AppState.Get().AutostakingOpsCount--;

                Db.AutostakingOps.Remove(op);
                Cache.AppState.ReleaseOperationId();
            }
        }
    }
}
