﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Tzkt.Data.Models;

namespace Tzkt.Api.Websocket
{
    public class OperationsParameter
    {
        public string Address { get; set; }
        public string Types { get; set; }
        public List<string> Entrypoints { get; set; }

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
            if (Address != null && !Regex.IsMatch(Address, "^(tz1|tz2|tz3|KT1)[0-9A-Za-z]{33}$"))
                throw new HubException("Invalid address");

            if (TypesList.Count == 0)
                throw new HubException("Invalid operation types");

            if (Entrypoints?.Count != 0 && !(TypesList.Count == 1 && TypesList[0] == Operations.Transactions))
                throw new HubException("`Entrypoint` field can be used with `transactions` type only");
        }
    }
}