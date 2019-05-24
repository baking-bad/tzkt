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
                    while (!await Node.HasUpdatesAsync(AppState.CurrentLevel))
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

                #region validate current branch
                try
                {
                    while (AppState.CurrentLevel >= 0
                        && !await Node.ValidateBranchAsync(AppState.CurrentLevel, AppState.CurrentHash))
                    {
                        Logger.LogError($"Invalid branch: {AppState.CurrentLevel} - {AppState.CurrentHash}. Reverting block...");

                        var protocol = Protocols.GetProtocolHandler(AppState.CurrentProtocol);
                        AppState = await protocol.RevertLastBlock();

                        Logger.LogDebug($"Reverted to: {AppState.CurrentLevel} - {AppState.CurrentHash}");
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
                    while (await Node.HasUpdatesAsync(AppState.CurrentLevel))
                    {
                        Logger.LogDebug($"Applying block {AppState.CurrentLevel + 1}...");

                        var block = await Node.Rpc.Blocks[AppState.CurrentLevel + 1].GetAsync();
                        var protocol = Protocols.GetProtocolHandler(block["protocol"].String());
                        AppState = await protocol.ApplyBlock((JObject)block);

                        Logger.LogDebug($"New head: {AppState.CurrentLevel} - {AppState.CurrentHash}");
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
