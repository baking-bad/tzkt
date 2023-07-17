using System.Numerics;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto17
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        readonly List<TicketUpdate> Updates = new();
        
        public virtual void Append(IEnumerable<TicketUpdate> updates)
        {
            Updates.AddRange(updates);
        }
        
        public virtual async Task Apply()
        {
            if (Updates.Count == 0) return;
            //TODO We need cache here;

            foreach (var update in Updates)
            {
                // var ticket = GetOrCreateTicket();
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
        
        Ticket GetOrCreateTicket(TicketUpdate update, ContractOperation op, Contract contract, int contentHash, int contentTypeHash)
        {
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
                ContentHash = Script.GetHash(update.TicketToken.Content.ToBytes()),
                ContentTypeHash = Script.GetHash(update.TicketToken.ContentType.ToBytes()),
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

        public virtual async Task Revert(Block block)
        {
            //TODO Implement revert

            throw new NotImplementedException("Revert for Tickets commit not implemented yet");
        }
    }
}
