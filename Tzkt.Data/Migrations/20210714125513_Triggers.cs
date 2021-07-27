using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class Triggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_state() RETURNS TRIGGER AS $$
                    BEGIN
                    PERFORM pg_notify('state_changed', NEW.""Level"" || ':' || NEW.""Hash"");
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_account_metadata() RETURNS TRIGGER AS $$
                    BEGIN
                    PERFORM pg_notify('account_metadata_changed', NEW.""Address"" || ':' || COALESCE(NEW.""Metadata""::text, ''));
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_proposal_metadata() RETURNS TRIGGER AS $$
                    BEGIN
                    PERFORM pg_notify('proposal_metadata_changed', NEW.""Hash"" || ':' || COALESCE(NEW.""Metadata""::text, ''));
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_protocol_metadata() RETURNS TRIGGER AS $$
                    BEGIN
                    PERFORM pg_notify('protocol_metadata_changed', NEW.""Hash"" || ':' || COALESCE(NEW.""Metadata""::text, ''));
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_software_metadata() RETURNS TRIGGER AS $$
                    BEGIN
                    PERFORM pg_notify('software_metadata_changed', NEW.""ShortHash"" || ':' || COALESCE(NEW.""Metadata""::text, ''));
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE TRIGGER state_changed
                    AFTER UPDATE ON ""AppState""
                    FOR EACH ROW
                    EXECUTE PROCEDURE notify_state();");

            migrationBuilder.Sql(@"
                CREATE TRIGGER account_metadata_changed
                    AFTER UPDATE OF ""Metadata"" ON ""Accounts""
                    FOR EACH ROW
                    WHEN (OLD.""Metadata"" IS DISTINCT FROM NEW.""Metadata"")
                    EXECUTE PROCEDURE notify_account_metadata();");

            migrationBuilder.Sql(@"
                CREATE TRIGGER proposal_metadata_changed
                    AFTER UPDATE OF ""Metadata"" ON ""Proposals""
                    FOR EACH ROW
                    WHEN (OLD.""Metadata"" IS DISTINCT FROM NEW.""Metadata"")
                    EXECUTE PROCEDURE notify_proposal_metadata();");

            migrationBuilder.Sql(@"
                CREATE TRIGGER protocol_metadata_changed
                    AFTER UPDATE OF ""Metadata"" ON ""Protocols""
                    FOR EACH ROW
                    WHEN (OLD.""Metadata"" IS DISTINCT FROM NEW.""Metadata"")
                    EXECUTE PROCEDURE notify_protocol_metadata();");

            migrationBuilder.Sql(@"
                CREATE TRIGGER software_metadata_changed
                    AFTER UPDATE OF ""Metadata"" ON ""Software""
                    FOR EACH ROW
                    WHEN (OLD.""Metadata"" IS DISTINCT FROM NEW.""Metadata"")
                    EXECUTE PROCEDURE notify_software_metadata();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS state_changed ON ""AppState"" CASCADE");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS account_metadata_changed ON ""Accounts"" CASCADE");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS proposal_metadata_changed ON ""Proposals"" CASCADE");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS protocol_metadata_changed ON ""Protocols"" CASCADE");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS software_metadata_changed ON ""Software"" CASCADE");

            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_state CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_account_metadata CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_proposal_metadata CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_protocol_metadata CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_software_metadata CASCADE");
        }
    }
}
