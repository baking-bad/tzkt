﻿namespace Mvkt.Api.Services.Cache
{
    public class RawContract : RawAccount
    {
        public int Kind { get; set; }
        public int TypeHash { get; set; }
        public int CodeHash { get; set; }
        public int Tags { get; set; }
        public int TokensCount { get; set; }
        public int TicketsCount { get; set; }
        public int EventsCount { get; set; }

        public int? CreatorId { get; set; }
        public int? ManagerId { get; set; }

        public string Metadata { get; set; }

        public string KindString => ContractKinds.ToString(Kind);
    }
}
