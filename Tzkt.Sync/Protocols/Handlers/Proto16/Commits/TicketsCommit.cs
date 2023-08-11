using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        readonly Dictionary<long, List<(ManagerOperation op, TicketUpdate update)>> Updates = new();
        
        public virtual void Append(long parentId, ManagerOperation op, IEnumerable<TicketUpdate> updates)
        {
            if (!Updates.TryGetValue(parentId, out var list))
            {
                Updates.Add(parentId, list = new List<(ManagerOperation op, TicketUpdate update)>());
            }
            list.AddRange(updates.Select(update => (op, update)).ToList());
        }
        
        public virtual async Task Apply()
        {
            if (Updates.Count == 0) return;
            
            #region precache

            var accountsSet = new HashSet<string>();
            var ticketsSet = new HashSet<(int, int, int)>();
            var balancesSet = new HashSet<(int, long)>();

            var list = Updates.SelectMany(x => x.Value).Select(x => x.update).ToList();
            
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
                    ticketsSet.Add((ticketer.Id, update.TicketToken.ContentHash, update.TicketToken.ContentTypeHash));
            }

            await Cache.Tickets.Preload(ticketsSet);

            foreach (var update in list)
            {
                if (Cache.Accounts.TryGetCached(update.TicketToken.Ticketer, out var ticketer))
                {
                    if (Cache.Tickets.TryGet(ticketer.Id, update.TicketToken.ContentHash, update.TicketToken.ContentTypeHash, out var ticket))
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

            foreach (var (_, opUpdates) in Updates)
            {
                var a = new Dictionary<Ticket, (ManagerOperation operation, List<Update> updates)>();

                foreach (var (op, upd) in opUpdates)
                {
                    var ticketer = GetOrCreateAccount(op, upd.TicketToken.Ticketer) as Contract;
                    var ticket = GetOrCreateTicket(op, ticketer, upd.TicketToken);

                    if (!a.TryGetValue(ticket, out var upds))
                    {
                        a.Add(ticket, upds = new()
                        {
                            updates = new List<Update>(),
                            operation = op
                        });
                    }
                    upds.updates.AddRange(upd.Updates);
                    //TODO That's wrong, isn't it? Parent operation everywhere?
                    upds.operation = op;
                }

                foreach (var (ticket, (op, updates)) in a)
                {
                    Db.TryAttach(op.Block);
                    op.Block.Events |= BlockEvents.Tickets;
                    
                    //TODO Will we add transfers to itself?
                    if (updates.Count == 1 || updates.BigSum(x => x.Amount) != BigInteger.Zero)
                    {
                        foreach (var ticketUpdate in updates)
                        {
                            var account = GetOrCreateAccount(op, ticketUpdate.Account);
                            var balance = GetOrCreateTicketBalance(op, ticket, account);
                            MintOrBurnTickets(op, ticket, account, balance, ticketUpdate.Amount);
                        }
                    }
                    else if (updates.Count(x => x.Amount < BigInteger.Zero) == 1)
                    {
                        var from = updates.First(x => x.Amount < BigInteger.Zero);
                        foreach (var ticketUpdate in updates)
                        {
                            if (from.Account == ticketUpdate.Account) continue;
                            TransferTickets(op, ticket, from.Account, ticketUpdate.Account, ticketUpdate.Amount);
                        }
                    }
                    else if (updates.Count(x => x.Amount > BigInteger.Zero) == 1)
                    {
                        var to = updates.First(x => x.Amount > BigInteger.Zero);
                        foreach (var ticketUpdate in updates)
                        {
                            if (to.Account == ticketUpdate.Account) continue;
                            TransferTickets(op, ticket, ticketUpdate.Account, to.Account, ticketUpdate.Amount);
                        }
                    }
                    else
                    {
                        foreach (var ticketUpdate in updates)
                        {
                            var account = GetOrCreateAccount(op, ticketUpdate.Account);
                            var balance = GetOrCreateTicketBalance(op, ticket, account);
                            MintOrBurnTickets(op, ticket, account, balance, ticketUpdate.Amount);
                        }
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

                //TODO Do we need to attach it twice?
                Db.TryAttach(op.Block);
                op.Block.Events |= BlockEvents.NewAccounts;
            }
            return account;
        }
        
        Ticket GetOrCreateTicket(ManagerOperation op, Contract contract, TicketToken ticketToken)
        {
            if (Cache.Tickets.TryGet(contract.Id, ticketToken.ContentHash, ticketToken.ContentTypeHash, out var ticket)) return ticket;
            
            var state = Cache.AppState.Get();
            state.TicketsCount++;

            ticket = new Ticket
            {
                Id = op switch
                {
                    ContractOperation contractOperation => Cache.AppState.NextSubId(contractOperation),
                    TransferTicketOperation transfer => Cache.AppState.NextSubId(transfer),
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                TicketerId = contract.Id,
                FirstMinterId = op switch
                {
                    ContractOperation contractOperation => contractOperation.InitiatorId ?? contractOperation.SenderId,
                    TransferTicketOperation transfer => transfer.SenderId,
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                FirstLevel = op.Level,
                LastLevel = op.Level,
                TotalBurned = BigInteger.Zero,
                TotalMinted = BigInteger.Zero,
                TotalSupply = BigInteger.Zero,
                RawContent = ticketToken.RawContent,
                RawType = ticketToken.RawType,
                JsonContent = ticketToken.JsonContent,
                ContentHash = ticketToken.ContentHash,
                ContentTypeHash = ticketToken.ContentTypeHash,
            };
             
            Db.Tickets.Add(ticket);
            Cache.Tickets.Add(ticket);

            Db.TryAttach(contract);
            contract.TicketsCount++;

            return ticket;
        }
        
        TicketBalance GetOrCreateTicketBalance(ManagerOperation op, Ticket ticket, Account account)
        {
            if (Cache.TicketBalances.TryGet(account.Id, ticket.Id, out var ticketBalance)) return ticketBalance;
            
            var state = Cache.AppState.Get();
            state.TicketBalancesCount++;

            ticketBalance = new TicketBalance
            {
                Id = op switch
                {
                    ContractOperation contractOperation => Cache.AppState.NextSubId(contractOperation),
                    TransferTicketOperation transfer => Cache.AppState.NextSubId(transfer),
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
            if (account.FirstLevel > op.Level)
            {
                account.FirstLevel = op.Level;
                op.Block.Events |= BlockEvents.NewAccounts;
            }
            return ticketBalance;
        }
        
        void TransferTickets(ManagerOperation op, Ticket ticket,
            string fromAddress, string toAddress,
            BigInteger amount)
        {
            var from = GetOrCreateAccount(op, fromAddress);
            var fromBalance = GetOrCreateTicketBalance(op, ticket, from);
            var to = GetOrCreateAccount(op, toAddress);
            var toBalance = GetOrCreateTicketBalance(op, ticket, to);
            
            switch (op)
            {
                case TransferTicketOperation transfer1:
                    transfer1.TicketTransfers = (transfer1.TicketTransfers ?? 0) + 1;
                    break;
                case TransactionOperation tx:
                    tx.TicketTransfers = (tx.TicketTransfers ?? 0) + 1;
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
            if (amount != BigInteger.Zero && fromBalance.Id != toBalance.Id)
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
                    ContractOperation contractOperation => Cache.AppState.NextSubId(contractOperation),
                    TransferTicketOperation transfer => Cache.AppState.NextSubId(transfer),
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                Amount = amount,
                FromId = from.Id,
                ToId = to.Id,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                TransferTicketId = (op as OriginationOperation)?.Id
            });
        }
        
        void MintOrBurnTickets(ManagerOperation op, Ticket ticket,
            Account account, TicketBalance balance,
            BigInteger amount)
        {
            Db.TryAttach(op);
            switch (op)
            {
                case TransferTicketOperation transfer1:
                    transfer1.TicketTransfers = (transfer1.TicketTransfers ?? 0) + 1;
                    break;
                case TransactionOperation tx:
                    tx.TicketTransfers = (tx.TicketTransfers ?? 0) + 1;
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
            if (balance.Balance == BigInteger.Zero)
            {
                account.ActiveTicketsCount--;
                ticket.HoldersCount--;
            }
            if (balance.Balance == amount)
            {
                account.ActiveTicketsCount++;
                ticket.HoldersCount++;
            }
            if (amount > 0) ticket.TotalMinted += amount;
            else ticket.TotalBurned += -amount;
            ticket.TotalSupply += amount;

            var state = Cache.AppState.Get();
            state.TicketTransfersCount++;

            Db.TicketTransfers.Add(new TicketTransfer
            {
                Id = op switch
                {
                    ContractOperation contractOperation => Cache.AppState.NextSubId(contractOperation),
                    TransferTicketOperation transfer => Cache.AppState.NextSubId(transfer),
                    _ => throw new ArgumentOutOfRangeException(nameof(op))
                },
                Amount = amount > BigInteger.Zero ? amount : -amount,
                FromId = amount < BigInteger.Zero ? account.Id : null,
                ToId = amount > BigInteger.Zero ? account.Id : null,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                TransferTicketId = (op as TransferTicketOperation)?.Id
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