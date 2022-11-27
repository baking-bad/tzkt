using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Tzkt.Api.Repositories;
using Tzkt.Api.Services.Cache;

namespace Tzkt.Api.Websocket.Processors
{
    public class HeadProcessor<T> : IHubProcessor where T : Hub
    {
        #region static
        const string HeadGroup = "head";
        const string HeadChannel = "head";
        static readonly SemaphoreSlim Sema = new (1, 1);
        #endregion

        readonly StateCache StateCache;
        readonly StateRepository StateRepo;
        readonly IHubContext<T> Context;
        readonly ILogger Logger;

        public HeadProcessor(StateCache cache, StateRepository repo, IHubContext<T> hubContext, ILogger<HeadProcessor<T>> logger)
        {
            StateCache = cache;
            StateRepo = repo;
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
                if (StateCache.Reorganized)
                {
                    Logger.LogDebug("Sending reorg message with state {state}", StateCache.ValidLevel);
                    sendings.Add(Context.Clients
                        .Group(HeadGroup)
                        .SendReorg(HeadChannel, StateCache.ValidLevel));
                }
                #endregion

                sendings.Add(Context.Clients
                    .Group(HeadGroup)
                    .SendData(HeadChannel, StateRepo.Get(), StateCache.Current.Level));

                Logger.LogDebug("Head {level} sent", StateCache.Current.Level);
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

                await Context.Groups.AddToGroupAsync(connectionId, HeadGroup);
                sending = client.SendState(HeadChannel, StateCache.Current.Level);

                Logger.LogDebug("Client {id} subscribed with state {state}", connectionId, StateCache.Current.Level);
                return StateCache.Current.Level;
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
