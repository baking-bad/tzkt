using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Hangzhou : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Tzips",
                table: "Accounts",
                newName: "Tags");

            migrationBuilder.Sql(@"
                UPDATE  ""Accounts""
                SET     ""Tags"" = 0
                WHERE   ""Type"" = 2 AND ""Tags"" IS NULL;");

            migrationBuilder.AddColumn<int>(
                name: "RegisterConstantsCount",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE  ""Accounts""
                SET     ""RegisterConstantsCount"" = 0
                WHERE   ""Type"" < 2;");

            migrationBuilder.AddColumn<byte[][]>(
                name: "Views",
                table: "Scripts",
                type: "bytea[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConstantsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RegisterConstantOpsCount",
                table: "AppState",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RegisterConstantOps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "character varying(54)", maxLength: 54, nullable: true),
                    Value = table.Column<byte[]>(type: "bytea", nullable: true),
                    Refs = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OpHash = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_RegisterConstantOps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegisterConstantOps_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegisterConstantOps_Blocks_Level",
                        column: x => x.Level,
                        principalTable: "Blocks",
                        principalColumn: "Level",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_Address",
                table: "RegisterConstantOps",
                column: "Address",
                unique: true,
                filter: "\"Address\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_Level",
                table: "RegisterConstantOps",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_OpHash",
                table: "RegisterConstantOps",
                column: "OpHash");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterConstantOps_SenderId",
                table: "RegisterConstantOps",
                column: "SenderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegisterConstantOps");

            migrationBuilder.DropColumn(
                name: "Views",
                table: "Scripts");

            migrationBuilder.DropColumn(
                name: "ConstantsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "RegisterConstantOpsCount",
                table: "AppState");

            migrationBuilder.DropColumn(
                name: "RegisterConstantsCount",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "Tags",
                table: "Accounts",
                newName: "Tzips");
        }
    }
}
