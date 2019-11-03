using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services.Cache
{
    static class AppCache
    {
        public const int BlocksCapacity = 3 * 4096;
        public const int AccountsCapacity = 8 * 4096;
        public const int ProtocolsCapacity = 16;

        static AppState AppState = null;
        static VotingPeriod VotingPeriod = null;

        static readonly Dictionary<int, Block> Blocks = new Dictionary<int, Block>(BlocksCapacity);
        static readonly Dictionary<string, Account> Accounts = new Dictionary<string, Account>(AccountsCapacity);
        static readonly Dictionary<string, Protocol> Protocols = new Dictionary<string, Protocol>(ProtocolsCapacity);

        #region state
        public static AppState SetAppState(AppState appState)
        {
            AppState = appState;
            return AppState;
        }

        public static async Task<AppState> GetOrSetAppState(Func<Task<AppState>> creator)
            => AppState ?? SetAppState(await creator());
        #endregion

        #region blocks
        public static Block AddBlock(Block block)
        {
            if (block == null) return null;

            if (Blocks.Count >= BlocksCapacity)
                foreach (var key in Blocks.Keys.OrderBy(x => x).Take(BlocksCapacity / 4).ToList())
                    Blocks.Remove(key);

            Blocks[block.Level] = block;
            return block;
        }

        public static Block GetBlock(int level)
            => Blocks.ContainsKey(level) ? Blocks[level] : null;

        public static async Task<Block> GetOrSetBlock(int level, Func<Task<Block>> creator)
            => GetBlock(level) ?? AddBlock(await creator());

        public static void RemoveBlock(Block block)
            => Blocks.Remove(block.Level);
        #endregion

        #region voting
        public static VotingPeriod SetVotingPeriod(VotingPeriod period)
        {
            VotingPeriod = period;
            return VotingPeriod;
        }

        public static async Task<VotingPeriod> GetOrSetVotingPeriod(Func<Task<VotingPeriod>> creator)
            => VotingPeriod ?? SetVotingPeriod(await creator());

        public static void RemoveVotingPeriod()
            => VotingPeriod = null;
        #endregion

        #region accounts
        public static bool HasAccount(string address)
            => Accounts.ContainsKey(address);

        public static void EnsureAccountsCap(int count)
        {
            if (Accounts.Count + count >= AccountsCapacity)
                foreach (var key in Accounts
                    .Where(x => x.Value.Type != AccountType.Delegate)
                    .Select(x => x.Key)
                    .Take(AccountsCapacity / 4)
                    .ToList())
                    Accounts.Remove(key);
        }

        public static Account AddAccount(Account account)
        {
            if (account == null) return null;

            if (Accounts.Count >= AccountsCapacity)
                foreach (var key in Accounts
                    .Where(x => x.Value.Type != AccountType.Delegate)
                    .Select(x => x.Key)
                    .Take(AccountsCapacity / 4)
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
            if (protocol == null) return null;

            if (Protocols.Count >= ProtocolsCapacity)
                foreach (var key in Protocols.Keys.Take(ProtocolsCapacity / 4).ToList())
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
            VotingPeriod = null;

            Blocks.Clear();
            Accounts.Clear();
            Protocols.Clear();
        }
    }
}
