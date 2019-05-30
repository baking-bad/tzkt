using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

using Tezzycat.Data;
using Tezzycat.Models;

namespace Tezzycat.Services
{
    public class Observer
    {
        #region services
        private readonly TezosNode Node;
        private readonly TezosProtocols Protocols;
        private readonly IServiceProvider Services;
        private readonly ILogger Logger;
        #endregion

        #region state
        private bool IsWorking;
        private ObserverState State;
        #endregion

        #region cache
        private AppState AppState;
        #endregion

        public Observer(TezosNode node, TezosProtocols protocols, IServiceProvider services, ILogger<Observer> logger)
        {
            Node = node;
            Protocols = protocols;
            Services = services;
            Logger = logger;
        }

        public void Start()
        {
            if (State == ObserverState.Working)
                return;

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<AppDbContext>();
                AppState = db.AppState.FirstOrDefault();
            }

            IsWorking = true;
            var task = Background();

            while (State != ObserverState.Working)
                Thread.Sleep(100);
        }
        public void Stop()
        {
            if (State != ObserverState.Working)
                return;

            IsWorking = false;

            while (State != ObserverState.Stoped)
                Thread.Sleep(500);
        }
        
        async Task Background()
        {
            Logger.LogWarning("Observer is started");
            State = ObserverState.Working;

            while (IsWorking)
            {
                #region check for updates
                try
                {
                    while (!await Node.HasUpdatesAsync(AppState.Level))
                        await Task.Delay(1000);

                    Logger.LogDebug($"Newer head found: {(await Node.GetHeaderAsync()).Level}");
                }
                catch (Exception ex)
                {
                    Logger.LogCritical($"Failed to check updates. {ex.Message}");
                    await Task.Delay(5000);
                    continue;
                }
                #endregion

                using (var scope = Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetService<AppDbContext>();

                    #region validate current branch
                    try
                    {
                        while (AppState.Level >= 0
                            && !await Node.ValidateBranchAsync(AppState.Level, AppState.Hash))
                        {
                            Logger.LogError($"Invalid branch: {AppState.Level} - {AppState.Hash}. Reverting block...");

                            var protoHandler = Protocols.GetProtocolHandler(AppState.Protocol);
                            AppState = await protoHandler.RevertLastBlock(db);

                            Logger.LogDebug($"Reverted to: {AppState.Level} - {AppState.Hash}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical($"Failed to validate branch. {ex.Message}");
                        await Task.Delay(5000);
                        continue;
                    }
                    #endregion

                    #region apply updates
                    try
                    {
                        while (await Node.HasUpdatesAsync(AppState.Level))
                        {
                            Logger.LogDebug($"Applying block {AppState.Level + 1}...");

                            var block = await Node.Rpc.Blocks[AppState.Level + 1].GetAsync();
                            var protoHandler = Protocols.GetProtocolHandler(block["protocol"].String());
                            AppState = await protoHandler.ApplyBlock(db, (JObject)block);

                            Logger.LogDebug($"New head: {AppState.Level} - {AppState.Hash}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical($"Failed to apply updates. {ex.Message}");
                        await Task.Delay(5000);
                        continue;
                    }
                    #endregion
                }
            }

            State = ObserverState.Stoped;
            Logger.LogWarning("Observer is stoped");
        }

        enum ObserverState
        {
            Inited,
            Working,
            Stoped
        }
    }

    public static class ObserverExt
    {
        public static void AddObserver(this IServiceCollection services)
        {
            services.AddSingleton<Observer>();
        }
    }
}
