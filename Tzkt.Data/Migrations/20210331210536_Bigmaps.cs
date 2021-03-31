using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Bigmaps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommitDate",
                table: "Software");

            migrationBuilder.DropColumn(
                name: "CommitHash",
                table: "Software");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Software");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Software");

            migrationBuilder.AddColumn<short>(
                name: "InternalDelegations",
                table: "TransactionOps",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "InternalOriginations",
                table: "TransactionOps",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "InternalTransactions",
                table: "TransactionOps",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Software",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Eth",
                table: "Quotes",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Protocols",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Proposals",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstLevel",
                table: "Cycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastLevel",
                table: "Cycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BigMapCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BigMapKeyCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BigMapUpdateCounter",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Cycle",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "QuoteEth",
                table: "AppState",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Accounts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BigMapKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BigMapPtr = table.Column<int>(type: "integer", nullable: false),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    Updates = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                    RawKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonKey = table.Column<string>(type: "jsonb", nullable: true),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigMapKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BigMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ptr = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    KeyType = table.Column<byte[]>(type: "bytea", nullable: true),
                    ValueType = table.Column<byte[]>(type: "bytea", nullable: true),
                    FirstLevel = table.Column<int>(type: "integer", nullable: false),
                    LastLevel = table.Column<int>(type: "integer", nullable: false),
                    TotalKeys = table.Column<int>(type: "integer", nullable: false),
                    ActiveKeys = table.Column<int>(type: "integer", nullable: false),
                    Updates = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigMaps", x => x.Id);
                    table.UniqueConstraint("AK_BigMaps_Ptr", x => x.Ptr);
                });

            migrationBuilder.CreateTable(
                name: "BigMapUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BigMapPtr = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    BigMapKeyId = table.Column<int>(type: "integer", nullable: true),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigMapUpdates", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppState",
                keyColumn: "Id",
                keyValue: -1,
                column: "Cycle",
                value: -1);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr",
                table: "BigMapKeys",
                column: "BigMapPtr");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr_Active",
                table: "BigMapKeys",
                columns: new[] { "BigMapPtr", "Active" },
                filter: "\"Active\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_BigMapPtr_KeyHash",
                table: "BigMapKeys",
                columns: new[] { "BigMapPtr", "KeyHash" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_Id",
                table: "BigMapKeys",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_LastLevel",
                table: "BigMapKeys",
                column: "LastLevel");

            migrationBuilder.CreateIndex(
                name: "IX_BigMaps_ContractId",
                table: "BigMaps",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_BigMaps_Id",
                table: "BigMaps",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMaps_Ptr",
                table: "BigMaps",
                column: "Ptr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_BigMapKeyId",
                table: "BigMapUpdates",
                column: "BigMapKeyId",
                filter: "\"BigMapKeyId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_BigMapPtr",
                table: "BigMapUpdates",
                column: "BigMapPtr");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_Id",
                table: "BigMapUpdates",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_Level",
                table: "BigMapUpdates",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_OriginationId",
                table: "BigMapUpdates",
                column: "OriginationId",
                filter: "\"OriginationId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_BigMapUpdates_TransactionId",
                table: "BigMapUpdates",
                column: "TransactionId",
                filter: "\"TransactionId\" is not null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BigMapKeys");

            migrationBuilder.DropTable(
                name: "BigMaps");

            migrationBuilder.DropTable(
                name: "BigMapUpdates");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "InternalDelegations",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "InternalOriginations",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "InternalTransactions",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Software");

            migrationBuilder.DropColumn(
                name: "Eth",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Protocols");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "FirstLevel",
                table: "Cycles");

            migrationBuilder.DropColumn(
                name: "LastLevel",
                table: "Cycles");

            migrationBuilder.DropColumn(
                name: "BigMapCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "BigMapKeyCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "BigMapUpdateCounter",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "Cycle",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "QuoteEth",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Accounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "CommitDate",
                table: "Software",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitHash",
                table: "Software",
                type: "character(40)",
                fixedLength: true,
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "Software",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Software",
                type: "text",
                nullable: true);
        }
    }
}
