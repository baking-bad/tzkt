using Npgsql;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Protocols.Proto17
{
    public class InboxCommit : Proto16.InboxCommit
    {
        public InboxCommit(ProtocolHandler protocol) : base(protocol) { }

        protected override void WriteMigrationMessage(NpgsqlBinaryImporter writer, Block block, ref int index)
        {
            writer.StartRow();
            writer.Write(Cache.AppState.NextInboxMessageId(), NpgsqlTypes.NpgsqlDbType.Integer);
            writer.Write(block.Level, NpgsqlTypes.NpgsqlDbType.Integer);
            writer.Write(index++, NpgsqlTypes.NpgsqlDbType.Integer);
            writer.Write((int)InboxMessageType.Migration, NpgsqlTypes.NpgsqlDbType.Integer);
            writer.WriteNull();
            writer.WriteNull();
            writer.WriteNull();
            writer.Write(Proto.VersionName, NpgsqlTypes.NpgsqlDbType.Text);
        }
    }
}
