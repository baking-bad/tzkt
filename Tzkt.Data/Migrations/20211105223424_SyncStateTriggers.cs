using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class SyncStateTriggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Triggers.AddNotificationTrigger(migrationBuilder,
                name: "sync_state_changed",
                table: "AppState",
                column: "LastSync",
                payload: @"NEW.""KnownHead"" || ':' || NEW.""LastSync"""); // ISO 8601 (1997-12-17 07:37:16)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Triggers.RemoveNotificationTrigger(migrationBuilder, "sync_state_changed", "AppState");
        }
    }
}
