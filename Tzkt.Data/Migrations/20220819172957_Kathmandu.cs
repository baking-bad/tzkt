using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Kathmandu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventsCount",
                table: "TransactionOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IncreasePaidStorageOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VdfRevelationOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EventsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IncreasePaidStorageCount",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VdfRevelationsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    ContractCodeHash = table.Column<int>(type: "integer", nullable: false),
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<byte[]>(type: "bytea", nullable: true),
                    RawPayload = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonPayload = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncreasePaidStorageOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    BakerFee = table.Column<long>(type: "bigint", nullable: false),
                    StorageFee = table.Column<long>(type: "bigint", nullable: true),
                    AllocationFee = table.Column<long>(type: "bigint", nullable: true),
                    GasLimit = table.Column<int>(type: "integer", nullable: false),
                    GasUsed = table.Column<int>(type: "integer", nullable: false),
                    StorageLimit = table.Column<int>(type: "integer", nullable: false),
                    StorageUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Errors = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncreasePaidStorageOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncreasePaidStorageOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncreasePaidStorageOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VdfRevelationOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
                    BakerId = table.Column<int>(type: "integer", nullable: false),
                    Reward = table.Column<long>(type: "bigint", nullable: false),
                    Solution = table.Column<byte[]>(type: "bytea", nullable: true),
                    Proof = table.Column<byte[]>(type: "bytea", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "character(51)", fixedLength: true, maxLength: 51, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VdfRevelationOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VdfRevelationOps_Accounts_BakerId",
                        column: x => x.BakerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VdfRevelationOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractCodeHash",
                table: "Events",
                column: "ContractCodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractCodeHash_Tag",
                table: "Events",
                columns: new[] { "ContractCodeHash", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractId",
                table: "Events",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ContractId_Tag",
                table: "Events",
                columns: new[] { "ContractId", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Id",
                table: "Events",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_JsonPayload",
                table: "Events",
                column: "JsonPayload")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Level",
                table: "Events",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Tag",
                table: "Events",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_Events_TransactionId",
                table: "Events",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_ContractId",
                table: "IncreasePaidStorageOps",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_Level",
                table: "IncreasePaidStorageOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_OpHash",
                table: "IncreasePaidStorageOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_IncreasePaidStorageOps_SenderId",
                table: "IncreasePaidStorageOps",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_BakerId",
                table: "VdfRevelationOps",
                column: "BakerId");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_Cycle",
                table: "VdfRevelationOps",
                column: "Cycle");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_Level",
                table: "VdfRevelationOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_VdfRevelationOps_OpHash",
                table: "VdfRevelationOps",
                column: "OpHash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "IncreasePaidStorageOps");

            migrationBuilder.DropTable(
                name: "VdfRevelationOps");

            migrationBuilder.DropColumn(
                name: "EventsCount",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "EventsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "IncreasePaidStorageOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "VdfRevelationOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "EventsCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IncreasePaidStorageCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "VdfRevelationsCount",
                table: "Accounts");
        }
    }
}
