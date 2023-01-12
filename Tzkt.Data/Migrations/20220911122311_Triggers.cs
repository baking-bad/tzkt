using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class Triggers : Migration
    {
        #region static
        public static void AddNotificationTrigger(MigrationBuilder builder, string name, string table, string column, string payload)
        {
            builder.Sql($@"
                CREATE OR REPLACE FUNCTION notify_{name}() RETURNS TRIGGER AS $$
                    BEGIN
                    PERFORM pg_notify('{name}', {payload});
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            builder.Sql($@"
                CREATE TRIGGER {name}
                    AFTER UPDATE OF ""{column}"" ON ""{table}""
                    FOR EACH ROW
                    WHEN (OLD.""{column}"" IS DISTINCT FROM NEW.""{column}"")
                    EXECUTE PROCEDURE notify_{name}();");
        }

        public static void RemoveNotificationTrigger(MigrationBuilder builder, string name, string table)
        {
            builder.Sql($@"DROP TRIGGER IF EXISTS {name} ON ""{table}"" CASCADE");
            builder.Sql($@"DROP FUNCTION IF EXISTS notify_{name} CASCADE");
        }
        #endregion

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddNotificationTrigger(migrationBuilder,
                "state_hash_changed",
                "AppState",
                "Hash",
                @"NEW.""Level"" || ':' || NEW.""Hash""");

            AddNotificationTrigger(migrationBuilder,
                "state_metadata_changed",
                "AppState",
                "Metadata",
                @"NEW.""Id"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "account_metadata_changed",
                "Accounts",
                "Metadata",
                @"NEW.""Address"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "proposal_metadata_changed",
                "Proposals",
                "Metadata",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "protocol_metadata_changed",
                "Protocols",
                "Metadata",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "software_metadata_changed",
                "Software",
                "Metadata",
                @"NEW.""ShortHash"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "block_metadata_changed",
                "Blocks",
                "Metadata",
                @"NEW.""Level"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "constant_metadata_changed",
                "RegisterConstantOps",
                "Metadata",
                @"COALESCE(NEW.""Address"", '') || ':' || COALESCE(NEW.""Metadata""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                name: "sync_state_changed",
                table: "AppState",
                column: "LastSync",
                payload: @"NEW.""KnownHead"" || ':' || NEW.""LastSync"""); // ISO 8601 (1997-12-17 07:37:16)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveNotificationTrigger(migrationBuilder, "state_hash_changed", "AppState");
            RemoveNotificationTrigger(migrationBuilder, "state_metadata_changed", "AppState");
            RemoveNotificationTrigger(migrationBuilder, "account_metadata_changed", "Accounts");
            RemoveNotificationTrigger(migrationBuilder, "proposal_metadata_changed", "Proposals");
            RemoveNotificationTrigger(migrationBuilder, "protocol_metadata_changed", "Protocols");
            RemoveNotificationTrigger(migrationBuilder, "software_metadata_changed", "Software");
            RemoveNotificationTrigger(migrationBuilder, "block_metadata_changed", "Blocks");
            RemoveNotificationTrigger(migrationBuilder, "constant_metadata_changed", "RegisterConstantOps");
            RemoveNotificationTrigger(migrationBuilder, "sync_state_changed", "AppState");
        }
    }
}
