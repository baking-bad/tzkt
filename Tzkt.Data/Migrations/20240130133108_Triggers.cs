using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddNotificationTrigger(migrationBuilder,
                "state_hash_changed",
                "AppState",
                "Hash",
                @"NEW.""Level"" || ':' || NEW.""Hash""");

            AddNotificationTrigger(migrationBuilder,
                name: "sync_state_changed",
                table: "AppState",
                column: "LastSync",
                payload: @"NEW.""KnownHead"" || ':' || NEW.""LastSync"""); // ISO 8601 (1997-12-17 07:37:16)

            AddNotificationTrigger(migrationBuilder,
                "state_extras_changed",
                "AppState",
                "Extras",
                @"NEW.""Id"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "account_extras_changed",
                "Accounts",
                "Extras",
                @"NEW.""Address"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "account_metadata_changed",
                "Accounts",
                "Metadata",
                @"NEW.""Address"" || ':'");

            AddNotificationTrigger(migrationBuilder,
                "proposal_extras_changed",
                "Proposals",
                "Extras",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "protocol_extras_changed",
                "Protocols",
                "Extras",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "software_extras_changed",
                "Software",
                "Extras",
                @"NEW.""ShortHash"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "block_extras_changed",
                "Blocks",
                "Extras",
                @"NEW.""Level"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            AddNotificationTrigger(migrationBuilder,
                "constant_extras_changed",
                "RegisterConstantOps",
                "Extras",
                @"COALESCE(NEW.""Address"", '') || ':' || COALESCE(NEW.""Extras""::text, '')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveNotificationTrigger(migrationBuilder, "state_hash_changed", "AppState");
            RemoveNotificationTrigger(migrationBuilder, "sync_state_changed", "AppState");
            RemoveNotificationTrigger(migrationBuilder, "state_extras_changed", "AppState");
            RemoveNotificationTrigger(migrationBuilder, "account_extras_changed", "Accounts");
            RemoveNotificationTrigger(migrationBuilder, "account_metadata_changed", "Accounts");
            RemoveNotificationTrigger(migrationBuilder, "proposal_extras_changed", "Proposals");
            RemoveNotificationTrigger(migrationBuilder, "protocol_extras_changed", "Protocols");
            RemoveNotificationTrigger(migrationBuilder, "software_extras_changed", "Software");
            RemoveNotificationTrigger(migrationBuilder, "block_extras_changed", "Blocks");
            RemoveNotificationTrigger(migrationBuilder, "constant_extras_changed", "RegisterConstantOps");
        }
    }
}