using System.Numerics;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto17
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        readonly List<(ManagerOperation op,TicketUpdate update)> Updates = new();
        // readonly List<(TransferTicketOperation op,TicketUpdate update)> Transfers = new();
        
        public virtual void Append(ManagerOperation op, IEnumerable<TicketUpdate> updates)
        {
            foreach (var update in updates)
            {
                Updates.Add((op, update));
            }
        }
        // public virtual void Append(TransferTicketOperation op, IEnumerable<TicketUpdate> updates)
        // {
        //     foreach (var update in updates)
        //     {
        //         Transfers.Add((op, update));
        //     }
        // }
        
        public virtual async Task Apply()
        {
            if (Updates.Count == 0) return;
            //TODO We need cache here;

            
            #region precache

            var accountsSet = new HashSet<string>();
            var ticketsSet = new HashSet<(int, int, int)>();
            var balancesSet = new HashSet<(int, long)>();

            foreach (var (op, update) in Updates)
            {
                accountsSet.Add(update.TicketToken.Ticketer);
                foreach (var upd in update.Updates)
                {
                    accountsSet.Add(upd.Account);
                }
            }
            await Cache.Accounts.Preload(accountsSet);

            foreach (var (op, update) in Updates)
            {
                if (Cache.Accounts.TryGetCached(update.TicketToken.Ticketer, out var ticketer))
                {
                    //TODO Move out
                    var contentHash = Script.GetHash(update.TicketToken.Content.ToBytes());
                    var contentTypeHash = Script.GetHash(update.TicketToken.ContentType.ToBytes());
                    ticketsSet.Add((ticketer.Id, contentHash, contentTypeHash));
                }
            }

            await Cache.Tickets.Preload(ticketsSet);

            foreach (var (op, update) in Updates)
            {
                if (Cache.Accounts.TryGetCached(update.TicketToken.Ticketer, out var ticketer))
                {
                    //TODO Move out
                    var contentHash = Script.GetHash(update.TicketToken.Content.ToBytes());
                    var contentTypeHash = Script.GetHash(update.TicketToken.ContentType.ToBytes());
                    if (Cache.Tickets.TryGet(ticketer.Id, contentHash, contentTypeHash, out var ticket))
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

            foreach (var (op, ticketUpdates) in Updates)
            {
                //TODO GetOrCreate?
                var ticketer = await Cache.Accounts.GetAsync(ticketUpdates.TicketToken.Ticketer);
                var contract = ticketer as Contract;

                var ticket = GetOrCreateTicket(op, contract, ticketUpdates.TicketToken);

                //TODO Match updates, if successful, transfers, if not, burns and mints
                foreach (var ticketUpdate in ticketUpdates.Updates)
                {
                    var amount = BigInteger.Parse(ticketUpdate.Amount);
                    //TODO Fix here for transfer_ticket
                    var account = GetOrCreateAccount(op, ticketUpdate.Account);
                    var balance = GetOrCreateTicketBalance(op, ticket, account);
                    MintOrBurnTickets(op, ticket, account, balance, amount);
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

                Db.TryAttach(op.Block);
                op.Block.Events |= BlockEvents.NewAccounts;
            }
            return account;
        }
        
        Ticket GetOrCreateTicket(ManagerOperation op, Contract contract, TicketToken ticketToken)
        {
            var contentHash = Script.GetHash(ticketToken.Content.ToBytes());
            var contentTypeHash = Script.GetHash(ticketToken.ContentType.ToBytes());
            
            if (Cache.Tickets.TryGet(contract.Id, contentHash, contentTypeHash, out var ticket)) return ticket;
            
            var state = Cache.AppState.Get();
            state.TicketsCount++;

            //Initiator to internal tx, sender for transfer_ticket and parent transaction             

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
                Content = ticketToken.Content.ToBytes(),
                ContentType = ticketToken.ContentType.ToBytes(),
                ContentHash = contentHash,
                ContentTypeHash = contentTypeHash,
                IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
            };
             
            Db.Tickets.Add(ticket);
            Cache.Tickets.Add(ticket);

            Db.TryAttach(contract);
            contract.TicketsCount++;

            Db.TryAttach(op.Block);
            op.Block.Events |= BlockEvents.Tickets;
            return ticket;
        }
        
        TicketBalance GetOrCreateTicketBalance(ManagerOperation op, Ticket ticket, Account account)
        {
            if (!Cache.TicketBalances.TryGet(account.Id, ticket.Id, out var ticketBalance))
            {
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
                    Balance = BigInteger.Zero,
                    IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
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
            }
            return ticketBalance;
        }
        
        void TransferTickets(ManagerOperation op, Contract contract, Ticket ticket,
            Account from, TicketBalance fromBalance,
            Account to, TicketBalance toBalance,
            BigInteger amount)
        {
            //TODO Need to be tested
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

            Db.TryAttach(to);
            if (to != from) to.TicketTransfersCount++;

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
                if (contract.Tags.HasFlag(ContractTags.Nft))
                    ticket.OwnerId = to.Id;
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
                TransferTicketId = (op as OriginationOperation)?.Id,
                IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
            });
        }
        
        void MintOrBurnTickets(ManagerOperation op, Ticket ticket,
            Account account, TicketBalance balance,
            BigInteger diff)
        {
            //TODO Need to be tested
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

            Db.TryAttach(balance);
            balance.Balance += diff;
            balance.TransfersCount++;
            balance.LastLevel = op.Level;

            ticket.TransfersCount++;
            if (balance.Balance == BigInteger.Zero)
            {
                account.ActiveTicketsCount--;
                ticket.HoldersCount--;
            }
            if (balance.Balance == diff)
            {
                account.ActiveTicketsCount++;
                ticket.HoldersCount++;
            }
            if (diff > 0) ticket.TotalMinted += diff;
            else ticket.TotalBurned += -diff;
            ticket.TotalSupply += diff;

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
                Amount = diff > BigInteger.Zero ? diff : -diff,
                FromId = diff < BigInteger.Zero ? account.Id : null,
                ToId = diff > BigInteger.Zero ? account.Id : null,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                TransferTicketId = (op as TransferTicketOperation)?.Id,
                IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
            });
        }

        public virtual async Task Revert(Block block)
        {
            //TODO Implement revert

            throw new NotImplementedException("Revert for Tickets commit not implemented yet");
        }
    }
}
