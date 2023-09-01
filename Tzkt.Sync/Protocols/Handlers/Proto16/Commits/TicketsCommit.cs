using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        readonly Dictionary<ManagerOperation, Dictionary<TicketIdentity, List<(ManagerOperation Op, TicketUpdate Update)>>> Updates = new();

        public virtual void Append(ManagerOperation parent, ManagerOperation op, IEnumerable<TicketUpdates> updates)
        {
            if (!Updates.TryGetValue(parent, out var opUpdates))
                Updates.Add(parent, opUpdates = new());

            foreach (var update in updates)
            {
                if (!opUpdates.TryGetValue(update.Ticket, out var ticketUpdates))
                    opUpdates.Add(update.Ticket, ticketUpdates = new());

                ticketUpdates.AddRange(update.Updates.Select(update => (op, update)));
            }
        }
        
        public virtual async Task Apply()
        {
            if (Updates.Count == 0) return;
            
            #region precache
            var accountsSet = new HashSet<string>();
            var ticketsSet = new HashSet<(int, byte[], int, byte[], int)>();
            var balancesSet = new HashSet<(int, long)>();

            foreach (var (ticket, updates) in Updates.SelectMany(x => x.Value))
            {
                accountsSet.Add(ticket.Ticketer);
                foreach (var (_, upd) in updates)
                    accountsSet.Add(upd.Account);
            }

            await Cache.Accounts.Preload(accountsSet);

            foreach (var (ticket, _) in Updates.SelectMany(x => x.Value))
            {
                if (Cache.Accounts.TryGetCached(ticket.Ticketer, out var ticketer))
                    ticketsSet.Add((ticketer.Id, ticket.RawType, ticket.TypeHash, ticket.RawContent, ticket.ContentHash));
            }

            await Cache.Tickets.Preload(ticketsSet);

            foreach (var (ticket, updates) in Updates.SelectMany(x => x.Value))
            {
                if (Cache.Accounts.TryGetCached(ticket.Ticketer, out var ticketer))
                {
                    if (Cache.Tickets.TryGetCached(ticketer.Id, ticket.RawType, ticket.RawContent, out var _ticket))
                    {
                        foreach (var (_, upd) in updates)
                        {
                            if (Cache.Accounts.TryGetCached(upd.Account, out var acc))
                                balancesSet.Add((acc.Id, _ticket.Id));
                        }
                    }
                }
            }

            await Cache.TicketBalances.Preload(balancesSet);
            #endregion

            Updates.First().Key.Block.Events |= BlockEvents.Tickets;

            foreach (var (parent, opUpdates) in Updates.OrderBy(kv => kv.Key.Id))
            {
                foreach (var (ticketIdentity, ticketUpdates) in opUpdates.OrderBy(x => x.Value[0].Op.Id).ThenBy(x => x.Key.ContentHash + x.Key.TypeHash))
                {
                    var ticketer = GetOrCreateAccount(ticketUpdates[0].Op, ticketIdentity.Ticketer) as Contract;
                    var ticket = GetOrCreateTicket(ticketUpdates[0].Op, ticketer, ticketIdentity);

                    if (ticketUpdates.Count == 1 || ticketUpdates.BigSum(x => x.Update.Amount) != BigInteger.Zero)
                    {
                        foreach (var (op, ticketUpdate) in ticketUpdates)
                            MintOrBurnTickets(op, ticket, ticketUpdate.Account, ticketUpdate.Amount);
                    }
                    else if (ticketUpdates.Count(x => x.Update.Amount < BigInteger.Zero) == 1)
                    {
                        var (fromOp, fromUpdate) = ticketUpdates.First(x => x.Update.Amount < BigInteger.Zero);
                        foreach (var (op, ticketUpdate) in ticketUpdates.Where(x => x.Update.Amount > BigInteger.Zero))
                            TransferTickets(ticketUpdates[0].Op, ticket, fromUpdate.Account, ticketUpdate.Account, ticketUpdate.Amount);
                    }
                    else if (ticketUpdates.Count(x => x.Update.Amount > BigInteger.Zero) == 1)
                    {
                        var (toOp, toUpdate) = ticketUpdates.First(x => x.Update.Amount > BigInteger.Zero);
                        foreach (var (op, ticketUpdate) in ticketUpdates.Where(x => x.Update.Amount < BigInteger.Zero))
                            TransferTickets(ticketUpdates[0].Op, ticket, ticketUpdate.Account, toUpdate.Account, -ticketUpdate.Amount);
                    }
                    else if (IsTransfersSequence(ticketUpdates))
                    {
                        for (int i = 0; i < ticketUpdates.Count; i += 2)
                        {
                            var u1 = ticketUpdates[i].Update;
                            var u2 = ticketUpdates[i + 1].Update;

                            if (u1.Amount < 0) // from u1 to u2
                                TransferTickets(ticketUpdates[i].Op, ticket, u1.Account, u2.Account, u2.Amount);
                            else // from u2 to u1
                                TransferTickets(ticketUpdates[i].Op, ticket, u2.Account, u1.Account, u1.Amount);
                        }
                    }
                    else
                    {
                        foreach (var (op, ticketUpdate) in ticketUpdates)
                            MintOrBurnTickets(op, ticket, ticketUpdate.Account, ticketUpdate.Amount);
                    }
                }
            }
        }

        static bool IsTransfersSequence(List<(ManagerOperation Op, TicketUpdate Update)> updates)
        {
            if (updates.Count % 2 != 0)
                return false;
            
            for (int i = 0; i < updates.Count; i += 2)
                if (updates[i].Update.Amount > 0 || updates[i].Update.Amount != -updates[i + 1].Update.Amount)
                    return false;
            
            return true;
        }

        Account GetOrCreateAccount(ManagerOperation op, string address)
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
            }
            return account;
        }
        
        Ticket GetOrCreateTicket(ManagerOperation op, Contract ticketer, TicketIdentity ticketToken)
        {
            if (!Cache.Tickets.TryGetCached(ticketer.Id, ticketToken.RawType, ticketToken.RawContent, out var ticket))
            {
                ticket = new Ticket
                {
                    Id = op switch
                    {
                        TransactionOperation transaction => Cache.AppState.NextSubId(transaction),
                        TransferTicketOperation transferTicket => Cache.AppState.NextSubId(transferTicket),
                        SmartRollupExecuteOperation srExecute => Cache.AppState.NextSubId(srExecute),
                        _ => throw new ArgumentOutOfRangeException(nameof(op))
                    },
                    TicketerId = ticketer.Id,
                    FirstMinterId = op switch
                    {
                        TransactionOperation transaction => transaction.InitiatorId ?? transaction.SenderId,
                        TransferTicketOperation transferTicket => transferTicket.SenderId,
                        SmartRollupExecuteOperation srExecute => srExecute.SenderId,
                        _ => throw new ArgumentOutOfRangeException(nameof(op))
                    },
                    FirstLevel = op.Level,
                    LastLevel = op.Level,
                    TotalBurned = BigInteger.Zero,
                    TotalMinted = BigInteger.Zero,
                    TotalSupply = BigInteger.Zero,
                    RawType = ticketToken.RawType,
                    RawContent = ticketToken.RawContent,
                    JsonContent = ticketToken.JsonContent,
                    ContentHash = ticketToken.ContentHash,
                    TypeHash = ticketToken.TypeHash,
                };

                Db.Tickets.Add(ticket);
                Cache.Tickets.Add(ticket);

                Db.TryAttach(ticketer);
                ticketer.TicketsCount++;

                var state = Cache.AppState.Get();
                state.TicketsCount++;
            }
            return ticket;
        }

        TicketBalance GetOrCreateTicketBalance(ManagerOperation op, Ticket ticket, Account account)
        {
            if (!Cache.TicketBalances.TryGet(account.Id, ticket.Id, out var ticketBalance))
            {
                ticketBalance = new TicketBalance
                {
                    Id = op switch
                    {
                        TransactionOperation transaction => Cache.AppState.NextSubId(transaction),
                        TransferTicketOperation transferTicket => Cache.AppState.NextSubId(transferTicket),
                        SmartRollupExecuteOperation srExecute => Cache.AppState.NextSubId(srExecute),
                        _ => throw new ArgumentOutOfRangeException(nameof(op))
                    },
                    AccountId = account.Id,
                    TicketId = ticket.Id,
                    TicketerId = ticket.TicketerId,
                    FirstLevel = op.Level,
                    LastLevel = op.Level,
                    Balance = BigInteger.Zero
                };

                Db.TicketBalances.Add(ticketBalance);
                Cache.TicketBalances.Add(ticketBalance);

                Db.TryAttach(ticket);
                ticket.BalancesCount++;

                Db.TryAttach(account);
                account.TicketBalancesCount++;

                var state = Cache.AppState.Get();
                state.TicketBalancesCount++;
            }
            return ticketBalance;
        }
        
        void TransferTickets(ManagerOperation op, Ticket ticket, string fromAddress, string toAddress, BigInteger amount)
        {
            var from = GetOrCreateAccount(op, fromAddress);
            var fromBalance = GetOrCreateTicketBalance(op, ticket, from);
            var to = GetOrCreateAccount(op, toAddress);
            var toBalance = GetOrCreateTicketBalance(op, ticket, to);
            
            switch (op)
            {
                case TransactionOperation transaction:
                    transaction.TicketTransfers = (transaction.TicketTransfers ?? 0) + 1;
                    break;
                case TransferTicketOperation transferTicket:
                    transferTicket.TicketTransfers = (transferTicket.TicketTransfers ?? 0) + 1;
                    break;
                case SmartRollupExecuteOperation srExecute:
                    srExecute.TicketTransfers = (srExecute.TicketTransfers ?? 0) + 1;
                    break;
            }

            Db.TryAttach(from);
            from.TicketTransfersCount++;
            from.LastLevel = op.Level;

            Db.TryAttach(to);
            if (to != from)
            {
                to.TicketTransfersCount++;
                to.LastLevel = op.Level;
            }

            Db.TryAttach(fromBalance);
            fromBalance.Balance -= amount;
            fromBalance.TransfersCount++;
            fromBalance.LastLevel = op.Level;

            Db.TryAttach(toBalance);
            toBalance.Balance += amount;
            if (toBalance != fromBalance) toBalance.TransfersCount++;
            toBalance.LastLevel = op.Level;

            Db.TryAttach(ticket);
            ticket.TransfersCount++;
            ticket.LastLevel = op.Level;
            if (amount != BigInteger.Zero && fromBalance != toBalance)
            {
                if (fromBalance.Balance == BigInteger.Zero)
                {
                    from.ActiveTicketsCount--;
                    ticket.HoldersCount--;
                }
                if (toBalance.Balance == amount)
                {
                    to.ActiveTicketsCount++;
                    ticket.HoldersCount++;
                }
            }

            var state = Cache.AppState.Get();
            state.TicketTransfersCount++;

            Db.TicketTransfers.Add(new TicketTransfer
            {
                Id = op switch
                {
                    TransactionOperation transaction => Cache.AppState.NextSubId(transaction),
                    TransferTicketOperation transferTicket => Cache.AppState.NextSubId(transferTicket),
                    SmartRollupExecuteOperation srExecute => Cache.AppState.NextSubId(srExecute),
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                Amount = amount,
                FromId = from.Id,
                ToId = to.Id,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                TransferTicketId = (op as TransferTicketOperation)?.Id,
                SmartRollupExecuteId = (op as SmartRollupExecuteOperation)?.Id
            });
        }
        
        void MintOrBurnTickets(ManagerOperation op, Ticket ticket, string address, BigInteger amount)
        {
            var account = GetOrCreateAccount(op, address);
            var balance = GetOrCreateTicketBalance(op, ticket, account);

            switch (op)
            {
                case TransactionOperation transaction:
                    transaction.TicketTransfers = (transaction.TicketTransfers ?? 0) + 1;
                    break;
                case TransferTicketOperation transferTicket:
                    transferTicket.TicketTransfers = (transferTicket.TicketTransfers ?? 0) + 1;
                    break;
                case SmartRollupExecuteOperation srExecute:
                    srExecute.TicketTransfers = (srExecute.TicketTransfers ?? 0) + 1;
                    break;
            }

            Db.TryAttach(account);
            account.TicketTransfersCount++;
            account.LastLevel = op.Level;

            Db.TryAttach(balance);
            balance.Balance += amount;
            balance.TransfersCount++;
            balance.LastLevel = op.Level;

            Db.TryAttach(ticket);
            ticket.TransfersCount++;
            ticket.LastLevel = op.Level;
            ticket.TotalSupply += amount;
            if (amount > BigInteger.Zero)
            {
                ticket.TotalMinted += amount;
                if (balance.Balance == amount)
                {
                    account.ActiveTicketsCount++;
                    ticket.HoldersCount++;
                }
            }
            else if (amount < BigInteger.Zero)
            {
                ticket.TotalBurned += -amount;
                if (balance.Balance == BigInteger.Zero)
                {
                    account.ActiveTicketsCount--;
                    ticket.HoldersCount--;
                }
            }

            var state = Cache.AppState.Get();
            state.TicketTransfersCount++;

            Db.TicketTransfers.Add(new TicketTransfer
            {
                Id = op switch
                {
                    TransactionOperation transaction => Cache.AppState.NextSubId(transaction),
                    TransferTicketOperation transferTicket => Cache.AppState.NextSubId(transferTicket),
                    SmartRollupExecuteOperation srExecute => Cache.AppState.NextSubId(srExecute),
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                Amount = amount > BigInteger.Zero ? amount : -amount,
                FromId = amount < BigInteger.Zero ? account.Id : null,
                ToId = amount > BigInteger.Zero ? account.Id : null,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                TransferTicketId = (op as TransferTicketOperation)?.Id,
                SmartRollupExecuteId = (op as SmartRollupExecuteOperation)?.Id
            });
        }

        public virtual async Task Revert(Block block)
        {
            if (!block.Events.HasFlag(BlockEvents.Tickets))
                return;

            var state = Cache.AppState.Get();

            var transfers = await Db.TicketTransfers
                .AsNoTracking()
                .Where(x => x.Level == block.Level)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            #region precache
            var accountsSet = new HashSet<int>();
            var ticketsSet = new HashSet<long>();
            var balancesSet = new HashSet<(int, long)>();

            foreach (var tr in transfers)
                ticketsSet.Add(tr.TicketId);

            await Cache.Tickets.Preload(ticketsSet);

            foreach (var tr in transfers)
            {
                if (tr.FromId is int fromId)
                {
                    accountsSet.Add(fromId);
                    balancesSet.Add((fromId, tr.TicketId));
                }

                if (tr.ToId is int toId)
                {
                    accountsSet.Add(toId);
                    balancesSet.Add((toId, tr.TicketId));
                }
            }

            foreach (var id in ticketsSet)
            {
                var ticket = Cache.Tickets.GetCached(id);
                accountsSet.Add(ticket.TicketerId);
            }

            await Cache.Accounts.Preload(accountsSet);
            await Cache.TicketBalances.Preload(balancesSet);
            #endregion

            var ticketsToRemove = new HashSet<Ticket>();
            var ticketBalancesToRemove = new HashSet<TicketBalance>();

            foreach (var transfer in transfers)
            {
                var ticket = Cache.Tickets.GetCached(transfer.TicketId);
                Db.TryAttach(ticket);
                ticket.TransfersCount--;
                ticket.LastLevel = block.Level;
                if (ticket.TransfersCount == 0)
                    ticketsToRemove.Add(ticket);

                state.TicketTransfersCount--;

                if (transfer.FromId is int fromId && transfer.ToId is int toId)
                {
                    #region revert transfer
                    var from = Cache.Accounts.GetCached(fromId);
                    var fromBalance = Cache.TicketBalances.Get(from.Id, ticket.Id);
                    var to = Cache.Accounts.GetCached(toId);
                    var toBalance = Cache.TicketBalances.Get(to.Id, ticket.Id);

                    Db.TryAttach(from);
                    from.TicketTransfersCount--;
                    from.LastLevel = block.Level;

                    Db.TryAttach(to);
                    if (to != from)
                    {
                        to.TicketTransfersCount--;
                        to.LastLevel = block.Level;
                    }

                    Db.TryAttach(fromBalance);
                    fromBalance.Balance += transfer.Amount;
                    fromBalance.TransfersCount--;
                    fromBalance.LastLevel = block.Level;
                    if (fromBalance.TransfersCount == 0)
                        ticketBalancesToRemove.Add(fromBalance);

                    Db.TryAttach(toBalance);
                    toBalance.Balance -= transfer.Amount;
                    if (toBalance != fromBalance) toBalance.TransfersCount--;
                    toBalance.LastLevel = block.Level;
                    if (toBalance.TransfersCount == 0)
                        ticketBalancesToRemove.Add(toBalance);

                    if (transfer.Amount != BigInteger.Zero && fromBalance != toBalance)
                    {
                        if (fromBalance.Balance == transfer.Amount)
                        {
                            from.ActiveTicketsCount++;
                            ticket.HoldersCount++;
                        }
                        if (toBalance.Balance == BigInteger.Zero)
                        {
                            to.ActiveTicketsCount--;
                            ticket.HoldersCount--;
                        }
                    }
                    #endregion
                }
                else if (transfer.ToId != null)
                {
                    #region revert mint
                    var to = Cache.Accounts.GetCached((int)transfer.ToId);
                    var toBalance = Cache.TicketBalances.Get(to.Id, ticket.Id);

                    Db.TryAttach(to);
                    to.TicketTransfersCount--;
                    to.LastLevel = block.Level;

                    Db.TryAttach(toBalance);
                    toBalance.Balance -= transfer.Amount;
                    toBalance.TransfersCount--;
                    toBalance.LastLevel = block.Level;
                    if (toBalance.TransfersCount == 0)
                        ticketBalancesToRemove.Add(toBalance);

                    if (transfer.Amount != BigInteger.Zero)
                    {
                        ticket.TotalSupply -= transfer.Amount;
                        ticket.TotalMinted -= transfer.Amount;
                        if (toBalance.Balance == BigInteger.Zero)
                        {
                            to.ActiveTicketsCount--;
                            ticket.HoldersCount--;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region revert burn
                    var from = Cache.Accounts.GetCached((int)transfer.FromId);
                    var fromBalance = Cache.TicketBalances.Get(from.Id, ticket.Id);

                    Db.TryAttach(from);
                    from.TicketTransfersCount--;
                    from.LastLevel = block.Level;

                    Db.TryAttach(fromBalance);
                    fromBalance.Balance += transfer.Amount;
                    fromBalance.TransfersCount--;
                    fromBalance.LastLevel = block.Level;
                    if (fromBalance.TransfersCount == 0)
                        ticketBalancesToRemove.Add(fromBalance);

                    if (transfer.Amount != BigInteger.Zero)
                    {
                        ticket.TotalSupply += transfer.Amount;
                        ticket.TotalBurned -= transfer.Amount;
                        if (fromBalance.Balance == transfer.Amount)
                        {
                            from.ActiveTicketsCount++;
                            ticket.HoldersCount++;
                        }
                    }
                    #endregion
                }
            }

            foreach (var ticketBalance in ticketBalancesToRemove)
            {
                Db.TicketBalances.Remove(ticketBalance);
                Cache.TicketBalances.Remove(ticketBalance);
                        
                var t = Cache.Tickets.GetCached(ticketBalance.TicketId);
                Db.TryAttach(t);
                t.BalancesCount--;

                var a = Cache.Accounts.GetCached(ticketBalance.AccountId);
                Db.TryAttach(a);
                a.TicketBalancesCount--;

                state.TicketBalancesCount--;
            }

            foreach (var ticket in ticketsToRemove)
            {
                Db.Tickets.Remove(ticket);
                Cache.Tickets.Remove(ticket);

                var contract = (Contract)Cache.Accounts.GetCached(ticket.TicketerId);
                Db.TryAttach(contract);
                contract.TicketsCount--;

                state.TicketsCount--;
            }

            await Db.Database.ExecuteSqlRawAsync($"""
                DELETE FROM "TicketTransfers"
                WHERE "Level" = {block.Level}
                """);
        }
    }
}