using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Triggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_state() RETURNS TRIGGER AS $$
                    BEGIN
                    NOTIFY state_changed;
                    RETURN null;
                    END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE TRIGGER state_changed
                    AFTER UPDATE ON ""AppState""
                    FOR EACH STATEMENT
                    EXECUTE PROCEDURE notify_state();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS state_changed ON ""AppState"" CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_state CASCADE");
        }
    }
}
