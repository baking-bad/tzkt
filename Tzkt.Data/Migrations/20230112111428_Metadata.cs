using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tzkt.Data.Migrations
{
    /// <inheritdoc />
    public partial class Metadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Triggers.RemoveNotificationTrigger(migrationBuilder, "state_metadata_changed", "AppState");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "account_metadata_changed", "Accounts");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "proposal_metadata_changed", "Proposals");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "protocol_metadata_changed", "Protocols");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "software_metadata_changed", "Software");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "block_metadata_changed", "Blocks");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "constant_metadata_changed", "RegisterConstantOps");

            migrationBuilder.AddColumn<bool>(
                name: "Reverse",
                table: "Domains",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Software",
                newName: "Extras");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "RegisterConstantOps",
                newName: "Extras");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Protocols",
                newName: "Extras");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Proposals",
                newName: "Extras");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Blocks",
                newName: "Extras");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "AppState",
                newName: "Extras");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Accounts",
                newName: "Extras");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Accounts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Extras",
                table: "Accounts",
                column: "Extras")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            Triggers.AddNotificationTrigger(migrationBuilder,
                "state_extras_changed",
                "AppState",
                "Extras",
                @"NEW.""Id"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "account_extras_changed",
                "Accounts",
                "Extras",
                @"NEW.""Address"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "account_metadata_changed",
                "Accounts",
                "Metadata",
                @"NEW.""Address"" || ':'");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "proposal_extras_changed",
                "Proposals",
                "Extras",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "protocol_extras_changed",
                "Protocols",
                "Extras",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "software_extras_changed",
                "Software",
                "Extras",
                @"NEW.""ShortHash"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "block_extras_changed",
                "Blocks",
                "Extras",
                @"NEW.""Level"" || ':' || COALESCE(NEW.""Extras""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "constant_extras_changed",
                "RegisterConstantOps",
                "Extras",
                @"COALESCE(NEW.""Address"", '') || ':' || COALESCE(NEW.""Extras""::text, '')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Triggers.RemoveNotificationTrigger(migrationBuilder, "state_extras_changed", "AppState");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "account_extras_changed", "Accounts");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "account_metadata_changed", "Accounts");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "proposal_extras_changed", "Proposals");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "protocol_extras_changed", "Protocols");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "software_extras_changed", "Software");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "block_extras_changed", "Blocks");
            Triggers.RemoveNotificationTrigger(migrationBuilder, "constant_extras_changed", "RegisterConstantOps");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Extras",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "Accounts",
                newName: "Metadata");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "Software",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "RegisterConstantOps",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "Protocols",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "Proposals",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "Blocks",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Extras",
                table: "AppState",
                newName: "Metadata");

            migrationBuilder.DropColumn(
                name: "Reverse",
                table: "Domains");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "state_metadata_changed",
                "AppState",
                "Metadata",
                @"NEW.""Id"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "account_metadata_changed",
                "Accounts",
                "Metadata",
                @"NEW.""Address"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "proposal_metadata_changed",
                "Proposals",
                "Metadata",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "protocol_metadata_changed",
                "Protocols",
                "Metadata",
                @"NEW.""Hash"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "software_metadata_changed",
                "Software",
                "Metadata",
                @"NEW.""ShortHash"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "block_metadata_changed",
                "Blocks",
                "Metadata",
                @"NEW.""Level"" || ':' || COALESCE(NEW.""Metadata""::text, '')");

            Triggers.AddNotificationTrigger(migrationBuilder,
                "constant_metadata_changed",
                "RegisterConstantOps",
                "Metadata",
                @"COALESCE(NEW.""Address"", '') || ':' || COALESCE(NEW.""Metadata""::text, '')");
        }
    }
}
