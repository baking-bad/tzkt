using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class MigrationKinds
    {
        public const string Bootstrap = "bootstrap";
        public const string ActivateDelegate = "activate_delegate";
        public const string Airdrop = "airdrop";
        public const string ProposalInvoice = "proposal_invoice";
        public const string CodeChange = "code_change";
        public const string Origination = "origination";
        public const string Subsidy = "subsidy";
        public const string RemoveBigMapKey = "remove_bigmap_key";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                Bootstrap => (int)MigrationKind.Bootstrap,
                ActivateDelegate => (int)MigrationKind.ActivateDelegate,
                Airdrop => (int)MigrationKind.AirDrop,
                ProposalInvoice => (int)MigrationKind.ProposalInvoice,
                CodeChange => (int)MigrationKind.CodeChange,
                Origination => (int)MigrationKind.Origination,
                Subsidy => (int)MigrationKind.Subsidy,
                RemoveBigMapKey => (int)MigrationKind.RemoveBigMapKey,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)MigrationKind.Bootstrap => Bootstrap,
            (int)MigrationKind.ActivateDelegate => ActivateDelegate,
            (int)MigrationKind.AirDrop => Airdrop,
            (int)MigrationKind.ProposalInvoice => ProposalInvoice,
            (int)MigrationKind.CodeChange => CodeChange,
            (int)MigrationKind.Origination => Origination,
            (int)MigrationKind.Subsidy => Subsidy,
            (int)MigrationKind.RemoveBigMapKey => RemoveBigMapKey,
            _ => throw new Exception("invalid migration kind value")
        };
    }
}
