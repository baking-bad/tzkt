using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        readonly Dictionary<ManagerOperation, List<TicketUpdate>> Updates = new();
        
        public virtual void Append(ManagerOperation op, IEnumerable<TicketUpdate> updates)
        {
            if (!Updates.TryGetValue(op, out var list))
                Updates.Add(op, list = new());

            list.AddRange(updates);
        }
        
        public virtual async Task Apply()
        {
            if (Updates.Count == 0) return;
            
            #region precache
            var accountsSet = new HashSet<string>();
            var ticketsSet = new HashSet<(int, byte[], int, byte[], int)>();
            var balancesSet = new HashSet<(int, long)>();

            var list = Updates.SelectMany(x => x.Value).ToList();
            
            foreach (var update in list)
            {
                accountsSet.Add(update.TicketToken.Ticketer);
                foreach (var upd in update.Updates)
                {
                    accountsSet.Add(upd.Account);
                }
            }
            await Cache.Accounts.Preload(accountsSet);
            
            foreach (var update in list)
            {
                if (Cache.Accounts.TryGetCached(update.TicketToken.Ticketer, out var ticketer))
                    ticketsSet.Add((ticketer.Id, update.TicketToken.RawContent, update.TicketToken.ContentHash, update.TicketToken.RawType, update.TicketToken.TypeHash));
            }

            await Cache.Tickets.Preload(ticketsSet);

            foreach (var update in list)
            {
                if (Cache.Accounts.TryGetCached(update.TicketToken.Ticketer, out var ticketer))
                {
                    if (Cache.Tickets.TryGetCached(ticketer.Id, update.TicketToken.RawContent, update.TicketToken.RawType, out var ticket))
                    {
                        foreach (var upd in update.Updates)
                        {
                            if (Cache.Accounts.TryGetCached(upd.Account, out var acc))
                                balancesSet.Add((acc.Id, ticket.Id));
                        }
                    }
                }
            }

            await Cache.TicketBalances.Preload(balancesSet);
            #endregion

            foreach (var (op, opUpdates) in Updates)
            {
                op.Block.Events |= BlockEvents.Tickets;

                var ticketUpdates = new Dictionary<Ticket, List<Update>>();

                foreach (var upd in opUpdates)
                {
                    var ticketer = GetOrCreateAccount(op, upd.TicketToken.Ticketer) as Contract;
                    var ticket = GetOrCreateTicket(op, ticketer, upd.TicketToken);

                    if (!ticketUpdates.TryGetValue(ticket, out var upds))
                        ticketUpdates.Add(ticket, upds = new());

                    upds.AddRange(upd.Updates);
                }

                foreach (var (ticket, updates) in ticketUpdates)
                {
                    if (updates.Count == 1 || updates.BigSum(x => x.Amount) != BigInteger.Zero)
                    {
                        foreach (var ticketUpdate in updates)
                            MintOrBurnTickets(op, ticket, ticketUpdate.Account, ticketUpdate.Amount);
                    }
                    else if (updates.Count(x => x.Amount < BigInteger.Zero) == 1)
                    {
                        var from = updates.First(x => x.Amount < BigInteger.Zero);
                        foreach (var ticketUpdate in updates.Where(x => x.Amount > BigInteger.Zero))
                            TransferTickets(op, ticket, from.Account, ticketUpdate.Account, ticketUpdate.Amount);
                    }
                    else if (updates.Count(x => x.Amount > BigInteger.Zero) == 1)
                    {
                        var to = updates.First(x => x.Amount > BigInteger.Zero);
                        foreach (var ticketUpdate in updates.Where(x => x.Amount < BigInteger.Zero))
                            TransferTickets(op, ticket, ticketUpdate.Account, to.Account, -ticketUpdate.Amount);
                    }
                    else
                    {
                        foreach (var ticketUpdate in updates)
                            MintOrBurnTickets(op, ticket, ticketUpdate.Account, ticketUpdate.Amount);
                    }
                }
            }
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
        
        Ticket GetOrCreateTicket(ManagerOperation op, Contract contract, TicketToken ticketToken)
        {
            if (Cache.Tickets.TryGetCached(contract.Id, ticketToken.RawContent, ticketToken.RawType, out var ticket))
                return ticket;
            
            var state = Cache.AppState.Get();
            state.TicketsCount++;

            ticket = new Ticket
            {
                Id = op switch
                {
                    TransactionOperation transaction => Cache.AppState.NextSubId(transaction),
                    TransferTicketOperation transferTicket => Cache.AppState.NextSubId(transferTicket),
                    SmartRollupExecuteOperation srExecute => Cache.AppState.NextSubId(srExecute),
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                TicketerId = contract.Id,
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

            Db.TryAttach(contract);
            contract.TicketsCount++;

            return ticket;
        }
        
        TicketBalance GetOrCreateTicketBalance(ManagerOperation op, Ticket ticket, Account account)
        {
            if (Cache.TicketBalances.TryGet(account.Id, ticket.Id, out var ticketBalance))
                return ticketBalance;
            
            var state = Cache.AppState.Get();
            state.TicketBalancesCount++;

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

            ticket.TransfersCount++;
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
                var ticket = Cache.Tickets.Get(id);
                accountsSet.Add(ticket.TicketerId);
            }

            await Cache.Accounts.Preload(accountsSet);
            await Cache.TicketBalances.Preload(balancesSet);
            #endregion

            var ticketsToRemove = new HashSet<Ticket>();
            var ticketBalancesToRemove = new HashSet<TicketBalance>();

            foreach (var transfer in transfers)
            {
                var ticket = Cache.Tickets.Get(transfer.TicketId);
                Db.TryAttach(ticket);
                ticket.LastLevel = block.Level;
                ticket.TransfersCount--;
                if (ticket.FirstLevel == block.Level)
                    ticketsToRemove.Add(ticket);

                if (transfer.FromId is int fromId && transfer.ToId is int toId)
                {
                    #region revert transfer
                    var from = Cache.Accounts.GetCached(fromId);
                    var to = Cache.Accounts.GetCached(toId);
                    var fromBalance = Cache.TicketBalances.Get(from.Id, ticket.Id);
                    var toBalance = Cache.TicketBalances.Get(to.Id, ticket.Id);

                    Db.TryAttach(from);
                    Db.TryAttach(to);
                    Db.TryAttach(fromBalance);
                    Db.TryAttach(toBalance);

                    from.TicketTransfersCount--;
                    if (to != from) to.TicketTransfersCount--;

                    fromBalance.Balance += transfer.Amount;
                    fromBalance.TransfersCount--;
                    fromBalance.LastLevel = block.Level;
                    if (fromBalance.FirstLevel == block.Level)
                        ticketBalancesToRemove.Add(fromBalance);

                    toBalance.Balance -= transfer.Amount;
                    if (toBalance != fromBalance) toBalance.TransfersCount--;
                    toBalance.LastLevel = block.Level;
                    if (toBalance.FirstLevel == block.Level)
                        ticketBalancesToRemove.Add(toBalance);

                    if (transfer.Amount != BigInteger.Zero && fromBalance.Id != toBalance.Id)
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

                    state.TicketTransfersCount--;
                    #endregion
                }
                else if (transfer.ToId != null)
                {
                    #region revert mint
                    var to = Cache.Accounts.GetCached((int)transfer.ToId);
                    var toBalance = Cache.TicketBalances.Get(to.Id, ticket.Id);

                    Db.TryAttach(to);
                    Db.TryAttach(toBalance);

                    to.TicketTransfersCount--;

                    toBalance.Balance -= transfer.Amount;
                    toBalance.TransfersCount--;
                    toBalance.LastLevel = block.Level;
                    if (toBalance.FirstLevel == block.Level)
                        ticketBalancesToRemove.Add(toBalance);

                    if (transfer.Amount != BigInteger.Zero)
                    {
                        if (toBalance.Balance == BigInteger.Zero)
                        {
                            to.ActiveTicketsCount--;
                            ticket.HoldersCount--;
                        }
                        
                        ticket.TotalMinted -= transfer.Amount;
                        ticket.TotalSupply -= transfer.Amount;
                    }

                    state.TicketTransfersCount--;
                    #endregion
                }
                else
                {
                    #region revert burn
                    var from = Cache.Accounts.GetCached((int)transfer.FromId);
                    var fromBalance = Cache.TicketBalances.Get(from.Id, ticket.Id);

                    Db.TryAttach(from);
                    Db.TryAttach(fromBalance);

                    from.TicketTransfersCount--;

                    fromBalance.Balance += transfer.Amount;
                    fromBalance.TransfersCount--;
                    fromBalance.LastLevel = block.Level;
                    if (fromBalance.FirstLevel == block.Level)
                        ticketBalancesToRemove.Add(fromBalance);

                    if (transfer.Amount != BigInteger.Zero)
                    {
                        if (fromBalance.Balance == transfer.Amount)
                        {
                            from.ActiveTicketsCount++;
                            ticket.HoldersCount++;
                        }

                        ticket.TotalBurned -= transfer.Amount;
                        ticket.TotalSupply += transfer.Amount;
                    }

                    state.TicketTransfersCount--;
                    #endregion
                }
            }

            //TODO TICKETS Make test for mint, burn and transfer in one operation
            foreach (var ticketBalance in ticketBalancesToRemove)
            {
                if (ticketBalance.FirstLevel == block.Level)
                {
                    Db.TicketBalances.Remove(ticketBalance);
                    Cache.TicketBalances.Remove(ticketBalance);
                        
                    var t = Cache.Tickets.Get(ticketBalance.TicketId);
                    Db.TryAttach(t);
                    t.BalancesCount--;

                    var a = Cache.Accounts.GetCached(ticketBalance.AccountId);
                    Db.TryAttach(a);
                    a.TicketBalancesCount--;

                    state.TicketBalancesCount--;
                }
            }

            foreach (var ticket in ticketsToRemove)
            {
                if (ticket.FirstLevel == block.Level)
                {
                    Db.Tickets.Remove(ticket);
                    Cache.Tickets.Remove(ticket);

                    var c = (Contract)Cache.Accounts.GetCached(ticket.TicketerId);
                    Db.TryAttach(c);
                    c.TicketsCount--;

                    state.TicketsCount--;
                }
            }

            await Db.Database.ExecuteSqlRawAsync($@"DELETE FROM ""TicketTransfers"" WHERE ""Level"" = {block.Level};");
        }
    }
}