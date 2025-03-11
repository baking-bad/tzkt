using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto5
{
    class TokensCommit(ProtocolHandler protocol) : ProtocolCommit(protocol)
    {
        public virtual async Task Apply(Block block,
            List<(BigMap BigMap, BigMapKey? Key, BigMapUpdate Update, ContractOperation Op)> updates)
        {
            updates = updates.OrderBy(x => x.Op.Id).ThenBy(x => x.BigMap.Ptr).ToList();
            var ops = new Dictionary<ContractOperation, (
                bool Reset,
                Contract Contract,
                Dictionary<BigInteger, (
                    List<(string From, string To, BigInteger Amount)> Transfers,
                    List<(string Address, BigInteger Balance)> Balances
                )> Tokens
            )>();
            var opBlocks = new Dictionary<int, Block> { { block.Level, block } };

            #region discover ledgers
            Dictionary<int, BigMap>? pendingBigMaps = null;
            foreach (var (bigmap, _, update, op) in updates)
            {
                if (update.Action == BigMapAction.Allocate)
                {
                    if ((bigmap.Tags & BigMapTag.LedgerTypes) != 0)
                    {
                        var contract = (await Cache.Accounts.GetAsync(
                            op is TransactionOperation tx
                                ? tx.TargetId!.Value
                                : (op as OriginationOperation)!.ContractId!.Value
                        ) as Contract)!;

                        if (contract.Tags.HasFlag(ContractTags.Ledger))
                        {
                            // there must be only one ledger bigmap
                            bigmap.Tags &= ~BigMapTag.LedgerMask;
                            Logger.LogWarning("Multiple ledger bigmaps discovered for {contract}", contract.Address);
                        }
                        else
                        {
                            Db.TryAttach(contract);
                            contract.Tags |= ContractTags.Ledger;
                            if ((bigmap.Tags & BigMapTag.LedgerNft) != 0)
                                contract.Tags |= ContractTags.Nft;
                        }
                    }
                }
                else if (update.Action == BigMapAction.Remove)
                {
                    if ((bigmap.Tags & BigMapTag.LedgerTypes) != 0)
                    {
                        var contract = (await Cache.Accounts.GetAsync(
                            op is TransactionOperation tx
                                ? tx.TargetId!.Value
                                : (op as OriginationOperation)!.ContractId!.Value
                        ) as Contract)!;

                        Db.TryAttach(contract);
                        contract.Tags &= ~ContractTags.Ledger;
                        if ((bigmap.Tags & BigMapTag.LedgerNft) != 0)
                            contract.Tags &= ~ContractTags.Nft;
                    }
                }
                else if ((bigmap.Tags & (BigMapTag.Persistent | BigMapTag.Ledger)) == BigMapTag.Persistent &&
                    op is TransactionOperation tx && tx.Entrypoint == "transfer" && await Cache.Accounts.GetAsync(tx.TargetId) is Contract contract &&
                    (contract.Tags & (ContractTags.FA | ContractTags.Ledger)) == ContractTags.FA)
                {
                    Db.TryAttach(bigmap);
                    bigmap.Tags |= BigMaps.GetLedgerType(bigmap.Schema);

                    if (bigmap.Tags.HasFlag(BigMapTag.Ledger))
                    {
                        Db.TryAttach(contract);
                        contract.Tags |= ContractTags.Ledger;
                        if ((bigmap.Tags & BigMapTag.LedgerNft) != 0)
                            contract.Tags |= ContractTags.Nft;

                        pendingBigMaps ??= [];
                        pendingBigMaps.Add(bigmap.Ptr, bigmap);
                    }
                    else
                    {
                        bigmap.Tags |= BigMapTag.Ledger;
                        Logger.LogWarning("Unsupported ledger bigmap #{ptr} ignored", bigmap.Ptr);
                    }
                }
            }
            if (pendingBigMaps != null)
            {
                #region load entities
                var ptrs = pendingBigMaps.Keys.ToHashSet();
                var pendingUpdates = await Db.BigMapUpdates
                    .AsNoTracking()
                    .Where(x => ptrs.Contains(x.BigMapPtr) &&
                                x.Action != BigMapAction.Allocate &&
                                x.Level < block.Level)
                    .ToListAsync();

                var keys = pendingUpdates.Count == 0 ? [] : await Db.BigMapKeys
                    .AsNoTracking()
                    .Where(x => ptrs.Contains(x.BigMapPtr))
                    .ToDictionaryAsync(x => x.Id);

                var txIds = pendingUpdates
                    .Where(x => x.TransactionId != null)
                    .Select(x => x.TransactionId!.Value)
                    .ToHashSet();

                var txs = txIds.Count == 0 ? [] : await Db.TransactionOps
                    //.AsNoTracking()
                    .Where(x => txIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);

                var origIds = pendingUpdates
                    .Where(x => x.OriginationId != null)
                    .Select(x => x.OriginationId!.Value)
                    .ToHashSet();

                var origs = origIds.Count == 0 ? [] : await Db.OriginationOps
                    //.AsNoTracking()
                    .Where(x => origIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id);

                var contracts = pendingBigMaps.Values
                    .Select(x => x.ContractId)
                    .ToHashSet();

                // transfers with no balance updates, e.g. to itself or with 0 amount
                var pendingTransfers = await Db.TransactionOps
                    //.AsNoTracking()
                    .Where(x => x.TargetId != null &&
                                contracts.Contains(x.TargetId.Value) &&
                                x.Status == OperationStatus.Applied &&
                                x.Entrypoint == "transfer" &&
                                x.TokenTransfers == null &&
                                x.Level < block.Level)
                    .ToListAsync();
                #endregion

                #region preload
                var blocks = pendingTransfers.Select(x => x.Level)
                    .Concat(pendingUpdates.Select(x => x.Level))
                    .ToHashSet();

                var targets = pendingTransfers.Select(x => x.TargetId!.Value)
                    .Concat(pendingBigMaps.Select(x => x.Value.ContractId))
                    .ToHashSet();

                await Cache.Blocks.Preload(blocks);
                await Cache.Accounts.Preload(targets);
                #endregion

                #region group
                foreach (var tx in pendingTransfers)
                {
                    if (!opBlocks.ContainsKey(tx.Level))
                    {
                        var opBlock = Cache.Blocks.GetCached(tx.Level);
                        opBlocks.Add(tx.Level, opBlock);
                        Db.TryAttach(opBlock);
                    }

                    var tokens = new Dictionary<BigInteger, (
                        List<(string From, string To, BigInteger Amount)> Transfers,
                        List<(string Address, BigInteger Balance)> Balances
                    )>();

                    foreach (var (from, to, tokenId, amount) in ParseTransferParam(Micheline.FromBytes(tx.RawParameters!)))
                    {
                        if (!tokens.TryGetValue(tokenId, out var ctx))
                        {
                            ctx = ([], []);
                            tokens.Add(tokenId, ctx);
                        }
                        ctx.Transfers.Add((from, to, amount));
                    }

                    var contract = (Cache.Accounts.GetCached(tx.TargetId!.Value) as Contract)!;
                    ops.Add(tx, (false, contract, tokens));
                }

                foreach (var update in pendingUpdates)
                {
                    var bigmap = pendingBigMaps[update.BigMapPtr];
                    var op = update.OriginationId != null
                        ? origs[update.OriginationId.Value] as ContractOperation
                        : txs[update.TransactionId!.Value];

                    if (!opBlocks.ContainsKey(op.Level))
                    {
                        var opBlock = Cache.Blocks.GetCached(op.Level);
                        opBlocks.Add(op.Level, opBlock);
                        Db.TryAttach(opBlock);
                    }

                    if (!ops.TryGetValue(op, out var opCtx))
                    {
                        var contract = (Cache.Accounts.GetCached(bigmap.ContractId) as Contract)!;
                        opCtx = (false, contract, []);
                        ops.Add(op, opCtx);
                    }

                    if (update.Action == BigMapAction.Remove)
                    {
                        ops[op] = (true, ops[op].Contract, ops[op].Tokens);
                    }
                    else
                    {
                        var key = keys[update.BigMapKeyId!.Value];

                        foreach (var (address, tokenId, balance) in BigMaps.ParseLedger(bigmap, key, update))
                        {
                            if (!opCtx.Tokens.TryGetValue(tokenId, out var tokenCtx))
                            {
                                tokenCtx = ([], []);
                                opCtx.Tokens.Add(tokenId, tokenCtx);
                            }
                            tokenCtx.Balances.Add((address, balance));
                        }
                    }

                }
                #endregion
            }
            #endregion

            #region group updates
            foreach (var tx in Context.TransactionOps)
            {
                if (tx.Status == OperationStatus.Applied && tx.Entrypoint == "transfer")
                {
                    var contract = (await Cache.Accounts.GetAsync(tx.TargetId!.Value) as Contract)!;
                    if (contract.Tags.HasFlag(ContractTags.Ledger))
                    {
                        var tokens = new Dictionary<BigInteger, (
                            List<(string From, string To, BigInteger Amount)> Transfers,
                            List<(string Address, BigInteger Balance)> Balances
                        )>();

                        foreach (var (from, to, tokenId, amount) in ParseTransferParam(Micheline.FromBytes(tx.RawParameters!)))
                        {
                            if (!tokens.TryGetValue(tokenId, out var ctx))
                            {
                                ctx = ([], []);
                                tokens.Add(tokenId, ctx);
                            }
                            ctx.Transfers.Add((from, to, amount));
                        }

                        ops.Add(tx, (false, contract, tokens));
                    }
                }
            }
            foreach (var (bigmap, key, update, op) in updates)
            {
                if ((bigmap.Tags & BigMapTag.LedgerTypes) == 0 || update.Action == BigMapAction.Allocate)
                    continue;

                if (!ops.TryGetValue(op, out var opCtx))
                {
                    var contract = (await Cache.Accounts.GetAsync(
                        op is TransactionOperation tx
                            ? tx.TargetId!.Value
                            : (op as OriginationOperation)!.ContractId!.Value
                    ) as Contract)!;

                    opCtx = (false, contract, []);
                    ops.Add(op, opCtx);
                }

                if (update.Action == BigMapAction.Remove)
                {
                    ops[op] = (true, ops[op].Contract, ops[op].Tokens);
                }
                else
                {
                    foreach (var (address, tokenId, balance) in BigMaps.ParseLedger(bigmap, key!, update))
                    {
                        if (!opCtx.Tokens.TryGetValue(tokenId, out var tokenCtx))
                        {
                            tokenCtx = ([], []);
                            opCtx.Tokens.Add(tokenId, tokenCtx);
                        }
                        tokenCtx.Balances.Add((address, balance));
                    }
                }
            }
            #endregion

            if (ops.Count == 0) return;

            #region precache
            var accountsSet = new HashSet<string>();
            var tokensSet = new HashSet<(int, BigInteger)>();
            var balancesSet = new HashSet<(int, long)>();
            var nftAccountsSet = new HashSet<int>();

            foreach (var (op, opCtx) in ops)
            {
                foreach (var (tokenId, tokenCtx) in opCtx.Tokens)
                {
                    foreach (var (from, to, _) in tokenCtx.Transfers)
                    {
                        accountsSet.Add(from);
                        accountsSet.Add(to);
                        tokensSet.Add((opCtx.Contract.Id, tokenId));
                    }
                    foreach (var (address, _) in tokenCtx.Balances)
                    {
                        accountsSet.Add(address);
                        tokensSet.Add((opCtx.Contract.Id, tokenId));
                    }
                }
            }
            
            await Cache.Tokens.Preload(tokensSet);
            await Cache.Accounts.Preload(accountsSet);

            foreach (var (op, opCtx) in ops)
            {
                foreach (var (tokenId, tokenCtx) in opCtx.Tokens)
                {
                    foreach (var (from, to, _) in tokenCtx.Transfers)
                        if (Cache.Tokens.TryGet(opCtx.Contract.Id, tokenId, out var token))
                        {
                            if (Cache.Accounts.TryGetCached(from, out var fromAcc))
                                balancesSet.Add((fromAcc.Id, token.Id));
                            
                            if (Cache.Accounts.TryGetCached(to, out var toAcc))
                                balancesSet.Add((toAcc.Id, token.Id));
                        }

                    foreach (var (address, _) in tokenCtx.Balances)
                        if (Cache.Tokens.TryGet(opCtx.Contract.Id, tokenId, out var token))
                        {
                            if (Cache.Accounts.TryGetCached(address, out var acc))
                                balancesSet.Add((acc.Id, token.Id));

                            if (token.OwnerId != null)
                            {
                                nftAccountsSet.Add((int)token.OwnerId);
                                balancesSet.Add(((int)token.OwnerId, token.Id));
                            }
                        }
                }
            }

            await Cache.Accounts.Preload(nftAccountsSet);
            await Cache.TokenBalances.Preload(balancesSet);
            #endregion

            foreach (var (op, opCtx) in ops.OrderBy(kv => kv.Key.Id))
            {
                if (opCtx.Reset)
                {
                    opBlocks[op.Level].Events |= BlockEvents.Tokens;
                    await ResetLedgers(op, opCtx.Contract);
                }

                foreach (var (tokenId, tokenCtx) in opCtx.Tokens)
                {
                    if (Cache.Tokens.TryGet(opCtx.Contract.Id, tokenId, out var token))
                    {
                        if (token.OwnerId != null && tokenCtx.Balances.Count == 1 && tokenCtx.Balances[0].Balance != BigInteger.Zero)
                        {
                            var prevHolder = Cache.Accounts.GetCached((int)token.OwnerId);
                            if (prevHolder.Address != tokenCtx.Balances[0].Address)
                                tokenCtx.Balances.Add((prevHolder.Address, BigInteger.Zero));
                        }

                        if (tokenCtx.Transfers.Count > 0 && ValidateTransfers(token, tokenCtx))
                        {
                            ProcessTransfers(op, opBlocks[op.Level], opCtx.Contract, token, tokenCtx.Transfers);
                        }
                        else
                        {
                            var diffs = GetDiffs(op, opBlocks[op.Level], token, tokenCtx.Balances);
                            if (diffs.Count > 0)
                            {
                                ProcessDiffs(op, opBlocks[op.Level], opCtx.Contract, token, diffs);
                            }
                        }
                    }
                    else
                    {
                        if (tokenCtx.Transfers.Count > 0 && ValidateTransfers(tokenCtx))
                        {
                            token = GetOrCreateToken(op, opBlocks[op.Level], opCtx.Contract, tokenId);
                            ProcessTransfers(op, opBlocks[op.Level], opCtx.Contract, token, tokenCtx.Transfers);
                        }
                        else
                        {
                            var diffs = GetDiffs(op, opBlocks[op.Level], opCtx.Contract, tokenId, tokenCtx.Balances);
                            if (diffs.Count > 0)
                            {
                                token = GetOrCreateToken(op, opBlocks[op.Level], opCtx.Contract, tokenId);
                                ProcessDiffs(op, opBlocks[op.Level], opCtx.Contract, token, diffs);
                            }
                        }
                    }
                }
            }
        }

        async Task ResetLedgers(ContractOperation op, Contract contract)
        {
            var tokens = await Db.Tokens
                //.AsNoTracking()
                .Where(x => x.ContractId == contract.Id)
                .ToListAsync();
            var tokenIds = tokens.Select(x => x.Id).ToHashSet();

            foreach (var token in tokens)
                Cache.Tokens.Add(token);

            var tokenBalances = await Db.TokenBalances
                .AsNoTracking()
                .Where(x => tokenIds.Contains(x.TokenId))
                .ToListAsync();

            var accountIds = tokenBalances.Select(x => x.AccountId).ToHashSet();
            await Cache.Accounts.Preload(accountIds);

            foreach (var tb in tokenBalances)
            {
                var tokenBalance = Cache.TokenBalances.GetOrAdd(tb);
                if (tokenBalance.Balance == BigInteger.Zero) continue;
                var account = Cache.Accounts.GetCached(tokenBalance.AccountId);
                var token = Cache.Tokens.Get(tokenBalance.TokenId);
                token.LastLevel = op.Level;
                MintOrBurnTokens(op, contract, token, account, tokenBalance, -tokenBalance.Balance);
            }
        }

        bool ValidateTransfers((List<(string, string, BigInteger)> Transfers, List<(string, BigInteger)> Balances) ctx)
        {
            var dic = new Dictionary<string, BigInteger>();
            foreach (var (from, to, amount) in ctx.Transfers)
            {
                if (!dic.ContainsKey(from))
                    dic.Add(from, BigInteger.Zero);

                if (!dic.ContainsKey(to))
                    dic.Add(to, BigInteger.Zero);

                dic[from] -= amount;
                dic[to] += amount;
            }
            foreach (var (address, balance) in ctx.Balances)
            {
                if (balance != BigInteger.Zero)
                {
                    if (!dic.ContainsKey(address))
                        return false;

                    dic[address] -= balance;
                }
            }
            return dic.Values.All(x => x == BigInteger.Zero);
        }

        bool ValidateTransfers(Token token, (List<(string, string, BigInteger)> Transfers, List<(string, BigInteger)> Balances) ctx)
        {
            var dic = new Dictionary<string, BigInteger>();
            foreach (var (from, to, amount) in ctx.Transfers)
            {
                if (!dic.ContainsKey(from))
                    dic.Add(from, BigInteger.Zero);

                if (!dic.ContainsKey(to))
                    dic.Add(to, BigInteger.Zero);

                dic[from] -= amount;
                dic[to] += amount;
            }
            foreach (var (address, balance) in ctx.Balances)
            {
                var prevBalance = BigInteger.Zero;
                if (Cache.Accounts.TryGetCached(address, out var account) &&
                    Cache.TokenBalances.TryGet(account.Id, token.Id, out var tokenBalance))
                    prevBalance = tokenBalance.Balance;

                var diff = balance - prevBalance;
                if (diff != BigInteger.Zero)
                {
                    if (!dic.ContainsKey(address))
                        return false;

                    dic[address] -= diff;
                }
            }
            return dic.Values.All(x => x == BigInteger.Zero);
        }

        List<(Account, TokenBalance, BigInteger)> GetDiffs(ContractOperation op, Block block, Contract contract, BigInteger tokenId, List<(string, BigInteger)> balances)
        {
            var diffs = new List<(Account, TokenBalance, BigInteger Diff)>(balances.Count);
            foreach (var (address, balance) in balances)
            {
                if (balance != BigInteger.Zero)
                {
                    var token = GetOrCreateToken(op, block, contract, tokenId);
                    var account = GetOrCreateAccount(op, block, address);
                    var tokenBalance = GetOrCreateTokenBalance(op, block, token, account);
                    diffs.Add((account, tokenBalance, balance));
                }
            }
            return diffs;
        }

        List<(Account, TokenBalance, BigInteger)> GetDiffs(ContractOperation op, Block block, Token token, List<(string, BigInteger)> balances)
        {
            var diffs = new List<(Account, TokenBalance, BigInteger Diff)>(balances.Count);
            foreach (var (address, balance) in balances)
            {
                var prevBalance = BigInteger.Zero;
                if (Cache.Accounts.TryGetCached(address, out var account) &&
                    Cache.TokenBalances.TryGet(account.Id, token.Id, out var tokenBalance))
                    prevBalance = tokenBalance.Balance;

                var diff = balance - prevBalance;
                if (diff != BigInteger.Zero)
                {
                    account = GetOrCreateAccount(op, block, address);
                    tokenBalance = GetOrCreateTokenBalance(op, block, token, account);
                    diffs.Add((account, tokenBalance, diff));
                }
            }
            return diffs;
        }

        void ProcessTransfers(ContractOperation op, Block block, Contract contract, Token token, List<(string, string, BigInteger)> transfers)
        {
            Db.TryAttach(token);
            token.LastLevel = op.Level;

            block.Events |= BlockEvents.Tokens;

            foreach (var (from, to, amount) in transfers)
            {
                var fromAcc = GetOrCreateAccount(op, block, from);
                var fromBalance = GetOrCreateTokenBalance(op, block, token, fromAcc);
                var toAcc = GetOrCreateAccount(op, block, to);
                var toBalance = GetOrCreateTokenBalance(op, block, token, toAcc);
                TransferTokens(op, contract, token, fromAcc, fromBalance, toAcc, toBalance, amount);
            }
        }

        void ProcessDiffs(ContractOperation op, Block block, Contract contract, Token token, List<(Account, TokenBalance, BigInteger Diff)> diffs)
        {
            Db.TryAttach(token);
            token.LastLevel = op.Level;

            block.Events |= BlockEvents.Tokens;

            if (diffs.Count == 1 || diffs.BigSum(x => x.Diff) != BigInteger.Zero)
            {
                foreach (var (account, tokenBalance, diff) in diffs)
                    MintOrBurnTokens(op, contract, token, account, tokenBalance, diff);
            }
            else if (diffs.Count(x => x.Diff < BigInteger.Zero) == 1)
            {
                var (fromAcc, fromBalance, fromDiff) = diffs.First(x => x.Diff < BigInteger.Zero);
                foreach (var (toAcc, toBalance, toDiff) in diffs)
                {
                    if (toAcc == fromAcc) continue;
                    TransferTokens(op, contract, token, fromAcc, fromBalance, toAcc, toBalance, toDiff);
                }
            }
            else if (diffs.Count(x => x.Diff > BigInteger.Zero) == 1)
            {
                var (toAcc, toBalance, toDiff) = diffs.First(x => x.Diff > BigInteger.Zero);
                foreach (var (fromAcc, fromBalance, fromDiff) in diffs)
                {
                    if (fromAcc == toAcc) continue;
                    TransferTokens(op, contract, token, fromAcc, fromBalance, toAcc, toBalance, -fromDiff);
                }
            }
            else
            {
                foreach (var (account, tokenBalance, diff) in diffs)
                    MintOrBurnTokens(op, contract, token, account, tokenBalance, diff);
            }
        }

        Account GetOrCreateAccount(ContractOperation op, Block block, string address)
        {
            if (!Cache.Accounts.TryGetCached(address, out var account))
            {
                account = address[0] == 't' && address[1] == 'z'
                    ? new User
                    {
                        Id = Cache.AppState.NextAccountId(),
                        Address = address,
                        FirstLevel = op.Level,
                        LastLevel = op.Level,
                        Type = AccountType.User
                    }
                    : new Account
                    {
                        Id = Cache.AppState.NextAccountId(),
                        Address = address,
                        FirstLevel = op.Level,
                        LastLevel = op.Level,
                        Type = AccountType.Ghost
                    };
                Db.Accounts.Add(account);
                Cache.Accounts.Add(account);

                Db.TryAttach(block);
                block.Events |= BlockEvents.NewAccounts;
            }
            return account;
        }

        Token GetOrCreateToken(ContractOperation op, Block block, Contract contract, BigInteger tokenId)
        {
            if (!Cache.Tokens.TryGet(contract.Id, tokenId, out var token))
            {
                var state = Cache.AppState.Get();
                state.TokensCount++;

                token = new Token
                {
                    Id = Cache.AppState.NextSubId(op),
                    ContractId = contract.Id,
                    TokenId = tokenId,
                    FirstMinterId = op.InitiatorId ?? op.SenderId,
                    FirstLevel = op.Level,
                    LastLevel = op.Level,
                    TotalBurned = BigInteger.Zero,
                    TotalMinted = BigInteger.Zero,
                    TotalSupply = BigInteger.Zero,
                    Tags = contract.Tags.HasFlag(ContractTags.Nft)
                        ? TokenTags.Nft
                        : contract.Tags.HasFlag(ContractTags.FA2)
                            ? TokenTags.Fa2
                            : TokenTags.Fa12,
                    IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
                };
                Db.Tokens.Add(token);
                Cache.Tokens.Add(token);

                Db.TryAttach(contract);
                contract.TokensCount++;

                Db.TryAttach(block);
                block.Events |= BlockEvents.Tokens;
            }
            return token;
        }

        TokenBalance GetOrCreateTokenBalance(ContractOperation op, Block block, Token token, Account account)
        {
            if (!Cache.TokenBalances.TryGet(account.Id, token.Id, out var tokenBalance))
            {
                var state = Cache.AppState.Get();
                state.TokenBalancesCount++;

                tokenBalance = new TokenBalance
                {
                    Id = Cache.AppState.NextSubId(op),
                    AccountId = account.Id,
                    TokenId = token.Id,
                    ContractId = token.ContractId,
                    FirstLevel = op.Level,
                    LastLevel = op.Level,
                    Balance = BigInteger.Zero,
                    IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
                };
                Db.TokenBalances.Add(tokenBalance);
                Cache.TokenBalances.Add(tokenBalance);

                Db.TryAttach(token);
                token.BalancesCount++;

                Db.TryAttach(account);
                account.TokenBalancesCount++;
                if (account.FirstLevel > op.Level)
                {
                    account.FirstLevel = op.Level;
                    block.Events |= BlockEvents.NewAccounts;
                }
            }
            return tokenBalance;
        }

        void TransferTokens(ContractOperation op, Contract contract, Token token,
            Account from, TokenBalance fromBalance,
            Account to, TokenBalance toBalance,
            BigInteger amount)
        {
            op.TokenTransfers = (op.TokenTransfers ?? 0) + 1;

            Db.TryAttach(from);
            from.TokenTransfersCount++;

            Db.TryAttach(to);
            if (to != from) to.TokenTransfersCount++;

            Db.TryAttach(fromBalance);
            fromBalance.Balance -= amount;
            fromBalance.TransfersCount++;
            fromBalance.LastLevel = op.Level;

            Db.TryAttach(toBalance);
            toBalance.Balance += amount;
            if (toBalance != fromBalance) toBalance.TransfersCount++;
            toBalance.LastLevel = op.Level;

            token.TransfersCount++;
            if (amount != BigInteger.Zero && fromBalance.Id != toBalance.Id)
            {
                if (fromBalance.Balance == BigInteger.Zero)
                {
                    from.ActiveTokensCount--;
                    token.HoldersCount--;
                }
                if (toBalance.Balance == amount)
                {
                    to.ActiveTokensCount++;
                    token.HoldersCount++;
                }
                if (contract.Tags.HasFlag(ContractTags.Nft))
                    token.OwnerId = to.Id;
            }

            var state = Cache.AppState.Get();
            state.TokenTransfersCount++;

            Db.TokenTransfers.Add(new TokenTransfer
            {
                Id = Cache.AppState.NextSubId(op),
                Amount = amount,
                FromId = from.Id,
                ToId = to.Id,
                Level = op.Level,
                TokenId = token.Id,
                ContractId = token.ContractId,
                TransactionId = (op as TransactionOperation)?.Id,
                OriginationId = (op as OriginationOperation)?.Id,
                IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
            });
        }

        void MintOrBurnTokens(ContractOperation op, Contract contract, Token token,
            Account account, TokenBalance balance,
            BigInteger diff)
        {
            op.TokenTransfers = (op.TokenTransfers ?? 0) + 1;

            Db.TryAttach(account);
            account.TokenTransfersCount++;

            Db.TryAttach(balance);
            balance.Balance += diff;
            balance.TransfersCount++;
            balance.LastLevel = op.Level;

            token.TransfersCount++;
            if (balance.Balance == BigInteger.Zero)
            {
                account.ActiveTokensCount--;
                token.HoldersCount--;

                if (contract.Tags.HasFlag(ContractTags.Nft))
                    token.OwnerId = null;
            }
            if (balance.Balance == diff)
            {
                account.ActiveTokensCount++;
                token.HoldersCount++;

                if (contract.Tags.HasFlag(ContractTags.Nft))
                    token.OwnerId = account.Id;
            }
            if (diff > 0) token.TotalMinted += diff;
            else token.TotalBurned += -diff;
            token.TotalSupply += diff;

            var state = Cache.AppState.Get();
            state.TokenTransfersCount++;

            Db.TokenTransfers.Add(new TokenTransfer
            {
                Id = Cache.AppState.NextSubId(op),
                Amount = diff > BigInteger.Zero ? diff : -diff,
                FromId = diff < BigInteger.Zero ? account.Id : null,
                ToId = diff > BigInteger.Zero ? account.Id : null,
                Level = op.Level,
                TokenId = token.Id,
                ContractId = token.ContractId,
                TransactionId = (op as TransactionOperation)?.Id,
                OriginationId = (op as OriginationOperation)?.Id,
                IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
            });
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.Tokens))
                return;

            var state = Cache.AppState.Get();

            var transfers = await Db.TokenTransfers
                .AsNoTracking()
                .Where(x => x.Level == block.Level)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            #region precache
            var accountsSet = new HashSet<int>();
            var tokensSet = new HashSet<long>();
            var tokenBalancesSet = new HashSet<(int, long)>();

            foreach (var tr in transfers)
                tokensSet.Add(tr.TokenId);

            await Cache.Tokens.Preload(tokensSet);

            foreach (var tr in transfers)
            {
                if (tr.FromId is int fromId)
                {
                    accountsSet.Add(fromId);
                    tokenBalancesSet.Add((fromId, tr.TokenId));
                }

                if (tr.ToId is int toId)
                {
                    accountsSet.Add(toId);
                    tokenBalancesSet.Add((toId, tr.TokenId));
                }
            }

            foreach (var id in tokensSet)
            {
                var token = Cache.Tokens.Get(id);
                accountsSet.Add(token.ContractId);
            }

            await Cache.Accounts.Preload(accountsSet);
            await Cache.TokenBalances.Preload(tokenBalancesSet);
            #endregion

            var tokensToRemove = new HashSet<Token>();
            var tokenBalancesToRemove = new HashSet<TokenBalance>();

            foreach (var transfer in transfers)
            {
                var token = Cache.Tokens.Get(transfer.TokenId);
                var contract = (Contract)Cache.Accounts.GetCached(token.ContractId);
                Db.TryAttach(token);
                token.LastLevel = block.Level;
                if (token.FirstLevel == block.Level)
                    tokensToRemove.Add(token);

                if (transfer.FromId is int fromId && transfer.ToId is int toId)
                {
                    #region revert transfer
                    var from = Cache.Accounts.GetCached(fromId);
                    var to = Cache.Accounts.GetCached(toId);
                    var fromBalance = Cache.TokenBalances.Get(from.Id, token.Id);
                    var toBalance = Cache.TokenBalances.Get(to.Id, token.Id);

                    Db.TryAttach(from);
                    Db.TryAttach(to);
                    Db.TryAttach(fromBalance);
                    Db.TryAttach(toBalance);

                    from.TokenTransfersCount--;
                    if (to != from) to.TokenTransfersCount--;

                    fromBalance.Balance += transfer.Amount;
                    fromBalance.TransfersCount--;
                    fromBalance.LastLevel = block.Level;
                    if (fromBalance.FirstLevel == block.Level)
                        tokenBalancesToRemove.Add(fromBalance);

                    toBalance.Balance -= transfer.Amount;
                    if (toBalance != fromBalance) toBalance.TransfersCount--;
                    toBalance.LastLevel = block.Level;
                    if (toBalance.FirstLevel == block.Level)
                        tokenBalancesToRemove.Add(toBalance);

                    token.TransfersCount--;
                    if (transfer.Amount != BigInteger.Zero && fromBalance.Id != toBalance.Id)
                    {
                        if (fromBalance.Balance == transfer.Amount)
                        {
                            from.ActiveTokensCount++;
                            token.HoldersCount++;
                        }
                        if (toBalance.Balance == BigInteger.Zero)
                        {
                            to.ActiveTokensCount--;
                            token.HoldersCount--;
                        }

                        if (contract.Tags.HasFlag(ContractTags.Nft))
                            token.OwnerId = from.Id;
                    }

                    state.TokenTransfersCount--;
                    #endregion
                }
                else if (transfer.ToId != null)
                {
                    #region revert mint
                    var to = Cache.Accounts.GetCached((int)transfer.ToId);
                    var toBalance = Cache.TokenBalances.Get(to.Id, token.Id);

                    Db.TryAttach(to);
                    Db.TryAttach(toBalance);

                    to.TokenTransfersCount--;

                    toBalance.Balance -= transfer.Amount;
                    toBalance.TransfersCount--;
                    toBalance.LastLevel = block.Level;
                    if (toBalance.FirstLevel == block.Level)
                        tokenBalancesToRemove.Add(toBalance);

                    token.TransfersCount--;
                    if (transfer.Amount != BigInteger.Zero)
                    {
                        if (toBalance.Balance == BigInteger.Zero)
                        {
                            to.ActiveTokensCount--;
                            token.HoldersCount--;
                        }

                        if (contract.Tags.HasFlag(ContractTags.Nft))
                            token.OwnerId = null;

                        token.TotalMinted -= transfer.Amount;
                        token.TotalSupply -= transfer.Amount;
                    }

                    state.TokenTransfersCount--;
                    #endregion
                }
                else
                {
                    #region revert burn
                    var from = Cache.Accounts.GetCached(transfer.FromId!.Value);
                    var fromBalance = Cache.TokenBalances.Get(from.Id, token.Id);

                    Db.TryAttach(from);
                    Db.TryAttach(fromBalance);

                    from.TokenTransfersCount--;

                    fromBalance.Balance += transfer.Amount;
                    fromBalance.TransfersCount--;
                    fromBalance.LastLevel = block.Level;
                    if (fromBalance.FirstLevel == block.Level)
                        tokenBalancesToRemove.Add(fromBalance);

                    token.TransfersCount--;
                    if (transfer.Amount != BigInteger.Zero)
                    {
                        if (fromBalance.Balance == transfer.Amount)
                        {
                            from.ActiveTokensCount++;
                            token.HoldersCount++;
                        }

                        if (contract.Tags.HasFlag(ContractTags.Nft))
                            token.OwnerId = from.Id;

                        token.TotalBurned -= transfer.Amount;
                        token.TotalSupply += transfer.Amount;
                    }

                    state.TokenTransfersCount--;
                    #endregion
                }
            }

            foreach (var tokenBalance in tokenBalancesToRemove)
            {
                if (tokenBalance.FirstLevel == block.Level)
                {
                    Db.TokenBalances.Remove(tokenBalance);
                    Cache.TokenBalances.Remove(tokenBalance);
                        
                    var t = Cache.Tokens.Get(tokenBalance.TokenId);
                    Db.TryAttach(t);
                    t.BalancesCount--;

                    var a = Cache.Accounts.GetCached(tokenBalance.AccountId);
                    Db.TryAttach(a);
                    a.TokenBalancesCount--;

                    state.TokenBalancesCount--;
                }
            }

            foreach (var token in tokensToRemove)
            {
                if (token.FirstLevel == block.Level)
                {
                    Db.Tokens.Remove(token);
                    Cache.Tokens.Remove(token);

                    var c = (Contract)Cache.Accounts.GetCached(token.ContractId);
                    Db.TryAttach(c);
                    c.TokensCount--;

                    state.TokensCount--;
                }
            }

            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""TokenTransfers"" WHERE ""Level"" = {block.Level};");
        }

        static List<(string, string, BigInteger, BigInteger)> ParseTransferParam(IMicheline micheline)
        {
            var transfers = new List<(string, string, BigInteger, BigInteger)>();
            if (micheline is MichelineArray arr)
            {
                foreach (var transfer in arr)
                {
                    var transferPair = (transfer as MichelinePrim)!;
                    var from = transferPair.Args![0].ParseAddress();
                    foreach (var tx in (transferPair.Args[1] as MichelineArray)!)
                    {
                        var txPair = (tx as MichelinePrim)!;
                        var to = txPair.Args![0].ParseAddress();
                        var txPair2 = (txPair.Args[1] as MichelinePrim)!;
                        var tokenId = (txPair2.Args![0] as MichelineInt)!.Value;
                        var amount = (txPair2.Args[1] as MichelineInt)!.Value;

                        transfers.Add((from, to, tokenId, amount));
                    }
                }
            }
            else if (micheline is MichelinePrim pair)
            {
                var from = pair.Args![0].ParseAddress();
                var pair2 = (pair.Args[1] as MichelinePrim)!;
                var to = pair2.Args![0].ParseAddress();
                var value = (pair2.Args[1] as MichelineInt)!.Value;

                transfers.Add((from, to, BigInteger.Zero, value));
            }
            return transfers;
        }
    }
}
