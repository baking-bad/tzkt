using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Tzkt.Data.Models;

namespace Tzkt.Api.Websocket
{
    public class OperationsParameter
    {
        public string Address { get; set; }
        public string Types { get; set; }
        public int? CodeHash { get; set; }

        List<Operations> _TypesList = null;
        public List<Operations> TypesList
        {
            get
            {
                if (_TypesList == null)
                {
                    if (Types == null)
                    {
                        _TypesList = new(1) { Operations.Transactions };
                    }
                    else
                    {
                        var types = Types.Split(',');
                        _TypesList = new(types.Length);
                        foreach (var type in types)
                        {
                            if (!OpTypes.TryParse(type, out var res))
                                throw new HubException("Invalid operation type");
                            _TypesList.Add(res);
                        }
                    }
                }
                return _TypesList;
            }
        }

        public void EnsureValid()
        {
            if (Address != null && !Regex.IsMatch(Address, "^(mv1|mv2|mv3|mv4|KT1|txr1|sr1)[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid address");

            if (TypesList.Count == 0)
                throw new HubException("Invalid operation types");

            if (CodeHash != null && TypesList.Any(x =>
                x != Operations.Delegations && 
                x != Operations.Originations &&
                x != Operations.Transactions))
                throw new HubException("CodeHash can be used with delegation, origination, and transaction types only");
        }
    }
}