using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Tzkt.Data.Migrations
{
    public partial class Scripts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Parameters",
                table: "TransactionOps",
                newName: "Entrypoint");

            migrationBuilder.AddColumn<string>(
                name: "JsonParameters",
                table: "TransactionOps",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RawParameters",
                table: "TransactionOps",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageId",
                table: "TransactionOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScriptId",
                table: "OriginationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageId",
                table: "OriginationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewScriptId",
                table: "MigrationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewStorageId",
                table: "MigrationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OldScriptId",
                table: "MigrationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OldStorageId",
                table: "MigrationOps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tzips",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Scripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    MigrationId = table.Column<int>(type: "integer", nullable: true),
                    Current = table.Column<bool>(type: "boolean", nullable: false),
                    ParameterSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    StorageSchema = table.Column<byte[]>(type: "bytea", nullable: true),
                    CodeSchema = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Storages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContractId = table.Column<int>(type: "integer", nullable: false),
                    OriginationId = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    MigrationId = table.Column<int>(type: "integer", nullable: true),
                    Current = table.Column<bool>(type: "boolean", nullable: false),
                    RawValue = table.Column<byte[]>(type: "bytea", nullable: true),
                    JsonValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_StorageId",
                table: "TransactionOps",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_ScriptId",
                table: "OriginationOps",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_OriginationOps_StorageId",
                table: "OriginationOps",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_NewScriptId",
                table: "MigrationOps",
                column: "NewScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_NewStorageId",
                table: "MigrationOps",
                column: "NewStorageId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_OldScriptId",
                table: "MigrationOps",
                column: "OldScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_MigrationOps_OldStorageId",
                table: "MigrationOps",
                column: "OldStorageId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_ContractId_Current",
                table: "Scripts",
                columns: new[] { "ContractId", "Current" },
                filter: "\"Current\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_Id",
                table: "Scripts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Storages_ContractId",
                table: "Storages",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_ContractId_Current",
                table: "Storages",
                columns: new[] { "ContractId", "Current" },
                filter: "\"Current\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_Id",
                table: "Storages",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Storages_Level",
                table: "Storages",
                column: "Level");

            migrationBuilder.AddForeignKey(
                name: "FK_MigrationOps_Scripts_NewScriptId",
                table: "MigrationOps",
                column: "NewScriptId",
                principalTable: "Scripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MigrationOps_Scripts_OldScriptId",
                table: "MigrationOps",
                column: "OldScriptId",
                principalTable: "Scripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MigrationOps_Storages_NewStorageId",
                table: "MigrationOps",
                column: "NewStorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MigrationOps_Storages_OldStorageId",
                table: "MigrationOps",
                column: "OldStorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OriginationOps_Scripts_ScriptId",
                table: "OriginationOps",
                column: "ScriptId",
                principalTable: "Scripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OriginationOps_Storages_StorageId",
                table: "OriginationOps",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionOps_Storages_StorageId",
                table: "TransactionOps",
                column: "StorageId",
                principalTable: "Storages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MigrationOps_Scripts_NewScriptId",
                table: "MigrationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_MigrationOps_Scripts_OldScriptId",
                table: "MigrationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_MigrationOps_Storages_NewStorageId",
                table: "MigrationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_MigrationOps_Storages_OldStorageId",
                table: "MigrationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_OriginationOps_Scripts_ScriptId",
                table: "OriginationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_OriginationOps_Storages_StorageId",
                table: "OriginationOps");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionOps_Storages_StorageId",
                table: "TransactionOps");

            migrationBuilder.DropTable(
                name: "Scripts");

            migrationBuilder.DropTable(
                name: "Storages");

            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_StorageId",
                table: "TransactionOps");

            migrationBuilder.DropIndex(
                name: "IX_OriginationOps_ScriptId",
                table: "OriginationOps");

            migrationBuilder.DropIndex(
                name: "IX_OriginationOps_StorageId",
                table: "OriginationOps");

            migrationBuilder.DropIndex(
                name: "IX_MigrationOps_NewScriptId",
                table: "MigrationOps");

            migrationBuilder.DropIndex(
                name: "IX_MigrationOps_NewStorageId",
                table: "MigrationOps");

            migrationBuilder.DropIndex(
                name: "IX_MigrationOps_OldScriptId",
                table: "MigrationOps");

            migrationBuilder.DropIndex(
                name: "IX_MigrationOps_OldStorageId",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "JsonParameters",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "RawParameters",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "StorageId",
                table: "TransactionOps");

            migrationBuilder.DropColumn(
                name: "ScriptId",
                table: "OriginationOps");

            migrationBuilder.DropColumn(
                name: "StorageId",
                table: "OriginationOps");

            migrationBuilder.DropColumn(
                name: "NewScriptId",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "NewStorageId",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "OldScriptId",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "OldStorageId",
                table: "MigrationOps");

            migrationBuilder.DropColumn(
                name: "Tzips",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "Entrypoint",
                table: "TransactionOps",
                newName: "Parameters");
        }
    }
}
