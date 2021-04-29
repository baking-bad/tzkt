using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class JsonIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionOps_JsonParameters",
                table: "TransactionOps",
                column: "JsonParameters")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_JsonKey",
                table: "BigMapKeys",
                column: "JsonKey")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_BigMapKeys_JsonValue",
                table: "BigMapKeys",
                column: "JsonValue")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionOps_JsonParameters",
                table: "TransactionOps");

            migrationBuilder.DropIndex(
                name: "IX_BigMapKeys_JsonKey",
                table: "BigMapKeys");

            migrationBuilder.DropIndex(
                name: "IX_BigMapKeys_JsonValue",
                table: "BigMapKeys");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Metadata",
                table: "Accounts",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });
        }
    }
}
