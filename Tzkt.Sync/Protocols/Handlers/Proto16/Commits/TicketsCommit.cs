using System.Numerics;
using Netezos.Encoding;
using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto16
{
    class TicketsCommit : ProtocolCommit
    {
        public TicketsCommit(ProtocolHandler protocol) : base(protocol) { }

        public virtual async Task Apply(Block block)
        {
            var ops = new Dictionary<TransferTicketOperation, (
                bool Reset,
                Contract Contract

                )>();
            
            #region group updates
            if (block.TransferTicketOps != null)
            {
                
                
                foreach (var tx in block.TransferTicketOps)
                {
                    if (tx.Status != OperationStatus.Applied) continue;
                    
                    ops.Add(tx, (false, tx.Ticketer as Contract));
                }
            }

            
            #endregion
            
            if (ops.Count == 0) return;

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
        
        Ticket GetOrCreateTicket(ContractOperation op, Contract contract, BigInteger ticketId)
        {
            if (Cache.Tickets.TryGet(contract.Id, ticketId, out var ticket)) return ticket;
            
            var state = Cache.AppState.Get();
            state.TicketsCount++;

            ticket = new Ticket
            {
                Id = Cache.AppState.NextSubId(op),
                ContractId = contract.Id,
                TicketId = ticketId,
                FirstMinterId = op.InitiatorId ?? op.SenderId,
                FirstLevel = op.Level,
                LastLevel = op.Level,
                TotalBurned = BigInteger.Zero,
                TotalMinted = BigInteger.Zero,
                TotalSupply = BigInteger.Zero,
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
    }
}
