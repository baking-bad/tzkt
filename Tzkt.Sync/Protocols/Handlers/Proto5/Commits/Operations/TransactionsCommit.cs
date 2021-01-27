using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Netezos.Encoding;

using Tzkt.Data.Models;
using Tzkt.Data.Models.Base;

namespace Tzkt.Sync.Protocols.Proto5
{
    class TransactionsCommit : Proto4.TransactionsCommit
    {
        public TransactionsCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override BlockEvents GetBlockEvents(Account target)
        {
            return target is Contract c
                ? c.Kind == ContractKind.DelegatorContract
                    ? BlockEvents.DelegatorContracts
                    : BlockEvents.SmartContracts
                : BlockEvents.None;
        }

        protected override async Task ProcessParameters(TransactionOperation transaction, JsonElement param)
        {
            if (transaction.Target is User) return;
            var (rawEp, rawParam) = (param.RequiredString("entrypoint"), Micheline.FromJson(param.Required("value")));

            if (transaction.Target is Contract contract)
            {
                var schema = contract.Kind > ContractKind.DelegatorContract
                    ? (await Cache.Scripts.GetAsync(contract)).Schema
                    : ManagerTz.Schema;

                try
                {
                    var (normEp, normParam) = schema.NormalizeParameters(rawEp, rawParam);

                    transaction.Entrypoint = normEp;
                    transaction.RawParameters = normParam.ToBytes();

                    if (contract.Kind > ContractKind.DelegatorContract)
                        transaction.JsonParameters = schema.HumanizeParameters(normEp, normParam);
                }
                catch (Exception ex)
                {
                    transaction.Entrypoint ??= rawEp;
                    transaction.RawParameters ??= rawParam.ToBytes();

                    if (transaction.Status == OperationStatus.Applied)
                        Logger.LogError($"Failed to humanize tx {transaction.OpHash} parameters: {ex.Message}");
                }
            }
            else
            {
                transaction.Entrypoint = rawEp;
                transaction.RawParameters = rawParam.ToBytes();
            }
        }
    }
}
