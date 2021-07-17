namespace Tzkt.Api.Services.Cache
{
    public class RawContract : RawAccount
    {
        public override string Type => AccountTypes.Contract;

        public int Kind { get; set; }
        public int TypeHash { get; set; }
        public int CodeHash { get; set; }
        public int? Tzips { get; set; }

        public int? CreatorId { get; set; }
        public int? ManagerId { get; set; }

        public string KindString => ContractKinds.ToString(Kind);
    }
}
