using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    static class AppCache
    {
        const int _AccountsCapacity = 8192;
        const int _ProtocolsCapacity = 8;

        static AppState AppState = null;
        static Block CurrentBlock = null;
        static Block PreviousBlock = null;
        static VotingEpoch VotingEpoch = null;

        static readonly Dictionary<string, Account> Accounts = new Dictionary<string, Account>(_AccountsCapacity);
        static readonly Dictionary<string, Protocol> Protocols = new Dictionary<string, Protocol>(_ProtocolsCapacity);

        #region state
        public static AppState SetAppState(AppState appState)
        {
            AppState = appState;
            return AppState;
        }

        public static AppState GetAppState()
            => AppState;

        public static async Task<AppState> GetOrSetAppState(Func<Task<AppState>> creator)
            => GetAppState() ?? SetAppState(await creator());
        #endregion

        #region blocks
        public static void PushBlock(Block block)
        {
            PreviousBlock = CurrentBlock;
            CurrentBlock = block;
        }

        public static void PushBlock(Block block, Block previous)
        {
            PreviousBlock = previous;
            CurrentBlock = block;
        }

        public static Block SetCurrentBlock(Block block)
        {
            CurrentBlock = block;
            return CurrentBlock;
        }

        public static Block GetCurrentBlock()
            => CurrentBlock;

        public static async Task<Block> GetOrSetCurrentBlock(Func<Task<Block>> creator)
            => GetCurrentBlock() ?? SetCurrentBlock(await creator());
        
        public static Block SetPreviousBlock(Block block)
        {
            PreviousBlock = block;
            return PreviousBlock;
        }

        public static Block GetPreviousBlock()
            => PreviousBlock;

        public static async Task<Block> GetOrSetPreviousBlock(Func<Task<Block>> creator)
            => GetPreviousBlock() ?? SetPreviousBlock(await creator());
        #endregion

        #region voting
        public static VotingEpoch SetVotingEpoch(VotingEpoch epoch)
        {
            VotingEpoch = epoch;
            return VotingEpoch;
        }

        public static VotingEpoch GetVotingEpoch()
            => VotingEpoch;

        public static async Task<VotingEpoch> GetOrSetVotingEpoch(Func<Task<VotingEpoch>> creator)
            => GetVotingEpoch() ?? SetVotingEpoch(await creator());
        #endregion

        #region accounts
        public static Account AddAccount(Account account)
        {
            if (account == null) return null;

            if (Accounts.Count >= _AccountsCapacity)
                foreach (var key in Accounts
                    .Where(x => x.Value.Type != AccountType.Delegate)
                    .Select(x => x.Key)
                    .Take(_AccountsCapacity / 8)
                    .ToList())
                    Accounts.Remove(key);

            Accounts[account.Address] = account;
            return account;
        }

        public static Account GetAccount(int? id)
            => id != null ? Accounts.Values.FirstOrDefault(x => x.Id == id) : null;

        public static Account GetAccount(string address)
            => Accounts.ContainsKey(address) ? Accounts[address] : null;

        public static async Task<Account> GetOrSetAccount(int? id, Func<Task<Account>> creator)
            => GetAccount(id) ?? AddAccount(await creator());

        public static async Task<Account> GetOrSetAccount(string address, Func<Task<Account>> creator)
            => GetAccount(address) ?? AddAccount(await creator());

        public static void RemoveAccount(Account account)
            => Accounts.Remove(account.Address);
        #endregion

        #region protocols
        public static Protocol AddProtocol(Protocol protocol)
        {
            if (Protocols.Count >= _ProtocolsCapacity)
                foreach (var key in Protocols.Keys.Take(_ProtocolsCapacity / 4).ToList())
                    Protocols.Remove(key);

            Protocols[protocol.Hash] = protocol;
            return protocol;
        }

        public static Protocol GetProtocol(int code)
            => Protocols.Values.FirstOrDefault(x => x.Code == code);

        public static Protocol GetProtocol(string hash)
            => Protocols.ContainsKey(hash) ? Protocols[hash] : null;

        public static async Task<Protocol> GetOrSetProtocol(int code, Func<Task<Protocol>> creator)
            => GetProtocol(code) ?? AddProtocol(await creator());

        public static async Task<Protocol> GetOrSetProtocol(string hash, Func<Task<Protocol>> creator)
            => GetProtocol(hash) ?? AddProtocol(await creator());

        public static void RemoveProtocol(Protocol protocol)
            => Protocols.Remove(protocol.Hash);
        #endregion

        public static void Clear()
        {
            AppState = null;
            CurrentBlock = null;
            PreviousBlock = null;
            Accounts.Clear();
            Protocols.Clear();
        }
    }
}
