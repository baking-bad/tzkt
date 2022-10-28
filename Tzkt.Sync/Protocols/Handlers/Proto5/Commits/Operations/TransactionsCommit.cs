using System;
using System.Collections.Generic;
using System.Linq;
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
            string rawEp = null;
            IMicheline rawParam = null;
            try
            {
                rawEp = param.RequiredString("entrypoint");
                rawParam = Micheline.FromJson(param.Required("value"));
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to parse tx parameters");
                transaction.Entrypoint = rawEp ?? string.Empty;
                transaction.RawParameters = new MichelineArray().ToBytes();
                return;
            }

            if (transaction.Target is Contract contract)
            {
                var schema = contract.Kind > ContractKind.DelegatorContract
                    ? (await Cache.Schemas.GetAsync(contract))
                    : Script.ManagerTz;

                try
                {
                    var (normEp, normParam) = schema.NormalizeParameter(rawEp, rawParam);

                    transaction.Entrypoint = normEp;
                    transaction.RawParameters = schema.OptimizeParameter(normEp, normParam).ToBytes();
                    transaction.JsonParameters = schema.HumanizeParameter(normEp, normParam);
                }
                catch (Exception ex)
                {
                    transaction.Entrypoint ??= rawEp;
                    transaction.RawParameters ??= rawParam.ToBytes();

                    if (transaction.Status == OperationStatus.Applied)
                        Logger.LogError($"Failed to humanize tx {transaction.OpHash} parameters: {ex.Message}");
                }
            }
            else if (transaction.Target is Rollup)
            {
                transaction.Entrypoint = rawEp;
                transaction.RawParameters = rawParam.ToBytes();
                try
                { 
                    var ticketValue = (rawParam as MichelineArray)[0];
                    var ticketType = (rawParam as MichelineArray)[1] as MichelinePrim;

                    if (ticketType.Annots == null)
                        ticketType.Annots = new List<IAnnotation>(1);

                    if (ticketType.Annots.Count == 0)
                        ticketType.Annots.Add(new FieldAnnotation("data"));

                    var schema = Netezos.Contracts.Schema.Create(new MichelinePrim
                    {
                        Prim = PrimType.pair,
                        Args = new List<IMicheline>(2)
                        {
                            new MichelinePrim
                            {
                                Prim = PrimType.pair,
                                Args = new List<IMicheline>(2)
                                {
                                    new MichelinePrim
                                    {
                                        Prim = PrimType.address,
                                        Annots = new List<IAnnotation>(1) { new FieldAnnotation("address") }
                                    },
                                    new MichelinePrim
                                    {
                                        Prim = PrimType.pair,
                                        Args = new List<IMicheline>(2)
                                        {
                                            ticketType,
                                            new MichelinePrim
                                            {
                                                Prim = PrimType.nat,
                                                Annots = new List<IAnnotation>(1) { new FieldAnnotation("amount") }
                                            }
                                        }
                                    }
                                },
                                Annots = new List<IAnnotation> { new FieldAnnotation("ticket") }
                            },
                            new MichelinePrim
                            {
                                Prim = PrimType.tx_rollup_l2_address,
                                Annots = new List<IAnnotation> { new FieldAnnotation("address") }
                            }
                        }
                    });

                    transaction.JsonParameters = schema.Humanize(ticketValue);
                }
                catch (Exception ex)
                {
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

        protected override IMicheline NormalizeStorage(TransactionOperation transaction, IMicheline storage, Netezos.Contracts.ContractScript schema)
        {
            return storage;
        }

        protected override IEnumerable<BigMapDiff> ParseBigMapDiffs(TransactionOperation transaction, JsonElement result)
        {
            return result.TryGetProperty("big_map_diff", out var diffs)
                ? diffs.RequiredArray().EnumerateArray().Select(BigMapDiff.Parse)
                : null;
        }
    }
}
