using System.Numerics;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto17
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        readonly List<(ContractOperation op,TicketUpdate update)> Updates = new();
        
        public virtual void Append(ContractOperation op, IEnumerable<TicketUpdate> updates)
        {
            foreach (var update in updates)
            {
                Updates.Add((op, update));
            }
        }
        
        public virtual async Task Apply()
        {
            if (Updates.Count == 0) return;
            //TODO We need cache here;

            foreach (var (op, ticketUpdates) in Updates)
            {
                var ticketer = await Cache.Accounts.GetAsync(ticketUpdates.TicketToken.Ticketer);
                var contract = ticketer as Contract;

                var ticket = GetOrCreateTicket(op, contract, ticketUpdates.TicketToken);

                //TODO Match updates, if successful, transfers, if not, burns and mints
                foreach (var ticketUpdate in ticketUpdates.Updates)
                {
                    var amount = BigInteger.Parse(ticketUpdate.Amount);
                    var account = GetOrCreateAccount(op, ticketUpdate.Account);
                    var balance = GetOrCreateTicketBalance(op, ticket, account);
                    MintOrBurnTickets(op, ticket, account, balance, amount);
                }

            }
        }

        Account GetOrCreateAccount(ContractOperation op, string address)
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
        
        Ticket GetOrCreateTicket(ContractOperation op, Contract contract, TicketToken ticketToken)
        {
            var contentHash = Script.GetHash(ticketToken.Content.ToBytes());
            var contentTypeHash = Script.GetHash(ticketToken.ContentType.ToBytes());
            
            if (Cache.Tickets.TryGet(contract.Id, contentHash, contentTypeHash, out var ticket)) return ticket;
            
            var state = Cache.AppState.Get();
            state.TicketsCount++;

            
            ticket = new Ticket
            {
                Id = Cache.AppState.NextSubId(op),
                TicketerId = contract.Id,
                FirstMinterId = op.InitiatorId ?? op.SenderId,
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
        
        TicketBalance GetOrCreateTicketBalance(ContractOperation op, Ticket ticket, Account account)
        {
            if (!Cache.TicketBalances.TryGet(account.Id, ticket.Id, out var ticketBalance))
            {
                var state = Cache.AppState.Get();
                state.TicketBalancesCount++;

                ticketBalance = new TicketBalance
                {
                    Id = Cache.AppState.NextSubId(op),
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
        
        void TransferTickets(ContractOperation op, Contract contract, Ticket ticket,
            Account from, TicketBalance fromBalance,
            Account to, TicketBalance toBalance,
            BigInteger amount)
        {
            op.TicketTransfers = (op.TicketTransfers ?? 0) + 1;

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
                Id = Cache.AppState.NextSubId(op),
                Amount = amount,
                FromId = from.Id,
                ToId = to.Id,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                OriginationId = (op as OriginationOperation)?.Id,
                IndexedAt = op.Level <= state.Level ? state.Level + 1 : null
            });
        }
        
        void MintOrBurnTickets(ContractOperation op, Ticket ticket,
            Account account, TicketBalance balance,
            BigInteger diff)
        {
            op.TicketTransfers = (op.TicketTransfers ?? 0) + 1;

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
                Id = Cache.AppState.NextSubId(op),
                Amount = diff > BigInteger.Zero ? diff : -diff,
                FromId = diff < BigInteger.Zero ? account.Id : null,
                ToId = diff > BigInteger.Zero ? account.Id : null,
                Level = op.Level,
                TicketId = ticket.Id,
                TicketerId = ticket.TicketerId,
                TransactionId = (op as TransactionOperation)?.Id,
                OriginationId = (op as OriginationOperation)?.Id,
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
