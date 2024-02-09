using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto18
{
    class StakingUpdateCommit : ProtocolCommit
    {
        public StakingUpdateCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Apply(IEnumerable<StakingUpdate> updates)
        {
            foreach (var update in updates)
            {
                var baker = Cache.Accounts.GetDelegate(update.BakerId);
                Db.TryAttach(baker);
                baker.StakingUpdatesCount = (baker.StakingUpdatesCount ?? 0) + 1;
                baker.LastLevel = update.Level;

                var staker = (User)await Cache.Accounts.GetAsync(update.StakerId);
                Db.TryAttach(staker);
                if (staker != baker)
                {
                    staker.StakingUpdatesCount = (staker.StakingUpdatesCount ?? 0) + 1;
                    staker.LastLevel = update.Level;
                }

                switch (update.Type)
                {
                    case StakingUpdateType.Stake:
                        #region stake
                        if (staker == baker)
                        {
                            baker.OwnStakedBalance += update.Amount;
                        }
                        else
                        {
                            staker.Balance -= update.Amount;
                            baker.DelegatedBalance -= update.Amount;

                            baker.ExternalStakedBalance += update.Amount;

                            if (update.Pseudotokens is BigInteger pseudotokens && pseudotokens > BigInteger.Zero)
                            {
                                if (staker.StakedPseudotokens == null)
                                    baker.StakersCount++;

                                staker.StakedPseudotokens = (staker.StakedPseudotokens ?? BigInteger.Zero) + pseudotokens;
                                baker.IssuedPseudotokens = (baker.IssuedPseudotokens ?? BigInteger.Zero) + pseudotokens;
                            }
                        }
                        Cache.Statistics.Current.TotalFrozen += update.Amount;
                        #endregion
                        break;
                    case StakingUpdateType.Unstake:
                        #region unstake
                        if (staker == baker)
                        {
                            baker.OwnStakedBalance -= update.Amount;
                            
                            baker.UnstakedBalance += update.Amount;
                        }
                        else
                        {
                            baker.ExternalStakedBalance -= update.Amount;

                            staker.Balance += update.Amount;
                            staker.UnstakedBalance += update.Amount;
                            baker.DelegatedBalance += update.Amount;
                            baker.ExternalUnstakedBalance += update.Amount;

                            if (update.Pseudotokens is BigInteger pseudotokens && pseudotokens > BigInteger.Zero)
                            {
                                staker.StakedPseudotokens -= pseudotokens;
                                baker.IssuedPseudotokens -= pseudotokens;

                                if (staker.StakedPseudotokens == BigInteger.Zero)
                                {
                                    baker.StakersCount--;
                                    staker.StakedPseudotokens = null;
                                    if (baker.IssuedPseudotokens == BigInteger.Zero)
                                        baker.IssuedPseudotokens = null;
                                }
                            }
                        }

                        if (staker.UnstakedBalance > 0)
                        {
                            if (staker.UnstakedBakerId == null)
                                staker.UnstakedBakerId = baker.Id;
                            else if (staker.UnstakedBakerId != baker.Id)
                                throw new Exception("Multiple unstaked bakers are not expected");
                        }

                        await UpdateUnstakeRequests(update);

                        Cache.Statistics.Current.TotalFrozen -= update.Amount;
                        #endregion
                        break;
                    case StakingUpdateType.Restake:
                        #region restake
                        if (staker != baker)
                            throw new NotImplementedException("It's expected that only bakers can restake");

                        baker.UnstakedBalance -= update.Amount;

                        baker.OwnStakedBalance += update.Amount;

                        if (baker.UnstakedBalance == 0)
                            baker.UnstakedBakerId = null;

                        await UpdateUnstakeRequests(update);

                        Cache.Statistics.Current.TotalFrozen += update.Amount;
                        #endregion
                        break;
                    case StakingUpdateType.Finalize:
                        #region finalize
                        if (staker == baker)
                        {
                            baker.UnstakedBalance -= update.Amount;
                        }
                        else
                        {
                            staker.UnstakedBalance -= update.Amount;

                            baker.ExternalUnstakedBalance -= update.Amount;
                            baker.DelegatedBalance -= update.Amount;
                            baker.StakingBalance -= update.Amount;

                            var currentBaker = staker as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(staker.DelegateId);
                            if (currentBaker != null)
                            {
                                Db.TryAttach(currentBaker);
                                currentBaker.StakingBalance += update.Amount;
                                if (currentBaker != staker)
                                    currentBaker.DelegatedBalance += update.Amount;
                            }
                        }

                        if (staker.UnstakedBalance == 0)
                            staker.UnstakedBakerId = null;

                        await UpdateUnstakeRequests(update);
                        #endregion
                        break;
                    case StakingUpdateType.SlashStaked:
                        if (staker == baker)
                        {
                            #region slash own staked
                            baker.Balance -= update.Amount;
                            baker.OwnStakedBalance -= update.Amount;
                            baker.StakingBalance -= update.Amount;

                            //Cache.Statistics.Current.TotalBurned += update.Amount;
                            Cache.Statistics.Current.TotalFrozen -= update.Amount;
                            #endregion
                        }
                        else
                        {
                            #region slash external staked
                            var slashed = update.Amount - (update.RoundingError ?? 0);
                            baker.ExternalStakedBalance -= slashed;
                            baker.StakingBalance -= slashed;

                            //Cache.Statistics.Current.TotalBurned += slashed;
                            Cache.Statistics.Current.TotalFrozen -= slashed;
                            #endregion
                        }
                        break;
                    case StakingUpdateType.SlashUnstaked:
                        #region slash unstaked
                        if (staker == baker)
                        {
                            baker.Balance -= update.Amount;
                            baker.UnstakedBalance -= update.Amount;
                            baker.StakingBalance -= update.Amount;
                        }
                        else
                        {
                            staker.Balance -= update.Amount;
                            staker.UnstakedBalance -= update.Amount;

                            baker.ExternalUnstakedBalance -= update.Amount;
                            baker.DelegatedBalance -= update.Amount;
                            baker.StakingBalance -= update.Amount;
                        }
                        
                        if (update.RoundingError is long roundingError)
                        {
                            baker.DelegatedBalance += roundingError;
                            baker.StakingBalance += roundingError;
                            baker.RoundingError += roundingError;
                        }

                        await UpdateUnstakeRequests(update);

                        //Cache.Statistics.Current.TotalBurned += update.Amount;
                        Cache.Statistics.Current.TotalLost += update.RoundingError ?? 0;
                        #endregion
                        break;
                    default:
                        throw new Exception("Unexpected staking balance updates behavior");
                }

                Db.StakingUpdates.Add(update);
            }
        }

        public async Task Revert(IEnumerable<StakingUpdate> updates)
        {
            foreach (var update in updates)
            {
                var baker = Cache.Accounts.GetDelegate(update.BakerId);
                Db.TryAttach(baker);
                baker.StakingUpdatesCount--;
                if (baker.StakingUpdatesCount == 0) baker.StakingUpdatesCount = null;

                var staker = (User)await Cache.Accounts.GetAsync(update.StakerId);
                Db.TryAttach(staker);
                if (staker != baker)
                {
                    staker.StakingUpdatesCount--;
                    if (staker.StakingUpdatesCount == 0) staker.StakingUpdatesCount = null;
                }

                switch (update.Type)
                {
                    case StakingUpdateType.Stake:
                        #region stake
                        if (staker == baker)
                        {
                            baker.OwnStakedBalance -= update.Amount;
                        }
                        else
                        {
                            staker.Balance += update.Amount;
                            baker.DelegatedBalance += update.Amount;

                            baker.ExternalStakedBalance -= update.Amount;

                            if (update.Pseudotokens is BigInteger pseudotokens && pseudotokens > BigInteger.Zero)
                            {
                                staker.StakedPseudotokens -= pseudotokens;
                                baker.IssuedPseudotokens -= pseudotokens;

                                if (staker.StakedPseudotokens == BigInteger.Zero)
                                {
                                    baker.StakersCount--;
                                    staker.StakedPseudotokens = null;
                                    if (baker.IssuedPseudotokens == BigInteger.Zero)
                                        baker.IssuedPseudotokens = null;
                                }
                            }
                        }
                        #endregion
                        break;
                    case StakingUpdateType.Unstake:
                        #region unstake
                        await RevertUnstakeRequests(update);

                        if (staker.UnstakedBalance == update.Amount)
                            staker.UnstakedBakerId = null;

                        if (staker == baker)
                        {
                            baker.OwnStakedBalance += update.Amount;

                            baker.UnstakedBalance -= update.Amount;
                        }
                        else
                        {
                            baker.ExternalStakedBalance += update.Amount;

                            staker.Balance -= update.Amount;
                            staker.UnstakedBalance -= update.Amount;
                            baker.DelegatedBalance -= update.Amount;
                            baker.ExternalUnstakedBalance -= update.Amount;

                            if (update.Pseudotokens is BigInteger pseudotokens && pseudotokens > BigInteger.Zero)
                            {
                                if (staker.StakedPseudotokens == null)
                                    baker.StakersCount++;

                                staker.StakedPseudotokens = (staker.StakedPseudotokens ?? BigInteger.Zero) + pseudotokens;
                                baker.IssuedPseudotokens = (baker.IssuedPseudotokens ?? BigInteger.Zero) + pseudotokens;
                            }
                        }
                        #endregion
                        break;
                    case StakingUpdateType.Restake:
                        #region restake
                        await RevertUnstakeRequests(update);

                        if (baker.UnstakedBalance == 0 && update.Amount > 0)
                            baker.UnstakedBakerId = baker.Id;

                        baker.UnstakedBalance += update.Amount;

                        baker.OwnStakedBalance -= update.Amount;
                        #endregion
                        break;
                    case StakingUpdateType.Finalize:
                        #region finalize
                        await RevertUnstakeRequests(update);

                        if (staker.UnstakedBalance == 0 && update.Amount > 0)
                            staker.UnstakedBakerId = baker.Id;

                        if (staker == baker)
                        {
                            baker.UnstakedBalance += update.Amount;
                        }
                        else
                        {
                            staker.UnstakedBalance += update.Amount;

                            baker.ExternalUnstakedBalance += update.Amount;
                            baker.DelegatedBalance += update.Amount;
                            baker.StakingBalance += update.Amount;

                            var currentBaker = staker as Data.Models.Delegate ?? Cache.Accounts.GetDelegate(staker.DelegateId);
                            if (currentBaker != null)
                            {
                                Db.TryAttach(currentBaker);
                                currentBaker.StakingBalance -= update.Amount;
                                if (currentBaker != staker)
                                    currentBaker.DelegatedBalance -= update.Amount;
                            }
                        }
                        #endregion
                        break;
                    case StakingUpdateType.SlashStaked:
                        if (staker == baker)
                        {
                            #region slash own staked
                            baker.Balance += update.Amount;
                            baker.OwnStakedBalance += update.Amount;
                            baker.StakingBalance += update.Amount;
                            #endregion
                        }
                        else
                        {
                            #region slash external staked
                            var slashed = update.Amount - (update.RoundingError ?? 0);
                            baker.ExternalStakedBalance += slashed;
                            baker.StakingBalance += slashed;
                            #endregion
                        }
                        break;
                    case StakingUpdateType.SlashUnstaked:
                        #region slash unstaked
                        await RevertUnstakeRequests(update);

                        if (staker == baker)
                        {
                            baker.Balance += update.Amount;
                            baker.UnstakedBalance += update.Amount;
                            baker.StakingBalance += update.Amount;
                        }
                        else
                        {
                            staker.Balance += update.Amount;
                            staker.UnstakedBalance += update.Amount;

                            baker.ExternalUnstakedBalance += update.Amount;
                            baker.DelegatedBalance += update.Amount;
                            baker.StakingBalance += update.Amount;
                        }

                        if (update.RoundingError is long roundingError)
                        {
                            baker.DelegatedBalance -= roundingError;
                            baker.StakingBalance -= roundingError;
                            baker.RoundingError -= roundingError;
                        }
                        #endregion
                        break;
                    default:
                        throw new Exception("Unexpected staking event type");
                }

                Cache.AppState.Get().StakingUpdatesCount--;

                Db.StakingUpdates.Remove(update);
            }
        }

        async Task UpdateUnstakeRequests(StakingUpdate update)
        {
            var bakerUnstaked = await Cache.UnstakeRequests.GetOrDefaultAsync(update.BakerId, null, update.Cycle);
            if (bakerUnstaked == null)
            {
                bakerUnstaked = new UnstakeRequest
                {
                    Id = ++Cache.AppState.Get().UnstakeRequestsCount,
                    Cycle = update.Cycle,
                    BakerId = update.BakerId,
                    StakerId = null,
                    FirstLevel = update.Level,
                };
                Db.UnstakeRequests.Add(bakerUnstaked);
                Cache.UnstakeRequests.Add(bakerUnstaked);
            }
            else
            {
                Db.TryAttach(bakerUnstaked);
            }

            var stakerUnstaked = await Cache.UnstakeRequests.GetOrDefaultAsync(update.BakerId, update.StakerId, update.Cycle);
            if (stakerUnstaked == null)
            {
                stakerUnstaked = new UnstakeRequest
                {
                    Id = ++Cache.AppState.Get().UnstakeRequestsCount,
                    Cycle = update.Cycle,
                    BakerId = update.BakerId,
                    StakerId = update.StakerId,
                    FirstLevel = update.Level
                };
                Db.UnstakeRequests.Add(stakerUnstaked);
                Cache.UnstakeRequests.Add(stakerUnstaked);
            }
            else
            {
                Db.TryAttach(stakerUnstaked);
            }

            if (update.Type == StakingUpdateType.Unstake)
            {
                bakerUnstaked.RequestedAmount += update.Amount;
                stakerUnstaked.RequestedAmount += update.Amount;
            }
            else if (update.Type == StakingUpdateType.Restake)
            {
                bakerUnstaked.RestakedAmount += update.Amount;
                stakerUnstaked.RestakedAmount += update.Amount;
            }
            else if (update.Type == StakingUpdateType.Finalize)
            {
                bakerUnstaked.FinalizedAmount += update.Amount;
                stakerUnstaked.FinalizedAmount += update.Amount;
            }
            else
            {
                bakerUnstaked.SlashedAmount += update.Amount;
                stakerUnstaked.SlashedAmount += update.Amount;

                if (update.RoundingError is long roundingError)
                    bakerUnstaked.RoundingError = (bakerUnstaked.RoundingError ?? 0) + roundingError;
            }

            bakerUnstaked.UpdatesCount++;
            stakerUnstaked.UpdatesCount++;

            bakerUnstaked.LastLevel = update.Level;
            stakerUnstaked.LastLevel = update.Level;
        }

        async Task RevertUnstakeRequests(StakingUpdate update)
        {
            var bakerUnstaked = await Cache.UnstakeRequests.GetAsync(update.BakerId, null, update.Cycle);
            Db.TryAttach(bakerUnstaked);

            var stakerUnstaked = await Cache.UnstakeRequests.GetAsync(update.BakerId, update.StakerId, update.Cycle);
            Db.TryAttach(stakerUnstaked);

            if (update.Type == StakingUpdateType.Unstake)
            {
                bakerUnstaked.RequestedAmount -= update.Amount;
                stakerUnstaked.RequestedAmount -= update.Amount;
            }
            else if (update.Type == StakingUpdateType.Restake)
            {
                bakerUnstaked.RestakedAmount -= update.Amount;
                stakerUnstaked.RestakedAmount -= update.Amount;
            }
            else if (update.Type == StakingUpdateType.Finalize)
            {
                bakerUnstaked.FinalizedAmount -= update.Amount;
                stakerUnstaked.FinalizedAmount -= update.Amount;
            }
            else
            {
                bakerUnstaked.SlashedAmount -= update.Amount;
                stakerUnstaked.SlashedAmount -= update.Amount;

                if (update.RoundingError is long roundingError)
                {
                    bakerUnstaked.RoundingError -= roundingError;
                    if (bakerUnstaked.RoundingError == 0)
                        bakerUnstaked.RoundingError = null;
                }
            }

            bakerUnstaked.UpdatesCount--;
            stakerUnstaked.UpdatesCount--;

            if (bakerUnstaked.UpdatesCount == 0)
            {
                Cache.AppState.Get().UnstakeRequestsCount--;
                Db.UnstakeRequests.Remove(bakerUnstaked);
                Cache.UnstakeRequests.Remove(bakerUnstaked);
            }
            else
            {
                var prevUpdate = await Db.StakingUpdates
                    .AsNoTracking()
                    .Where(x =>
                        x.BakerId == update.BakerId &&
                        x.Cycle == update.Cycle &&
                        x.Id < update.Id)
                    .OrderByDescending(x => x.Id)
                    .FirstAsync();

                bakerUnstaked.LastLevel = prevUpdate.Level;
            }

            if (stakerUnstaked.UpdatesCount == 0)
            {
                Cache.AppState.Get().UnstakeRequestsCount--;
                Db.UnstakeRequests.Remove(stakerUnstaked);
                Cache.UnstakeRequests.Remove(stakerUnstaked);
            }
            else
            {
                var prevUpdate = await Db.StakingUpdates
                    .AsNoTracking()
                    .Where(x =>
                        x.StakerId == update.StakerId &&
                        x.Cycle == update.Cycle &&
                        x.Id < update.Id)
                    .OrderByDescending(x => x.Id)
                    .FirstAsync();

                stakerUnstaked.LastLevel = prevUpdate.Level;
            }
        }
    }
}
