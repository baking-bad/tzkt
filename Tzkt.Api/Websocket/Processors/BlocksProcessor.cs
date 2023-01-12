using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Models;
using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Websocket.Processors
{
    public class BlocksProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string BlocksGroup = "blocks";
        const string BlocksChannel = "blocks";
        static readonly SemaphoreSlim Sema = new (1, 1);
        #endregion

        readonly StateCache State;
        readonly BlockRepository Blocks;
        readonly IHubContext<T> Context;
        readonly ILogger Logger;

        public BlocksProcessor(StateCache state, BlockRepository blocks, IHubContext<T> hubContext, ILogger<BlocksProcessor<T>> logger)
        {
            State = state;
            Blocks = blocks;
            Context = hubContext;
            Logger = logger;
        }
        
        public async Task OnStateChanged()
        {
            var sendings = new List<Task>(2);
            try
            {
                await Sema.WaitAsync();

                #region check reorg
                if (State.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {state}", State.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(BlocksGroup)
                        .SendReorg(BlocksChannel, State.ValidLevel));
                }
                #endregion

                if (State.ValidLevel == State.Current.Level)
                {
                    Logger.LogDebug("No blocks to send");
                    return;
                }

                #region load blocks
                Logger.LogDebug("Fetching blocks from {valid} to {current}", State.ValidLevel, State.Current.Level);

                var level = State.Current.Level == State.ValidLevel + 1
                    ? new Int32Parameter
                    {
                        Eq = State.Current.Level
                    }
                    : new Int32Parameter
                    {
                        Gt = State.ValidLevel,
                        Le = State.Current.Level
                    };
                var limit = State.Current.Level - State.ValidLevel;
                var symbols = Symbols.None;

                var blocks = await Blocks.Get(null, null, null, level, null, null, null, null, limit, symbols);
                var count = blocks.Count();

                Logger.LogDebug("{cnt} blocks fetched", count);
                #endregion

                #region send
                sendings.Add(Context.Clients
                    .Group(BlocksGroup)
                    .SendData(BlocksChannel, blocks, State.Current.Level));

                Logger.LogDebug("{cnt} blocks sent", count);
                #endregion
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process state change");
            }
            finally
            {
                Sema.Release();
                #region await sendings
                try
                {
                    await Task.WhenAll(sendings);
                }
                catch (Exception ex)
                {
                    // should never get here
                    Logger.LogError(ex, "Sendings failed");
                }
                #endregion
            }
        }

        public async Task<int> Subscribe(IClientProxy client, string connectionId)
        {
            Task sending = Task.CompletedTask;
            try
            {
                await Sema.WaitAsync();
                Logger.LogDebug("New subscription...");

                await Context.Groups.AddToGroupAsync(connectionId, BlocksGroup);
                sending = client.SendState(BlocksChannel, State.Current.Level);

                Logger.LogDebug("Client {id} subscribed with state {state}", connectionId, State.Current.Level);
                return State.Current.Level;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to add subscription");
                return 0;
            }
            finally
            {
                Sema.Release();
                try
                {
                    await sending;
                }
                catch (Exception ex)
                {
                    // should never get here
                    Logger.LogError(ex, "Sending failed");
                }
            }
        }
    }
}
