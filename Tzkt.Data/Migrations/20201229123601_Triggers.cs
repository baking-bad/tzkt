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
                CREATE TRIGGER state_changed
                    AFTER UPDATE ON ""AppState""
                    FOR EACH ROW
                    EXECUTE PROCEDURE notify_state();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS state_changed ON ""AppState"" CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_state CASCADE");
        }
    }
}
